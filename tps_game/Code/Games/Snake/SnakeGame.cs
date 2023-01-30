using System.Net.WebSockets;

namespace tps_game.Code.Games
{
    public class SnakeGame : IGame
    {
        Dictionary<Guid, Snake> players = new Dictionary<Guid, Snake>();

        int mapHeight, mapWidth;

        public SnakeGame(int mapHeight, int mapWidth)
        {
            this.mapHeight = mapHeight;
            this.mapWidth = mapWidth;
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

            // Send new map data to all players
            BroadcastSummary();
            
            return null;
        }

        public void OnPlayerDisconnected(Guid clientGuid)
        {
            players.Remove(clientGuid);
        }

        public void OnPlayerMessage(Guid clientGuid, string message)
        {

        }

        public void OnPlayerMessage(Guid clientGuid, byte[] payload)
        {

        }

        private void BroadcastSummary()
        {
            var summary = Newtonsoft.Json.JsonConvert.SerializeObject(new {
                players = players.Values.Select(player => new {
                    player.username,
                    player.color,
                    positions = player.GetPositions()
                })
            });
            var summaryBytes = System.Text.Encoding.UTF8.GetBytes(summary);

            foreach(Snake player in players.Values)
            {
                _ = player.SendText(summaryBytes);
            }
        }

    }
}
