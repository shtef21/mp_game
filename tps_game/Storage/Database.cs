using System.Data;
using System.Globalization;
using tps_game.Code.Games;
using shtef21.SQLite;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace tps_game
{

    // Database wrapper class
    public class Database
    {
        // When project loads, create a database handler
        static DB db = DB.Create("shtef21_sqlite.db", tps_game.Code.Static.dbDeleteSetting);

        #region usersTable
        public static DBTable usersTable = new DBTable("users")
            .AddColumn(DBColumn.MakeIdColumn())
            .AddColumn(DBColumn.MakeTextColumn("guid", notNull: true))
            .AddColumn(DBColumn.MakeUniqueTextColumn("username"))
            .AddColumn(DBColumn.MakeUniqueTextColumn("email"))
            .AddColumn(DBColumn.MakeTextColumn("hashed_pw", notNull: true))
            .AddColumn(DBColumn.MakeTextColumn("login_token"))
            .AddColumn(DBColumn.MakeTimestampColumn("login_token_expire_utc")) // YYYY-MM-DD HH:MM:SS
            .AddColumn(new DBColumn(
                name: "email_confirmed",
                type: DBColumnType.TEXT,
                // y/n
                defaultStr: "n"
                ))
            .AddColumn(DBColumn.MakeTimestampColumn("created_utc", setDefault: true));
        #endregion

        #region snakeGamesTable
        public static DBTable snakeGamesTable = new DBTable("snake_games")
            .AddColumn(DBColumn.MakeIdColumn())
            .AddColumn(DBColumn.MakeTextColumn("guid", notNull: true))
            .AddColumn(DBColumn.MakeTimestampColumn("finished_utc"))
            .AddColumn(DBColumn.MakeTimestampColumn("created_utc", setDefault: true));
        #endregion

        #region snakeGameLogs
        // Map logs
        //  snake_positions:
        //      format: [(y1,x1),(y2,x2),...,(yN,xN)]
        //          where N=snake_length
        public static DBTable snakeGameLogs = new DBTable("snake_game_logs")
            .AddColumn(DBColumn.MakeIdColumn())
            .AddColumn(DBColumn.MakeFKColumn("game_id", "snake_games", "id"))
            .AddColumn(DBColumn.MakeFKColumn("user_id", "users", "id"))
            .AddColumn(DBColumn.MakeIntegerColumn("snake_length"))
            .AddColumn(DBColumn.MakeTextColumn("snake_positions"))
            .AddColumn(DBColumn.MakeTextColumn("next_move"))
            .AddColumn(DBColumn.MakeTimestampColumn("created_utc", setDefault: true));
        #endregion

        // Reset the database
        public static void ResetDB()
        {
            File.Delete(Properties.Resources.dbPath);
            InitDB();
        }

        // Initialize database
        public static void InitDB()
        {
            // Running the first query will create a .db file

            // Create tables
            db.CreateTable(usersTable);
            db.CreateTable(snakeGamesTable);
            db.CreateTable(snakeGameLogs);

            // Add first user
            string username = Properties.Resources.exampleUser;
            string email = Properties.Resources.exampleEmail;
            string password = Properties.Resources.examplePassword;
            AddUser(username, email, password);
        }

        public static string FormatDateTime(DateTime date)
        {
            return date.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        }

        #region Users

        public static bool AddUser(string username, string email, string password)
        {
            long? count = db.SelectScalar<long>(
                new SQLSelect(usersTable.tableName)
                .Columns("count(*)")
                .WhereEq("username", username)
            );

            if (count != null && count > 0)
            {
                // User already exists
                return false;
            }

            Guid userGuid = Guid.NewGuid();
            string hashedPassword = shtef21.Crypto.Sha512.Generate(password);

            SQLInsert insertCommand = new SQLInsert(usersTable.tableName)
                .Columns(
                    "guid", "username", "email", "hashed_pw"
                )
                .Values(
                    userGuid, username, email, hashedPassword
                );

            db.InsertRow(insertCommand);
            Console.WriteLine($"Added user \"{username}\" to DB. Total number of users: {(count ?? 0) + 1}");

            return true;
        }

        // Login the user and return his login token
        public static string? LoginUser(string username, string password, bool rememberMe)
        {
            string hashedPw = shtef21.Crypto.Sha512.Generate(password);

            DataTable findUser = db.Select(new SQLSelect(usersTable.tableName)
                .Columns("username", "hashed_pw")
                .WhereEq(
                    ("username", username),
                    ("hashed_pw", hashedPw)
                ));

            if (findUser.Rows.Count == 0)
            {
                return null;
            }
            else
            {
                // Generate and save token
                string token = Guid.NewGuid().ToString();

                // Calculate token expiration date
                int expireDays = rememberMe ? 7 : 1;
                DateTime tokenExpire = DateTime.UtcNow.AddDays(expireDays);
                string expireFormatted = FormatDateTime(tokenExpire);

                db.Update(new SQLUpdate(usersTable.tableName)
                    .ColumnUpdate(
                        ("login_token", token),
                        ("login_token_expire_utc", expireFormatted)
                    )
                    .WhereEq("username", username)
                );
                return token;
            }
        }

        public static string? GetUsername(string loginToken)
        {
            // If token found & valid
            if (CheckToken(loginToken) == true)
            {
                string? username = db.SelectScalar<string>(new SQLSelect(usersTable.tableName)
                    .Columns("username")
                    .WhereEq("login_token", loginToken));
                return username;
            }
            return null;
        }

        public static bool CheckToken(string token)
        {
            // login_token_expire_utc
            DataTable findUser = db.Select(
                new SQLSelect(usersTable.tableName)
                .Columns("login_token", "login_token_expire_utc")
                .WhereEq("login_token", token)
            );

            // If token found
            if (findUser.Rows.Count == 1)
            {
                string? expires = findUser.Rows[0]["login_token_expire_utc"].ToString();

                // If token not expired
                if (expires != null && DateTime.Parse(expires) > DateTime.UtcNow)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Snake

        // Create a game, insert it to DB and return its ID
        public static long? CreateGame()
        {
            // All columns: id, guid, finished_utc, created_utc
            var insertCommand = new SQLInsert(snakeGamesTable.tableName)
                .Columns("guid")
                .Values(Guid.NewGuid());

            long? gameId = db.InsertRow(insertCommand);
            return gameId;
        }

        // Set date when game was finished
        public static void FinishGame(long gameId)
        {
            var updateCommand = new SQLUpdate(snakeGamesTable.tableName)
                .ColumnUpdate("finished_utc", FormatDateTime(DateTime.UtcNow))
                .WhereEq("id", gameId);

            db.Update(updateCommand);
        }

        public static async Task SnakeAddMapLog(long gameId, Dictionary<Guid, Snake> players)
        {
            string timestamp = FormatDateTime(DateTime.UtcNow);
            DataTable users = db.Select(new SQLSelect(usersTable.tableName));

            List<Task> tasks = new List<Task>(players.Count);

            foreach (Snake player in players.Values)
            {
                tasks.Add(new Task(delegate
                    {
                        // Prepare custom array of positions in format { y: y, x: x }
                        string positionsStringified = Newtonsoft.Json.JsonConvert.SerializeObject(
                            player.GetPositions().Select((yx) => new { y=yx.Item1, x=yx.Item2 })
                        );

                        var sqlInsert = new SQLInsert(snakeGameLogs.tableName)
                            .Columns(
                                "game_id",
                                "user_id",
                                "snake_length",
                                "snake_positions",
                                "next_move",
                                "created_utc"
                            )
                            .Values(
                                gameId,
                                users.Select($" username = '{player.username.Replace("'", "''")}' ")[0]["id"],
                                player.GetPositions().Length,
                                positionsStringified,
                                player.GetDirection(),
                                timestamp
                            );

                        db.InsertRow(sqlInsert);
                    })
                );
                tasks.Last().Start();
            }

            await Task.WhenAll(tasks.ToArray());
        }

        public static DataTable SnakeGetHighScores()
        {
            string sql = $@"

            ";
            DataTable highScores = db.SelectCustom(sql);
            return highScores;
        }

        #endregion

    }

}
