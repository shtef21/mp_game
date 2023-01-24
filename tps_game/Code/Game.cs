using System.Net.WebSockets;
using System.Numerics;

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
            map[player.y, player.x] = "p-" + player.ID + "-" + player.color; // "p{ID}" is player dot

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
                player.movesLeft = 0;
                subtractPlayerMoves(player);
            }

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
            // First set player's positions to territory blocks
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

            // If this move connected player's territory
            string surrounding = getSurroundingTypes(player.y, player.x);
            if (surrounding.Count(ch => ch == 't') >= 2)
            {
                expandTerritoryInit(player);
            }
        }

        // Try to expand territory around player's coordinates
        void expandTerritoryInit(Player player)
        {
            HashSet<(int, int)> visited = new HashSet<(int, int)>();
            bool isValidTerritory = true;

            // Try to expand territory in all 8 directions of player's block
            for (int i = -1; i < 2; ++i)
            {
                for (int j = -1; j < 2; ++j)
                {
                    if (i != 0 || j != 0)
                    {
                        // Before processing, clear any previous attempts
                        visited.Clear();
                        isValidTerritory = true;
                        expandTerritory(player, player.y + i, player.x + j, ref isValidTerritory, ref visited);

                        // If valid territory found, mark it as player's territory
                        if (visited.Count > 0 && isValidTerritory)
                        {
                            foreach ((int, int) block in visited)
                            {
                                map[block.Item1, block.Item2] = "t-" + player.ID.ToString() + "-" + player.color;
                            }
                        }
                    }
                }
            }

        }

        // Try to find player's territory starting from y, x.
        // Territory is found and valid if isValidTerritory is true and visited.Count > 0
        void expandTerritory(Player player, int y, int x, ref bool isValidTerritory, ref HashSet<(int, int)> visited)
        {
            // Check if this block needs processing:
            // - territory hasn't been marked invalid
            // - this block hasn't been visited
            // - this is not this player's territory already
            if (isValidTerritory == false || visited.Contains((y, x)) || isPlayerOrTerritory(y, x, player))
            {
                return;
            }

            // Check if this block is invalid territory.
            // Can only expand to:
            // - terrain block or
            // - other player's territory
            if (isTerrain(y, x) == false && isPlayerOrTerritory(y, x, null) == false)
            {
                isValidTerritory = false;
                return;
            }

            // First, mark this block as visited
            visited.Add((y, x));

            // Try to expand territory in all 4 directions from the current block
            expandTerritory(player, y - 1, x, ref isValidTerritory, ref visited); // Up
            expandTerritory(player, y + 1, x, ref isValidTerritory, ref visited); // Down
            expandTerritory(player, y, x - 1, ref isValidTerritory, ref visited); // Left
            expandTerritory(player, y, x + 1, ref isValidTerritory, ref visited); // Right

            // Now return from this block
            return;
        }

        // Get blocks up-left-right-down of a block specified by (y, x)
        string getSurroundingTypes(int y, int x)
        {
            string blocksAround = getFirstChar(y - 1, x) + getFirstChar(y, x - 1)
                + getFirstChar(y, x + 1) + getFirstChar(y + 1, x);
            return blocksAround;
        }

        // Check if this coordinate marks terrain on map
        bool isTerrain(int y, int x)
        {
            if (y >= 0 && y <= mapHeight - 1 && x >= 0 && x <= mapWidth - 1)
            {
                return map[y, x] == terrainSkin;
            }
            return false;
        }

        /// <summary>
        /// Check if coordinate is player's territory. If player is null, then any territory is valid.
        /// </summary>
        bool isPlayerOrTerritory(int y, int x, Player? player)
        {
            if (y >= 0 && y <= mapHeight - 1 && x >= 0 && x <= mapWidth - 1)
            {
                if (map[y, x][0] == 't' || map[y, x][0] == 'p')
                {
                    if (player == null || map[y, x].Split("-")[1] == player.ID.ToString())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        string getFirstChar(int y, int x, string fallback="")
        {
            if (y >= 0 && y <= mapHeight - 1 && x >= 0 && x <= mapWidth - 1)
            {
                return map[y, x][0].ToString();
            }
            return "";
        }

        // A player has moved, so remove 1 turn
        void subtractPlayerMoves(Player player)
        {
            player.movesLeft--;
            
            // If this player has no moves left, give 5 moves to the next player
            if (player.movesLeft <= 0)
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

                // Movement on player territory does not decrease move count
                if (isPlayerOrTerritory(player.y, player.x, player) == false)
                {
                    subtractPlayerMoves(player);
                }
                updatePlayerPosition(player);
                return true;
            }
            return false;
        }

        public bool MoveRight(Player player)
        {
            if (player.x < mapWidth - 1 && player.movesLeft > 0)
            {
                player.x++;

                // Movement on player territory does not decrease move count
                if (isPlayerOrTerritory(player.y, player.x, player) == false)
                {
                    subtractPlayerMoves(player);
                }
                updatePlayerPosition(player);
                return true;
            }
            return false;
        }

        public bool MoveUp(Player player)
        {
            if (player.y > 0 && player.movesLeft > 0)
            {
                player.y--;

                // Movement on player territory does not decrease move count
                if (isPlayerOrTerritory(player.y, player.x, player) == false)
                {
                    subtractPlayerMoves(player);
                }
                updatePlayerPosition(player);
                return true;
            }
            return false;
        }

        public bool MoveDown(Player player)
        {
            if (player.y < mapHeight - 1 && player.movesLeft > 0)
            {
                player.y++;

                // Movement on player territory does not decrease move count
                if (isPlayerOrTerritory(player.y, player.x, player) == false)
                {
                    subtractPlayerMoves(player);
                }
                updatePlayerPosition(player);
                return true;
            }
            return false;
        }

    }
}
