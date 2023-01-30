using System.Net.WebSockets;
using System.Numerics;

namespace tps_game.Code.Old
{
    public class Game
    {
        public static Random Random = new Random();
        Map map;

        string terrainSkin = "x";

        List<Player> players = new List<Player>();

        public Game(int mapWidth, int mapHeight)
        {
            map = new Map(mapWidth, mapHeight);
        }

        public Player AddPlayer(WebSocket socket, string username)
        {
            // Create a player
            Player player = new Player(socket, username);
            player.GenerateCoordinates(map.width, map.height);
            
            // Add player to the list and on the map
            players.Add(player);
            map.AddPlayer(player);

            // If nobody has moves left, give 5 moves to this player
            if (players.Where(player => player.movesLeft > 0).Count() == 0)
            {
                player.movesLeft = 5;
            }

            return player;
        }

        public void RemovePlayer(Player player)
        {
            // If it was this player's turn, move to the next
            if (player.movesLeft > 0)
            {
                ChangeMoveCount(player, -player.movesLeft);
            }

            // Remove player from the list
            players.Remove(player);

            // Remove player's territories
            map.RemovePlayer(player);
        }

        public bool UsernameAvailable(string username)
        {
            var search = players.Where(p => p.username == username);
            bool isAvailable = search.Count() == 0;
            return isAvailable;
        }

        public void BroadcastSummary()
        {
            // First display every player's position on the map
            foreach (Player player in players)
            {
                map.DrawPlayer(player);
            }

            // Give info about current player's moves
            Player? activePlayer = players.FirstOrDefault(player => player.movesLeft > 0);
            var playerTurn = activePlayer == null ? null : new
            {
                playerID = activePlayer.ID,
                movesLeft = activePlayer.movesLeft
            };

            // Prepare JSON summary
            string update = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                type = "summary",
                mapHeight = map.height,
                mapWidth = map.width,
                map = map.Summarize(),
                turn = playerTurn,
                players = players.Select(player =>
                {
                    return new
                    {
                        username = player.username,
                        color = player.color,
                        territoryCount = map.CountTerritory(player)
                    };
                })
            });

            // Send JSON to all players (async)
            for (int i = 0; i < players.Count; ++i)
            {
                _ = players[i].SendJSON(update);
            }
        }

        // Add or remove turns of a player
        void ChangeMoveCount(Player player, int difference)
        {
            player.movesLeft += difference;
            
            // If this player has no moves left,
            // and there are no players with moves,
            // give 5 moves to the next player
            if (player.movesLeft <= 0 && players.Where(p => p.movesLeft > 0).Count() == 0)
            {
                player.movesLeft = 0; // Reset count

                if (players.IndexOf(player) == players.Count - 1)
                {
                    players[0].movesLeft = 5;
                }
                else
                {
                    players[players.IndexOf(player) + 1].movesLeft = 5;
                }
            }
        }

        void UpdatePlayerMovement(Player player)
        {
            // Movement on player territory does not decrease move count
            if (map.IsPlayerOrTerritory(player.y, player.x, player) == false)
            {
                ChangeMoveCount(player, -1);
            }
            map.UpdatePlayerPosition(player);
        }

        public bool MoveLeft(Player player)
        {
            if (player.x > 0 && player.movesLeft > 0)
            {
                player.x--;
                UpdatePlayerMovement(player);
                return true;
            }
            return false;
        }

        public bool MoveRight(Player player)
        {
            if (player.x < map.width - 1 && player.movesLeft > 0)
            {
                player.x++;
                UpdatePlayerMovement(player);
                return true;
            }
            return false;
        }

        public bool MoveUp(Player player)
        {
            if (player.y > 0 && player.movesLeft > 0)
            {
                player.y--;
                UpdatePlayerMovement(player);
                return true;
            }
            return false;
        }

        public bool MoveDown(Player player)
        {
            if (player.y < map.height - 1 && player.movesLeft > 0)
            {
                player.y++;
                UpdatePlayerMovement(player);
                return true;
            }
            return false;
        }

    }
}
