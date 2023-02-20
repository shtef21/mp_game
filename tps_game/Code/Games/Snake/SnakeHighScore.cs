using System.Data;

namespace tps_game.Code.Games
{
    class SnakeHighScore
    {
        public long gameNum { get; private set; }
        public string playerUsername { get; private set; }
        public long playerLength { get; private set; }
        public string timestampUtc { get; private set; }

        SnakeHighScore(long gameNum, string playerUsername, long playerLength, string timestampUtc)
        {
            this.gameNum = gameNum;
            this.playerUsername = playerUsername;
            this.playerLength = playerLength;
            this.timestampUtc = timestampUtc;
        }

        public static SnakeHighScore[] FetchHighScores()
        {
            /*
                snake_game_logs.id,
                snake_game_logs.game_id,
                snake_game_logs.user_id,
                users.username,
    
                max(snake_game_logs.snake_length) snake_length,
                snake_game_logs.created_utc
             */

            DataTable highScoresDb = Database.SnakeGetHighScores();
            List<SnakeHighScore> highScores = new List<SnakeHighScore>(highScoresDb.Rows.Count);

            foreach (DataRow row in highScoresDb.Rows)
            {
                highScores.Add(new SnakeHighScore(
                    (long)row["id"],
                    (string)row["username"],
                    (long)row["snake_length"],
                    (string)row["created_utc"]
                ));
            }

            return highScores.ToArray();
        }
    }
}
