using System.Net.WebSockets;

namespace tps_game.Code
{
    public class Player
    {
        private static ulong idCounter = 0;

        public readonly ulong ID;
        public readonly string username;
        public readonly string color;
        public int x, y;

        public int movesLeft = 0;

        // Underlying WebSocket connection with the client
        WebSocket socket;

        public Player(WebSocket socket, string username)
        {
            this.ID = idCounter++;
            this.socket = socket;
            this.username = username;
            
            // Assign a random hex color to player
            this.color = string.Format("#{0:X6}", Game.random.Next(0x1000000));
        }

        public async Task SendData(object data)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            await socket.SendAsync(System.Text.Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task SendJSON(string json)
        {
            await socket.SendAsync(System.Text.Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
        }

    }
}
