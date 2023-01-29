using System.Net.WebSockets;
using System.Numerics;

namespace tps_game.Code
{
    public class SnakePlayer
    {
        public string username;
        public List<(int, int)> positions;
        public string color;

        // Underlying WebSocket connection with the client
        WebSocket socket;

        public SnakePlayer(WebSocket socket, string username)
        {
            this.socket = socket;
            this.username = username;

            positions = new List<(int, int)>();

            // Assign a random hex color to player
            this.color = string.Format("#{0:X6}", Game.Random.Next(0x1000000));
        }

        // Generate random coordinates for player
        public void GenerateRandomPosition(int mapWidth, int mapHeight, int snakeLength = 3)
        {
            int startY, startX;
            startY = WebSocketRouter.Random.Next(0, mapHeight);
            startX = WebSocketRouter.Random.Next(snakeLength - 1, mapWidth);
            positions.Clear();
            for (int i = 0; i < snakeLength; ++i)
            {
                positions.Add((startY, startX + snakeLength - i));
            }
        }

        /// <summary>
        /// Encode data to JSON and send it to the underlying web socket.
        /// </summary>
        /// <param name="data"> Data that needs to be sent. </param>
        /// <returns> Awaitable Task object. </returns>
        public async Task SendJSON(object data)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            await socket.SendAsync(System.Text.Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        /// <summary>
        /// Send JSON data to the underlying web socket.
        /// </summary>
        /// <param name="json"> Data that needs to be sent. </param>
        /// <returns> Awaitable Task object. </returns>
        public async Task SendJSON(string json)
        {
            await socket.SendAsync(System.Text.Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
        }

    }
}
