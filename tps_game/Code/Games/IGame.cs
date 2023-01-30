using System.Net.WebSockets;

namespace tps_game.Code.Games
{
    public interface IGame
    {

        /// <summary>
        /// A function that handles new players.
        /// </summary>
        /// <param name="guid"> Client GUID. </param>
        /// <param name="context"> HttpContext behind player client. Useful for getting things such as connection route, query parameters etc. </param>
        /// <param name="socket"> Underlying web socket connection. </param>
        /// <returns> Error message for invalid connections, or null for valid ones. </returns>
        public string? OnPlayerConnected(Guid clientGuid, HttpContext context, WebSocket socket);

        /// <summary>
        /// A function that handles player disconnects.
        /// </summary>
        /// <param name="guid"> Client GUID. </param>
        public void OnPlayerDisconnected(Guid clientGuid);

        /// <summary>
        /// A function that handles player text messages.
        /// </summary>
        /// <param name="guid"> Client GUID. </param>
        /// <param name="message"> Client text message string. </param>
        public void OnPlayerMessage(Guid clientGuid, string message);

        /// <summary>
        /// A function that handles player binary messages.
        /// </summary>
        /// <param name="payload"> Client text message bytes. </param>
        public void OnPlayerMessage(Guid clientGuid, byte[] payload);

    }
}
