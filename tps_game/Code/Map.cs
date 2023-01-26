using System;
using System.Numerics;

namespace tps_game.Code
{
    public class Map
    {
        public readonly int width, height;
        string[,] map;

        string terrainSkin = "x";

        public Map(int mapWidth, int mapHeight)
        {
            width = mapWidth;
            height = mapHeight;

            map = new string[mapHeight, mapWidth];
            ClearMap();
        }

        void ForEachBlock(Action<string, int, int> trigger)
        {
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    trigger(map[y, x], y, x);
                }
            }
        }

        void ClearMap()
        {
            ForEachBlock((block, y, x) =>
            {
                map[y, x] = terrainSkin; // No man's land
            });
        }

        string GetBlock(int y, int x, string fallback)
        {
            if (y >= 0 && y < height && x >= 0 && x < width)
            {
                return map[y, x];
            }
            return fallback;
        }

        public string Summarize()
        {
            string summary = "";
            ForEachBlock((block, y, x) =>
            {
                if (x < width - 1)
                {
                    // Draw columns
                    summary += block + " ";
                }
                else
                {
                    // End of column -> go to next row
                    summary += block + "\n";
                }
            });
            return summary;
        }

        public void AddPlayer(Player player)
        {
            // If player's position is taken, try to reset
            int counter = 5;
            while (IsPlayer(player.y, player.x, null) && --counter > 0)
            {
                player.GenerateCoordinates(width, height);
            }

            // Put player on the map
            DrawPlayer(player);
        }

        public void RemovePlayer(Player player)
        {
            ForEachBlock((block, y, x) =>
            {
                if (IsPlayerOrTerritory(block, player))
                {
                    // Remove player from the map
                    map[y, x] = terrainSkin;
                }
            });
        }

        public string GetPlayerId(int y, int x)
        {
            if (y >= 0 && y < height && x >= 0 && x < width)
            {
                return GetPlayerId(map[y, x]);
            }
            return string.Empty;
        }

        public string GetPlayerId(string block)
        {
            if (block.Contains("-"))
            {
                return block.Split("-")[1];
            }
            return string.Empty;
        }

        public bool IsTerrain(int y, int x)
        {
            if (y >= 0 && y < height && x >= 0 && x < width)
            {
                return IsTerrain(map[y, x]);
            }
            return false;
        }

        public bool IsTerrain(string block)
        {
            return block == terrainSkin;
        }

        public bool IsPlayer(int y, int x, Player? player)
        {
            if (y >= 0 && y < height && x >= 0 && x < width)
            {
                return IsPlayer(map[y, x], player);
            }
            return false;
        }

        public bool IsPlayer(string block, Player? player)
        {
            if (block[0] == 'p')
            {
                if (player == null || block.Split("-")[1] == player.ID.ToString())
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsTerritory(int y, int x, Player? player)
        {
            if (y >= 0 && y < height && x >= 0 && x < width)
            {
                return IsTerritory(map[y, x], player);
            }
            return false;
        }

        public bool IsTerritory(string block, Player? player)
        {
            if (block[0] == 't')
            {
                if (player == null || block.Split("-")[1] == player.ID.ToString())
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsPlayerOrTerritory(int y, int x, Player? player)
        {
            return IsPlayer(y, x, player) || IsTerritory(y, x, player);
        }

        public bool IsPlayerOrTerritory(string block, Player? player)
        {
            return IsPlayer(block, player) || IsTerritory(block, player);
        }

        public void DrawPlayer(Player player)
        {
            if (player.y >= 0 && player.y < height && player.x >= 0 && player.x < width)
            {
                map[player.y, player.x] = $"p-{player.ID}-{player.color}";
            }
        }

        public int CountTerritory(Player player)
        {
            int count = 0;
            ForEachBlock((block, y, x) =>
            {
                if (IsTerritory(block, player))
                {
                    ++count;
                }
            });
            return count;
        }

        void DrawTerritory(int y, int x, Player player)
        {
            if (y >= 0 && y < height && x >= 0 && x < width)
            {
                map[y, x] = $"t-{player.ID}-{player.color}";
            }
        }

        public void UpdatePlayerPosition(Player player)
        {
            // First set player's position to territory block
            ForEachBlock((block, y, x) =>
            {
                if (IsPlayer(block, player))
                {
                    DrawTerritory(y, x, player);
                }
            });

            // Now set player's main block
            map[player.y, player.x] = "p-" + player.ID + "-" + player.color;

            // If this move connected player's territory
            string surrounding = GetSideBlocks(player.y, player.x);
            if (surrounding.Count(ch => ch == 't') >= 2)
            {
                expandTerritoryInit(player);
            }

            // Now validate all territories on map
            // TODO:
            //      Improve - Can I only validate territories around current player's block?
            validateTerritoriesInit();
        }

        // Get blocks up-left-right-down of the specified position (only take first char)
        string GetSideBlocks(int y, int x)
        {
            string sideBlocks = "" + GetBlock(y - 1, x, " ")[0] + GetBlock(y, x - 1, " ")[0] + GetBlock(y, x + 1, " ")[0] + GetBlock(y + 1, x, " ")[0];
            return sideBlocks;
        }

        // Try to expand territory around player's coordinates
        void expandTerritoryInit(Player player)
        {
            HashSet<(int, int)> visited = new HashSet<(int, int)>();
            bool isValidTerritory = true;

            // All 8 directions (y, x) in which player's territory may be expanded
            List<(int, int)> directions = new List<(int, int)>
            {
                (-1, -1), (-1, 0), (-1, +1), // Up-left, up and up-right
                (0, -1), (0, +1), // Left and right
                (+1, -1), (+1, 0), (+1, +1) // Down-left, down and down-right
            };

            // Try to expand territory in all 8 directions of player's block
            foreach ((int, int) yxCoords in directions)
            {
                // Before processing, clear any previous attempts
                visited.Clear();
                isValidTerritory = true;
                int yDiff = yxCoords.Item1;
                int xDiff = yxCoords.Item2;

                // Search surrounding to check if this is valid territory
                expandTerritory(player, player.y + yDiff, player.x + xDiff, ref isValidTerritory, ref visited);

                // If found, mark it as player's territory
                if (visited.Count > 0 && isValidTerritory)
                {
                    foreach ((int, int) block in visited)
                    {
                        DrawTerritory(block.Item1, block.Item2, player);
                    }
                }
            }

        }

        // Recursively try to find player's territory starting from (y, x)
        // Territory is found and valid if isValidTerritory is true and visited.Count > 0
        void expandTerritory(Player player, int y, int x, ref bool isValidTerritory, ref HashSet<(int, int)> visited)
        {
            // Check if this block needs processing:
            // - territory hasn't been marked invalid
            // - this block hasn't been visited
            // - this is not this player's territory already
            if (isValidTerritory == false || visited.Contains((y, x)) || IsPlayerOrTerritory(y, x, player))
            {
                return;
            }

            // Check if this block is invalid territory.
            // Can only expand to:
            // - terrain block or
            // - other player's territory
            if (IsTerrain(y, x) == false && IsPlayerOrTerritory(y, x, null) == false)
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

        // Check if all territories are connected to their player, otherwise remove them
        void validateTerritoriesInit()
        {
            HashSet<(int, int)> processed = new HashSet<(int, int)>();

            HashSet<(int, int)> visited = new HashSet<(int, int)>();
            bool playerFound = false;

            ForEachBlock((block, y, x) =>
            {
                // If this block needs processing
                if (IsTerritory(y, x, null) && processed.Contains((y, x)) == false)
                {
                    // Reset search
                    visited.Clear();
                    playerFound = false;
                    string playerId = GetPlayerId(block);

                    // Try to seek for territory starting from block (y, x)
                    validateTerritory(playerId, y, x, ref playerFound, ref visited);

                    if (playerFound == false)
                    {
                        // Player not connected to this territory, so delete it
                        foreach((int, int) yxTuple in visited)
                        {
                            map[y, x] = terrainSkin;
                        }
                    }
                    else
                    {
                        // Player is found, so do not touch territory,
                        // but mark all blocks as processed
                        foreach ((int, int) yxTuple in visited)
                        {
                            processed.Add(yxTuple);
                        }
                    }
                }
            });
        }

        void validateTerritory(string playerId, int y, int x, ref bool playerFound, ref HashSet<(int, int)> visited)
        {
            // Stop processing if:
            // - block already visited
            // - this is terrain
            // - this block does not belong to player
            if (visited.Contains((y, x)) || IsTerrain(y, x) || GetPlayerId(y, x) != playerId)
            {
                return;
            }

            // If this is player's main block, mark it, but keep looking for territory
            if (IsPlayer(y, x, null) && GetPlayerId(y, x) == playerId)
            {
                playerFound = true;
            }

            // Mark block as visited
            visited.Add((y, x));

            // Try to expand territory in all 4 directions from the current block
            validateTerritory(playerId, y - 1, x, ref playerFound, ref visited); // Up
            validateTerritory(playerId, y + 1, x, ref playerFound, ref visited); // Down
            validateTerritory(playerId, y, x - 1, ref playerFound, ref visited); // Left
            validateTerritory(playerId, y, x + 1, ref playerFound, ref visited); // Right

            // Now return from this block
            return;
        }

    }
}
