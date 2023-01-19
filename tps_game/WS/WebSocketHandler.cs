using System.Net.WebSockets;

namespace tps_game.WS
{
    public class WebSocketHandler
    {
        private static Random random = new Random();

        private static List<Dictionary<string, string>> players;

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

            // Send initial position
            int posX = (int)random.NextInt64(1, 10);
            int posY = (int)random.NextInt64(1, 10);
            await send(socket, new { type="initial", posX, posY });

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
                    if (key == "W")
                    {
                        Console.WriteLine($"Client {username} moved up");
                        posY--;
                    }
                    else if (key == "A")
                    {
                        Console.WriteLine($"Client {username} moved left");
                        posX--;
                    }
                    else if (key == "S")
                    {
                        Console.WriteLine($"Client {username} moved down");
                        posY++;
                    }
                    else if (key == "D")
                    {
                        Console.WriteLine($"Client {username} moved right");
                        posX++;
                    }
                }
                else if (wsResponse.MessageType == WebSocketMessageType.Close)
                {
                    connectionAlive = false;
                }
            }

            Console.WriteLine($"User \"{username}\" disconnected.");

        }


        private static async Task send(WebSocket? socket, object data)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            await socket.SendAsync(System.Text.Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private static Dictionary<string, string> getPlayer(string username)
        {
            List<Dictionary<string, string>> search = players.Where(player => player["username"].ToString() == username).ToList();
            if (search.Count > 0)
            {
                return search[0];
            }
            else
            {
                return null;
            }
        }

        private static void addPlayer(string username, int posX, int posY)
        {
            Dictionary<string, string> player = new Dictionary<string, string>();
            player["username"] = username;
            player["posX"] = posX;
            player["posY"] = posY;
            players.Add(player);
        }

    }
}
