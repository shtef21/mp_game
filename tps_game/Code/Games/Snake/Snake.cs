using System.Net.WebSockets;

namespace tps_game.Code.Games
{
    public enum SnakeDirection
    {
        Up,
        Right,
        Left,
        Down
    }

    public class Snake
    {
        public readonly string username;
        public readonly string color;
        public readonly HttpContext httpContext;
        WebSocket webSocket;

        List<(int, int)> positions;

        SnakeDirection direction;
        readonly int mapHeight;
        readonly int mapWidth;

        public Snake (HttpContext httpContext, WebSocket webSocket, string username, int mapHeight, int mapWidth, (int, int) spawnCoordinate)
        {
            this.httpContext = httpContext;
            this.webSocket = webSocket;
            this.username = username;
            this.mapHeight = mapHeight;
            this.mapWidth = mapWidth;
            this.color = string.Format("#{0:X6}", Static.Random.Next(0x1000000));
            this.direction = SnakeDirection.Right;
            this.positions = new List<(int, int)>() {
                (spawnCoordinate.Item1, spawnCoordinate.Item2 + 2),
                (spawnCoordinate.Item1, spawnCoordinate.Item2 + 1),
                (spawnCoordinate.Item1, spawnCoordinate.Item2)
            };
        }

        // Move in the direction indicated by 'direction'
        public void Crawl(bool eatFood = false)
        {
            // Calculate next position

            int yDiff = 0, xDiff = 0;

            if (direction == SnakeDirection.Up) {
                yDiff = -1;
            }
            else if (direction == SnakeDirection.Down) {
                yDiff = +1;
            }

            if (direction == SnakeDirection.Left) {
                xDiff = -1;
            }
            else if (direction == SnakeDirection.Right) {
                xDiff = +1;
            }

            (int, int) nextPosition = (
                positions[0].Item1 + yDiff,
                positions[0].Item2 + xDiff
            );

            if (nextPosition.Item1 < 0 || nextPosition.Item1 > mapHeight - 1 ||
                nextPosition.Item2 < 0 || nextPosition.Item2 > mapWidth - 1)
            {
                // Cannot go past map edge
                return;
            }

            // Save tail in case the snake should be extended
            (int, int) previousTailPosition = positions[positions.Count - 1];

            // Move snake
            for (int i = positions.Count - 1; i > 0; --i)
            {
                positions[i] = positions[i - 1];
            }
            positions[0] = nextPosition;

            // If food eaten, extend the snake (to previous tail position)
            if (eatFood)
            {
                positions.Add(previousTailPosition);
            }
        }

        // Change movement direction
        public void ChangeDirection(string input)
        {
            input = input.ToLower();
            switch(input)
            {
                case "w":
                    if (direction != SnakeDirection.Down) {
                        direction = SnakeDirection.Up;
                    }
                    break;
                case "a":
                    if (direction != SnakeDirection.Right) {
                        direction = SnakeDirection.Left;
                    }
                    break;
                case "s":
                    if (direction != SnakeDirection.Up) {
                        direction = SnakeDirection.Down;
                    }
                    break;
                case "d":
                    if (direction != SnakeDirection.Left) {
                        direction = SnakeDirection.Right;
                    }
                    break;
                default:
                    break;
            }
        }

        public string GetDirection()
        {
            switch(direction)
            {
                case SnakeDirection.Up:
                    return "w";
                case SnakeDirection.Down:
                    return "s";
                case SnakeDirection.Left:
                    return "a";
                case SnakeDirection.Right:
                    return "d";
                default:
                    return string.Empty;
            }
        }

        public (int, int)[] GetPositions() {
            return positions.ToArray();
        }

        public async Task SendText(byte[] textBytes)
        {
            await webSocket.SendAsync(
                textBytes,
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }

    }
}
