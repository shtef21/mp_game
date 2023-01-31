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
        public readonly HttpContext? httpContext;
        WebSocket webSocket;

        List<(int, int)> positions;

        SnakeDirection direction;
        SnakeDirection prevMoveDirection;
        readonly int mapHeight;
        readonly int mapWidth;

        public bool alive;

        public Snake (HttpContext httpContext, WebSocket webSocket, string username, int mapHeight, int mapWidth, (int, int) spawnCoordinate)
        {
            this.httpContext = httpContext;
            this.webSocket = webSocket;
            this.username = username;
            this.mapHeight = mapHeight;
            this.mapWidth = mapWidth;
            this.alive = true;
            this.color = string.Format("#{0:X6}", Static.Random.Next(0x1000000));
            this.direction = SnakeDirection.Right;
            this.prevMoveDirection = SnakeDirection.Right;
            this.positions = new List<(int, int)>() {
                (spawnCoordinate.Item1, spawnCoordinate.Item2 + 2),
                (spawnCoordinate.Item1, spawnCoordinate.Item2 + 1),
                (spawnCoordinate.Item1, spawnCoordinate.Item2)
            };
        }

        Snake (Snake deadSnake)
        {
            // Copy coordinates from the dead snake
            this.httpContext = deadSnake.httpContext;
            this.webSocket = deadSnake.webSocket;
            this.username = deadSnake.username;
            this.mapHeight = deadSnake.mapHeight;
            this.mapWidth = deadSnake.mapWidth;
            this.alive = false;
            this.color = deadSnake.color;
            this.direction = deadSnake.direction;
            this.prevMoveDirection = deadSnake.prevMoveDirection;
            this.positions = deadSnake.positions;
        }

        public static Snake KillSnake(Snake snake)
        {
            return new Snake(snake);
        }

        // Move in the direction indicated by 'direction'
        public void Crawl(bool eatFood = false)
        {
            // Get next position
            (int, int) nextPosition = GetNextCoordinate();

            if (nextPosition.Item1 < 0 || nextPosition.Item1 > mapHeight - 1 ||
                nextPosition.Item2 < 0 || nextPosition.Item2 > mapWidth - 1)
            {
                // Cannot go past map edge
                return;
            }

            // Set previous direction
            prevMoveDirection = direction;

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
        public void ChangeDirection(char input)
        {
            input = char.ToLower(input);
            switch(input)
            {
                case 'w':
                    if (prevMoveDirection != SnakeDirection.Down) {
                        prevMoveDirection = direction;
                        direction = SnakeDirection.Up;
                    }
                    break;
                case 'a':
                    if (prevMoveDirection != SnakeDirection.Right) {
                        prevMoveDirection = direction;
                        direction = SnakeDirection.Left;
                    }
                    break;
                case 's':
                    if (prevMoveDirection != SnakeDirection.Up) {
                        prevMoveDirection = direction;
                        direction = SnakeDirection.Down;
                    }
                    break;
                case 'd':
                    if (prevMoveDirection != SnakeDirection.Left) {
                        prevMoveDirection = direction;
                        direction = SnakeDirection.Right;
                    }
                    break;
                default:
                    break;
            }
        }

        // Get next coordinate after player moves in current direction
        public (int, int) GetNextCoordinate()
        {
            int yDiff = 0, xDiff = 0;

            if (direction == SnakeDirection.Up)
            {
                yDiff = -1;
            }
            else if (direction == SnakeDirection.Down)
            {
                yDiff = +1;
            }

            if (direction == SnakeDirection.Left)
            {
                xDiff = -1;
            }
            else if (direction == SnakeDirection.Right)
            {
                xDiff = +1;
            }

            (int, int) nextPosition = (
                positions[0].Item1 + yDiff,
                positions[0].Item2 + xDiff
            );
            return nextPosition;
        }

        public char? GetDirection()
        {
            switch(direction)
            {
                case SnakeDirection.Up:
                    return 'w';
                case SnakeDirection.Down:
                    return 's';
                case SnakeDirection.Left:
                    return 'a';
                case SnakeDirection.Right:
                    return 'd';
                default:
                    return null;
            }
        }

        public (int, int)[] GetPositions() {
            return positions.ToArray();
        }

        public async Task SendText(byte[] textBytes)
        {
            if (webSocket.State == WebSocketState.Open)
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
}
