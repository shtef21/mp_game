using System.Net.WebSockets;
using System.Linq;
using System.Timers;
using tps_game.Code.Old;

namespace tps_game.Code.Games
{
    public class SnakeGame : IGame
    {
        Dictionary<Guid, Snake> players = new Dictionary<Guid, Snake>();
        List<Snake> deadPlayers = new List<Snake>();
        (int, int)? foodCoordinate;
        public bool gameActive = true;

        public static int gameRefreshRateMs = 350;

        int mapHeight, mapWidth;

        public SnakeGame(int mapHeight, int mapWidth)
        {
            this.mapHeight = mapHeight;
            this.mapWidth = mapWidth;
            this.foodCoordinate = null;

            _ = StartGame();
        }

        async Task StartGame()
        {
            // Clear corpses
            deadPlayers.Clear();

            var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(gameRefreshRateMs));

            while(gameActive)
            {
                foreach (var kvPair in players)
                {
                    Guid guid = kvPair.Key;
                    Snake player = kvPair.Value;

                    (int, int) nextMove = player.GetNextCoordinate();
                    bool hitFood = nextMove == foodCoordinate;

                    bool blockAvailable = BlockIsFreeNextTurn(nextMove, player);

                    if (blockAvailable == false)
                    {
                        // Kill player
                        Snake deadPlayer = Snake.KillSnake(player);
                        deadPlayers.Add(deadPlayer);
                        players.Remove(guid);

                        // If currently no players alive
                        if (players.Count == 0)
                        {
                            TryTerminateGame();
                        }
                    }
                    else
                    {
                        player.Crawl(hitFood);

                        if (hitFood)
                        {
                            foodCoordinate = null;
                            TryGenerateFood();
                        }
                    }
                }
                BroadcastSummary();

                await timer.WaitForNextTickAsync();
            }
        }

        public string? OnPlayerConnected(Guid clientGuid, HttpContext context, WebSocket socket)
        {
            if (context.Request.Query.ContainsKey("username") == false)
            {
                // Username missing
                return "Username missing.";
            }

            string username = context.Request.Query["username"];

            if (players.Values.Where(player => player.username == username).Count() != 0)
            {
                // Username is taken
                return "Username taken.";
            }

            // Create player
            Snake player = new Snake(
                context,
                socket,
                username,
                mapHeight,
                mapWidth,
                (Static.Random.Next(0, mapHeight), Static.Random.Next(0, mapWidth - 6))
            );
            
            // Save player
            players.Add(clientGuid, player);

            // Try to create food
            TryGenerateFood();

            // Send new map data to all players
            BroadcastSummary();

            // New player connected, start the timer again
            if (gameActive == false)
            {
                gameActive = true;
                _ = StartGame();
            }
            
            return null;
        }

        public void OnPlayerDisconnected(Guid clientGuid)
        {
            players.Remove(clientGuid);

            // Send new map data to all players
            BroadcastSummary();

            // If currently no players alive
            if (players.Count == 0)
            {
                TryTerminateGame();
            }
        }

        void TryTerminateGame()
        {
            // If no new players after 10 seconds, stop the game
            _ = SetTimeout(10_000, () =>
            {
                if (players.Count == 0)
                {
                    // Set game to inactive
                    gameActive = false;
                    Console.WriteLine("No active players for 5 seconds. Stopping the game.");

                    // Broadcast summary for the last time to any connected corpses
                    BroadcastSummary();
                }
            });
        }

        async Task SetTimeout(int milliseconds, Action trigger)
        {
            await Task.Delay(milliseconds);
            trigger();
        }

        public void OnPlayerMessage(Guid clientGuid, string message)
        {
            if (message.Length > 0)
            {
                players[clientGuid].ChangeDirection(message[0]);
            }
        }

        public void OnPlayerMessage(Guid clientGuid, byte[] payload)
        {

        }

        private void BroadcastSummary()
        {
            var foodData = (object)null;
            if (foodCoordinate != null)
            {
                foodData = new
                {
                    y = foodCoordinate.Value.Item1,
                    x = foodCoordinate.Value.Item2
                };
            }

            var summary = Newtonsoft.Json.JsonConvert.SerializeObject(new {
                gameActive = gameActive,
                timestamp = DateTime.Now.Ticks,
                mapHeight,
                mapWidth,
                food = foodData,
                players = players.Values.Select(player => new {
                    player.username,
                    player.color,
                    positions = player.GetPositions().Select(yx => new
                    {
                        y = yx.Item1,
                        x = yx.Item2
                    }),
                    direction = player.GetDirection()
                }),
                corpses = deadPlayers.Select(deadSnake => new {
                    deadSnake.username,
                    deadSnake.color,
                    positions = deadSnake.GetPositions().Select(yx => new
                    {
                        y = yx.Item1,
                        x = yx.Item2
                    }),
                    direction = deadSnake.GetDirection()
                })
            });
            var summaryBytes = System.Text.Encoding.UTF8.GetBytes(summary);

            // Send info to players
            foreach(Snake player in players.Values)
            {
                _ = player.SendText(summaryBytes);
            }

            // If any dead players still connected, also send them the summary
            foreach(Snake deadPlayer in deadPlayers)
            {
                _ = deadPlayer.SendText(summaryBytes);
            }
        }

        void TryGenerateFood()
        {
            int attempts = 5;
            (int, int) tempCoord = (-1, -1);

            while (attempts > 0)
            {
                tempCoord = (Static.Random.Next(0, mapHeight), Static.Random.Next(0, mapWidth));

                int playersConflicting = players.Values.Where(snake => snake.GetPositions().Contains(tempCoord)).Count();
                if (playersConflicting == 0)
                {
                    foodCoordinate = tempCoord;
                    break;
                }
                --attempts;
            }
        }

        /// <summary>
        /// The block will be free next turn if it is inside map and does not collide with any player or corpse.
        /// </summary>
        /// <param name="yxCoord"> Coordinate to check. </param>
        /// <param name="currPlayer"> Send to get a proper perspective. </param>
        bool BlockIsFreeNextTurn((int, int) yxCoord, Snake? currPlayer)
        {
            // Outside of map
            if (yxCoord.Item1 < 0 || yxCoord.Item1 >= mapHeight || yxCoord.Item2 < 0 || yxCoord.Item2 >= mapWidth)
            {
                return false;
            }

            // Hit a corpse
            if (deadPlayers.Where(dp => dp.GetPositions().Contains((yxCoord.Item1, yxCoord.Item2))).Count() != 0)
            {
                return false;
            }

            // Hit a player
            foreach (Snake player in players.Values)
            {
                // Get head position next turn
                (int, int) headCoord = player.GetNextCoordinate();

                // Calculate tail position next turn
                (int, int)[] positions = player.GetPositions();
                bool hitsFood = headCoord == foodCoordinate;
                int tailIdx = hitsFood ? positions.Length - 1 : positions.Length - 2;

                // Check if body hit
                bool bodyHit = false;
                for (int i = 0; i < tailIdx; ++i)
                {
                    if (positions[i] == yxCoord)
                    {
                        bodyHit = true;
                    }
                }

                // If body hit, or player hit an enemy's head
                if (bodyHit || (headCoord == yxCoord && player != currPlayer))
                {
                    return false;
                }
            }

            return true;
        }

    }
}
