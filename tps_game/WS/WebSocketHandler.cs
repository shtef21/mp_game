using System.Net.WebSockets;

namespace tps_game.WS
{
    public class WebSocketHandler
    {
        private static Random random = new Random();

        private static List<Dictionary<string, object>> players = new List<Dictionary<string, object>>();

        public static async Task HandleWebSocketRequest(HttpContext context)
        {
            // Get the underlying socket
            using var socket = await context.WebSockets.AcceptWebSocketAsync();

            // Extract data
            string route = context.Request.Path.ToString();
            string username = context.Request.Query["username"];

            Console.WriteLine($"User \"{username}\" connected.");

            if (getPlayer(username) != null)
            {
                Console.WriteLine($"Username {username} taken. Disconnecting...");
                return;
            }

            // Loop setup
            bool connectionAlive = true;
            List<byte> wsPayload = new List<byte>(1024 * 4); // 4 KB initial capacity
            byte[] messageReader = new byte[1024 * 4]; // Temp container

            // Initialize the player on the server
            var player = addPlayer(
                socket,
                username,
                (int)random.NextInt64(1, 10),
                (int)random.NextInt64(1, 10)
            );
            await broadcastSummary();
            //await playerConnected(username, posX, posY);

            // Reading loop
            while (connectionAlive)
            {
                // Empty previous container
                wsPayload.Clear();

                // Message handler
                WebSocketReceiveResult? wsResponse;

                // Read message in a loop
                do
                {
                    // Wait until client sends a message
                    wsResponse = await socket.ReceiveAsync(messageReader, CancellationToken.None);

                    // Save bytes
                    wsPayload.AddRange(new ArraySegment<byte>(messageReader, 0, wsResponse.Count));
                    Console.WriteLine($"Received {wsPayload.Count} bytes from {username}");
                }
                while (wsResponse.EndOfMessage == false);

                // Process the message
                if (wsResponse.MessageType == WebSocketMessageType.Text)
                {
                    string message = System.Text.Encoding.UTF8.GetString(wsPayload.ToArray());
                    Console.WriteLine($"Client {username} says \"{message}\"");

                    string key = message.ToUpper();
                    if ("WASD".Contains(key))
                    {
                        if (key == "W")
                        {
                            Console.WriteLine($"Client {username} moved up");
                            player["posY"] = (int)player["posY"] - 1;
                        }
                        else if (key == "A")
                        {
                            Console.WriteLine($"Client {username} moved left");
                            player["posX"] = (int)player["posX"] - 1;
                        }
                        else if (key == "S")
                        {
                            Console.WriteLine($"Client {username} moved down");
                            player["posY"] = (int)player["posY"] + 1;
                        }
                        else if (key == "D")
                        {
                            Console.WriteLine($"Client {username} moved right");
                            player["posX"] = (int)player["posX"] + 1;
                        }

                        await broadcastSummary();
                    }
                }
                else if (wsResponse.MessageType == WebSocketMessageType.Close)
                {
                    connectionAlive = false;
                }
            }

            Console.WriteLine($"User \"{username}\" disconnected.");
            removePlayer(player);
            await broadcastSummary();

        }


        private static async Task send(WebSocket? socket, object data)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            await socket.SendAsync(System.Text.Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private static async Task send(WebSocket? socket, string json)
        {
            await socket.SendAsync(System.Text.Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private static async Task broadcastSummary()
        {
            var playerPositions = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                type = "playerPositions",
                list = summarizePlayers()
            });

            for (int i = 0; i < players.Count; ++i)
            {
                var socket = (WebSocket) players[i]["socket"];
                await send(socket, playerPositions);
            }
        }

        private static Dictionary<string, object> getPlayer(string username)
        {
            var search = players.Where(player => player["username"].ToString() == username).ToList();
            if (search.Count > 0)
            {
                return search[0];
            }
            else
            {
                return null;
            }
        }

        // Summarize players and their positions
        private static List<Dictionary<string, object>> summarizePlayers()
        {
            return players.Select(player =>
            {
                var summary = new Dictionary<string, object>()
                {
                    { "username", player["username"] },
                    { "posX", player["posX"] },
                    { "posY", player["posY"] }
                };
                return summary;
            }).ToList();
        }

        private static Dictionary<string, object> addPlayer(WebSocket socket, string username, int posX, int posY)
        {
            var player = new Dictionary<string, object>();
            player["socket"] = socket;
            player["username"] = username;
            player["posX"] = posX;
            player["posY"] = posY;
            players.Add(player);
            return player;
        }

        private static void removePlayer(Dictionary<string, object> player)
        {
            players.Remove(player);
        }

        private static async Task playerConnected(string username, int posX, int posY)
        {
            // First notify everyone about this player
            var notify = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                type = "new-enemy",
                posX,
                posY
            });

            // Notify other players about the newcomer
            for (int i = 0; i < players.Count; ++i)
            {
                if (players[i]["username"].ToString() != username)
                {
                    var socket = (WebSocket)players[i]["socket"];
                    await send(socket, notify);
                }
            }
        }


    }
}
