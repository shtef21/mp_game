using System.Net.WebSockets;

namespace tps_game.Code
{
    public class WebSocketHandler
    {
        static Game game = new Game(10, 10);

        private static List<Dictionary<string, object>> players = new List<Dictionary<string, object>>();

        public static async Task HandleWebSocketRequest(HttpContext context)
        {
            // Get the underlying socket
            using var socket = await context.WebSockets.AcceptWebSocketAsync();

            // Extract data
            string route = context.Request.Path.ToString();
            string username = context.Request.Query["username"];

            Console.WriteLine($"User \"{username}\" connected.");

            if (game.UsernameAvailable(username) == false)
            {
                Console.WriteLine($"Username {username} taken. Disconnecting...");
                return;
            }

            // Loop setup
            bool connectionAlive = true;
            List<byte> wsPayload = new List<byte>(1024 * 4); // 4 KB initial capacity
            byte[] messageReader = new byte[1024 * 4]; // Temp container

            // Initialize the player on the server
            var player = game.AddPlayer(socket, username);
            await player.SendData(new
            {
                type = "ID",
                ID = player.ID
            });

            // Send update to all players
            await game.SendUpdate();

            // Reading loop
            while (connectionAlive)
            {
                // Empty previous container
                wsPayload.Clear();

                // Message handler
                WebSocketReceiveResult? wsResponse;

                try
                {
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
                }
                catch (System.Exception e)
                {
                    Console.WriteLine($"I've got an exception for the player '{username}':\n -> {e.Message}");
                    break;
                }

                if (wsResponse.CloseStatus.HasValue)
                {
                    // Client has closed the connection for some reason
                    connectionAlive = false;
                }
                else if (wsResponse.MessageType == WebSocketMessageType.Text)
                {
                    // Process the message

                    string message = System.Text.Encoding.UTF8.GetString(wsPayload.ToArray());
                    Console.WriteLine($"Client {username} says \"{message}\"");

                    string key = message.ToUpper();
                    if ("WASD".Contains(key))
                    {
                        bool successfulMove = false;

                        if (key == "W")
                        {
                            Console.WriteLine($"Client {username} moved up");
                            successfulMove = game.MoveUp(player);
                        }
                        else if (key == "A")
                        {
                            Console.WriteLine($"Client {username} moved left");
                            successfulMove = game.MoveLeft(player);
                        }
                        else if (key == "S")
                        {
                            Console.WriteLine($"Client {username} moved down");
                            successfulMove = game.MoveDown(player);
                        }
                        else if (key == "D")
                        {
                            Console.WriteLine($"Client {username} moved right");
                            successfulMove = game.MoveRight(player);
                        }

                        if (successfulMove)
                        {
                            await game.SendUpdate();
                        }
                    }
                }
            }

            Console.WriteLine($"User \"{username}\" disconnected.");
            game.RemovePlayer(player);
            await game.SendUpdate();

        }

    }
}
