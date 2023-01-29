using System.Net.WebSockets;

namespace tps_game.Code.Games
{
    public class SnakeGame : IGame
    {
        // Clients saved using their Guid and defined by their HttpContext and Player objects
        Dictionary<Guid, (HttpContext, SnakePlayer)> clients = new Dictionary<Guid, (HttpContext, SnakePlayer)>();

        // Players saved using their usernames and Player objects
        Dictionary<string, SnakePlayer> players = new Dictionary<string, SnakePlayer>();

        SnakeMap map;

        public SnakeGame(int width, int height)
        {
            map = new SnakeMap(width, height);
        }

        public string? OnPlayerConnected(Guid clientGuid, HttpContext context, WebSocket socket)
        {
            if (context.Request.Query.ContainsKey("username") == false)
            {
                // Username missing
                return "Username missing.";
            }

            string username = context.Request.Query["username"];
            username = username.Replace(" ", "_").Replace("-", "_"); // Filter out characters that are not allowed

            if (players.ContainsKey(username))
            {
                // Username is taken
                return "Username taken.";
            }

            // Create player
            SnakePlayer player = new SnakePlayer(socket, username);
            player.GenerateRandomPosition(map.width, map.height);
            
            // Save player
            clients.Add(clientGuid, (context, player));
            players.Add(username, player);

            // Add player to map
            map.AddPlayer(player);

            // Send new map data to all players
            BroadcastSummary();
            
            return null;
        }

        public void OnPlayerDisconnected(Guid clientGuid)
        {
            SnakePlayer player = clients[clientGuid].Item2;
            players.Remove(player.username);
            clients.Remove(clientGuid);
            map.RemovePlayer(player);
        }

        public void OnPlayerMessage(string message)
        {

        }

        public void OnPlayerMessage(byte[] payload)
        {

        }

        private void BroadcastSummary()
        {
            // Prepare JSON summary
            string update = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                type = "summary",
                mapHeight = map.height,
                mapWidth = map.width,
                map = map.Summarize(),
                players = players.Values.Select(player =>
                {
                    return new
                    {
                        username = player.username,
                        color = player.color,
                        length = player.positions.Count
                    };
                })
            });

            // Send JSON to all players (async)
            foreach(SnakePlayer player in players.Values)
            {
                _ = player.SendJSON(update);
            }
        }

    }
}
