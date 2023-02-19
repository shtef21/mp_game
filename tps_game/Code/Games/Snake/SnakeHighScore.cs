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
            List<SnakeHighScore> highScores = new List<SnakeHighScore>();

            // Unique names and users grouped
            HashSet<long> gameIds = new HashSet<long>();
            Dictionary<long, HashSet<long>> gameUsers = new Dictionary<long, HashSet<long>>();

            foreach (DataRow row in highScoresDb.Rows)
            {
                long gameId = (long)row["game_id"];
                gameIds.Add(gameId);

                if (gameUsers.ContainsKey(gameId) == false)
                {
                    gameUsers[gameId] = new HashSet<long>();
                }

                long userId = (long)row["user_id"];
                gameUsers[gameId].Add(userId);
            }

            /*
                string expression = "Date = '1/31/1979' or OrderID = 2";

      string sortOrder = "CompanyName ASC";
      DataRow[] foundRows;

      // Use the Select method to find all rows matching the filter.
      foundRows = table.Select(expression, sortOrder);
             */

            foreach (DataRow row in highScoresDb.Rows)
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
