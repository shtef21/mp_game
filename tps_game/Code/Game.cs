﻿using System.Net.WebSockets;

namespace tps_game.Code
{
    public class Game
    {
        private static Random random = new Random();
        int mapWidth, mapHeight;
        string[,] map;

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
                    map[i, j] = "x"; // No man's land
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

            return player;
        }

        public void RemovePlayer(Player player)
        {
            players.Remove(player);
        }

        public bool UsernameAvailable(string username)
        {
            var search = players.Where(p => p.username == username);
            bool isAvailable = search.Count() == 0;
            return isAvailable;
        }

        public async Task SendUpdate()
        {
            object update = new
            {
                type = "map",
                height = mapHeight,
                width = mapWidth,
                map = SummarizeMap()
            };

            for (int i = 0; i < players.Count; ++i)
            {
                await players[i].SendData(update);
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
                            map[i, j] = "t-" + player.ID;
                        }
                    }
                }
            }

            // Now set player's main block
            map[player.y, player.x] = "p-" + player.ID;
        }

        public bool MoveLeft(Player player)
        {
            if (player.x > 0)
            {
                player.x--;
                updatePlayerPosition(player);
                return true;
            }
            return false;
        }

        public bool MoveRight(Player player)
        {
            if (player.x < mapWidth - 1)
            {
                player.x++;
                updatePlayerPosition(player);
                return true;
            }
            return false;
        }

        public bool MoveUp(Player player)
        {
            if (player.y > 0)
            {
                player.y--;
                updatePlayerPosition(player);
                return true;
            }
            return false;
        }

        public bool MoveDown(Player player)
        {
            if (player.y < mapHeight - 1)
            {
                player.y++;
                updatePlayerPosition(player);
                return true;
            }
            return false;
        }

    }
}
