using System.Net.WebSockets;
using System.Numerics;

namespace tps_game.Code.Old
{
    public class Player
    {
        private static ulong idCounter = 0;

        // Obsolete: should remove
        public readonly ulong ID;

        public string username;
        public int x, y;
        public string color;

        public int movesLeft = 0;

        // Underlying WebSocket connection with the client
        WebSocket socket;

        public Player(WebSocket socket, string username)
        {
            this.ID = idCounter++;
            this.socket = socket;
            this.username = username;
            
            // Assign a random hex color to player
            this.color = string.Format("#{0:X6}", Game.Random.Next(0x1000000));
        }

        // Generate random coordinates for player
        public void GenerateCoordinates(int mapWidth, int mapHeight)
        {
            this.x = Game.Random.Next(0, mapWidth);
            this.y = Game.Random.Next(0, mapHeight);
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
