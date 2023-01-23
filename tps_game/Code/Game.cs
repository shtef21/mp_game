using System.Net.WebSockets;

namespace tps_game.Code
{
    public class Game
    {
        public static Random random = new Random();
        int mapWidth, mapHeight;
        string[,] map;

        string terrainSkin = "x";

        List<Player> players = new List<Player>();

        public Game(int mapWidth, int mapHeight)
        {
            this.mapWidth = mapWidth;
            this.mapHeight = mapHeight;

            map = new string[mapHeight, mapWidth];
            ClearMap();
        }

        void ClearMap()
        {
            for (int i = 0; i < mapHeight; ++i)
            {
                for (int j = 0; j < mapWidth; ++j)
                {
                    map[i, j] = terrainSkin; // No man's land
                }
            }
        }

        string SummarizeMap()
        {
            string summary = "";

            for (int i = 0; i < mapHeight; ++i)
            {
                for (int j = 0; j < mapWidth - 1; ++j)
                {
                    summary += map[i, j] + " ";
                }
                summary += map[i, mapWidth - 1] + "\n";
            }
            return summary;
        }

        public Player AddPlayer(WebSocket socket, string username)
        {
            Player player = new Player(socket, username);

            player.x = random.Next(0, mapWidth);
            player.y = random.Next(0, mapHeight);
            
            // Add player to the list and on the map
            players.Add(player);
            map[player.y, player.x] = "p-" + player.ID; // "p{ID}" is player dot

            // If nobody has moves left, give 5 moves to this player
            if (players.Where(player => player.movesLeft > 0).Count() == 0)
            {
                player.movesLeft = 5;
            }

            return player;
        }

        public void RemovePlayer(Player player)
        {
            // Remove player from the list
            players.Remove(player);

            // Remove player's teritories
            for (int i = 0; i < mapHeight; ++i)
            {
                for (int j = 0; j < mapWidth; ++j)
                {
                    if (map[i, j].Contains("-") && map[i, j].Split("-")[1] == player.ID.ToString())
                    {
                        map[i, j] = terrainSkin;
                    }
                }
            }
        }

        public bool UsernameAvailable(string username)
        {
            var search = players.Where(p => p.username == username);
            bool isAvailable = search.Count() == 0;
            return isAvailable;
        }

        public async Task SendUpdate()
        {
            // First display every player's position on the map
            foreach (Player player in players)
            {
                map[player.y, player.x] = "p-" + player.ID + "-" + player.color;
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
                type = "map",
                height = mapHeight,
                width = mapWidth,
                map = SummarizeMap(),
                turn = playerTurn
            });

            // Send JSON to each player
            for (int i = 0; i < players.Count; ++i)
            {
                await players[i].SendJSON(update);
            }
        }

        void updatePlayerPosition(Player player)
        {
            // First set player's positions to teritory blocks
            for (int i = 0; i < mapHeight; ++i)
            {
                for (int j = 0; j < mapWidth; ++j)
                {
                    string mapBlock = map[i, j];
                    if (mapBlock.Contains("-"))
                    {
                        if (mapBlock.Split("-")[1] == player.ID.ToString())
                        {
                            map[i, j] = "t-" + player.ID + "-" + player.color;
                        }
                    }
                }
            }

            // Now set player's main block
            map[player.y, player.x] = "p-" + player.ID + "-" + player.color;
        }

        // A player has moved, so remove 1 turn
        void subtractPlayerMoves(Player player)
        {
            player.movesLeft--;
            
            // If this player has no moves left, give 5 moves to the next player
            if (player.movesLeft == 0)
            {
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

        public bool MoveLeft(Player player)
        {
            if (player.x > 0 && player.movesLeft > 0)
            {
                player.x--;
                updatePlayerPosition(player);
                subtractPlayerMoves(player);
                return true;
            }
            return false;
        }

        public bool MoveRight(Player player)
        {
            if (player.x < mapWidth - 1 && player.movesLeft > 0)
            {
                player.x++;
                updatePlayerPosition(player);
                subtractPlayerMoves(player);
                return true;
            }
            return false;
        }

        public bool MoveUp(Player player)
        {
            if (player.y > 0 && player.movesLeft > 0)
            {
                player.y--;
                updatePlayerPosition(player);
                subtractPlayerMoves(player);
                return true;
            }
            return false;
        }

        public bool MoveDown(Player player)
        {
            if (player.y < mapHeight - 1 && player.movesLeft > 0)
            {
                player.y++;
                updatePlayerPosition(player);
                subtractPlayerMoves(player);
                return true;
            }
            return false;
        }

    }
}
