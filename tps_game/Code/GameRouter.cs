using System.Net.WebSockets;
using tps_game.Code.Games;

namespace tps_game.Code
{
    public class GameRouter
    {
        /// <summary>
        /// If true, log client events.
        /// </summary>
#if DEBUG
        public static readonly bool debugMode = true;
#else
        public static readonly bool debugMode = false;
#endif

        public static async Task HandleWebSocketRequest(HttpContext context)
        {
            // Accept client's underlyig web socket and assign it a GUID
            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            Guid clientGuid = Guid.NewGuid();

            if (debugMode)
            {
                Log(clientGuid, "Connected!");
            }

            // Connection route
            string connectionRoute = context.Request.Path.ToString().ToLower().Trim('/');

            // Game setup
            IGame? game = FindGame(connectionRoute);
            bool connectionAlive = false;

            if (game != null)
            {
                string? errorMessage = game.OnPlayerConnected(clientGuid, context, socket);
                connectionAlive = errorMessage == null;

                if (errorMessage != null)
                {
                    LogError(clientGuid, errorMessage);
                }
            }
            if (game == null && debugMode)
            {
                LogError(clientGuid, $"Cannot find a game for route \"{connectionRoute}\"");
            }

            // Loop setup
            List<byte> wsPayload = new List<byte>(1024 * 4); // 4 KB initial capacity
            byte[] messageReader = new byte[1024 * 4]; // Temp container

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

                        // If byte count equals 0, then this is most likely a control message
                        if (debugMode && wsResponse.Count > 0)
                        {
                            Log(clientGuid, $"Received {wsPayload.Count} bytes.");
                        }
                    }
                    while (wsResponse.EndOfMessage == false);
                }
                catch (System.Exception e)
                {
                    if (debugMode)
                    {
                        LogError(clientGuid, $"Error '${e.Message}'");
                    }
                    break;
                }

                if (wsResponse.CloseStatus.HasValue)
                {
                    // Client has closed the connection for some reason
                    connectionAlive = false;
                }
                else if (wsResponse.MessageType == WebSocketMessageType.Binary)
                {
                    // Process the message
                    byte[] payload = wsPayload.ToArray();
                    
                    if (debugMode)
                    {
                        Log(clientGuid, $"${payload.Length} bytes received.");
                    }
                    game.OnPlayerMessage(clientGuid, payload);
                }
                else if (wsResponse.MessageType == WebSocketMessageType.Text)
                {
                    // Process the message
                    string message = System.Text.Encoding.UTF8.GetString(wsPayload.ToArray());

                    if (debugMode)
                    {
                        Log(clientGuid, $"\"{message}");
                    }
                    game.OnPlayerMessage(clientGuid, message);
                }
            }

            if (debugMode)
            {
                Log(clientGuid, "Disconnected.");
            }
            if (game != null)
            {
                game.OnPlayerDisconnected(clientGuid);
            }
        }

        private static void Log(Guid clientGuid, string text)
        {
            ConsoleColor previousColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(clientGuid);
            Console.ForegroundColor = previousColor;
            Console.WriteLine(": " + text);
        }

        private static void LogError(Guid clientGuid, string text)
        {
            ConsoleColor previousColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(clientGuid);
            Console.ForegroundColor = previousColor;
            Console.WriteLine(": " + text);
        }


        static SnakeGame snakeGame = new SnakeGame(10, 10);
        private static IGame? FindGame(string connectionRoute)
        {
            if (connectionRoute == "game/snake")
            {
                return snakeGame;
            }
            return null;
        }

    }
}
