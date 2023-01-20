using System.Net.WebSockets;

namespace tps_game.Code
{
    public class Player
    {
        private static ulong idCounter = 0;

        public readonly ulong ID;
        public readonly string username;
        public int x, y;

        // Underlying WebSocket connection with the client
        WebSocket socket;

        public Player(WebSocket socket, string username)
        {
            this.ID = idCounter++;
            this.socket = socket;
            this.username = username;
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
