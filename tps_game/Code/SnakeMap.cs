using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace tps_game.Code
{
    public class SnakeMap
    {
        public readonly int width, height;
        string[,] map;

        string terrainSkin = "x";

        public SnakeMap(int mapWidth, int mapHeight)
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

        public void AddPlayer(SnakePlayer player)
        {
            UpdatePlayerPosition(player);
        }

        public void RemovePlayer(SnakePlayer player)
        {
            ForEachBlock((block, y, x) =>
            {
                if (IsPlayer(block, player))
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

        public bool IsPlayer(int y, int x, SnakePlayer? player)
        {
            if (y >= 0 && y < height && x >= 0 && x < width)
            {
                return IsPlayer(map[y, x], player);
            }
            return false;
        }

        public bool IsPlayer(string block, SnakePlayer? player)
        {
            if (block[0] == 'p')
            {
                if (player == null || block.Split("-")[1] == player.username)
                {
                    return true;
                }
            }
            return false;
        }

        public void Draw(int y, int x, string block)
        {
            if (y >= 0 && y < height && x >= 0 && x < width)
            {
                map[y, x] = block;
            }
        }

        public void UpdatePlayerPosition(SnakePlayer player)
        {
            // First delete previous player blocks
            ForEachBlock((block, y, x) =>
            {
                if (IsPlayer(block, player))
                {
                    Draw(y, x, terrainSkin);
                }
            });

            // Now draw snake
            foreach((int, int) yxPos in player.positions)
            {
                int y = yxPos.Item1;
                int x = yxPos.Item2;
                map[y, x] = "p-" + player.username + "-" + player.color;
            }

        }

    }
}
