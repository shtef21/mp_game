using System.Data;

namespace tps_game.Code.Games
{
    class SnakeHighScore
    {
        public long gameNum { get; private set; }
        public string playerUsername { get; private set; }
        public long playerLength { get; private set; }
        public string nextMove { get; private set; }
        public string timestampUtc { get; private set; }

        SnakeHighScore(long gameNum, string playerUsername, long playerLength, string nextMove, string timestampUtc)
        {
            this.gameNum = gameNum;
            this.playerUsername = playerUsername;
            this.playerLength = playerLength;
            this.nextMove = nextMove;
            this.timestampUtc = timestampUtc;
        }

        public static SnakeHighScore[] FetchHighScores()
        {
            long gameCounter = 1;
            Dictionary<string, long> gameCountersDict = new Dictionary<string, long>();

            DataTable highScoresDb = Database.SnakeGetHighScores();
            List<SnakeHighScore> highScores = new List<SnakeHighScore>(highScoresDb.Rows.Count);

            foreach(DataRow row in highScoresDb.Rows)
            {
                string gameGuid = (string) row["game_guid"];
                if (gameCountersDict.ContainsKey(gameGuid) == false)
                {
                    gameCountersDict.Add(gameGuid, gameCounter++);
                }

                highScores.Add(new SnakeHighScore(
                    gameCountersDict[gameGuid],
                    (string) row["username"],
                    (long)row["snake_length"],
                    (string)row["next_move"],
                    (string)row["timestamp_utc"]
                    ));
            }

            return highScores.ToArray();
        }
    }
}
