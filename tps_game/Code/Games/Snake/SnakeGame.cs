using System.Net.WebSockets;
using System.Linq;
using System.Timers;

namespace tps_game.Code.Games
{
    public class SnakeGame : IGame
    {
        Dictionary<Guid, Snake> players = new Dictionary<Guid, Snake>();
        (int, int)? food;
        public bool gameActive = true;

        int mapHeight, mapWidth;

        public SnakeGame(int mapHeight, int mapWidth)
        {
            this.mapHeight = mapHeight;
            this.mapWidth = mapWidth;
            this.food = null;

            StartTimer();
        }

        async Task StartTimer()
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

            while(gameActive)
            {
                Console.WriteLine("Timer...");
                foreach (Snake player in players.Values)
                {
                    player.Crawl();
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
                (mapHeight / 2, mapWidth / 2)
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
                StartTimer();
            }
            
            return null;
        }

        public void OnPlayerDisconnected(Guid clientGuid)
        {
            players.Remove(clientGuid);

            // Send new map data to all players
            BroadcastSummary();

            // No players online, stop updates
            if (players.Count == 0)
            {
                gameActive = false;
            }
        }

        public void OnPlayerMessage(Guid clientGuid, string message)
        {

        }

        public void OnPlayerMessage(Guid clientGuid, byte[] payload)
        {

        }

        private void BroadcastSummary()
        {
            var foodData = (object)null;
            if (food != null)
            {
                foodData = new
                {
                    y = food.Value.Item1,
                    x = food.Value.Item2
                };
            }

            var summary = Newtonsoft.Json.JsonConvert.SerializeObject(new {
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
                })
            });
            var summaryBytes = System.Text.Encoding.UTF8.GetBytes(summary);

            foreach(Snake player in players.Values)
            {
                _ = player.SendText(summaryBytes);
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
                    food = tempCoord;
                    break;
                }
                --attempts;
            }
        }

    }
}
