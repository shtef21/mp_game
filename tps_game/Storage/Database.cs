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
            .AddColumn(DBColumn.MakeTimestampColumn("finished_utc", setDefault: true))
            .AddColumn(DBColumn.MakeTimestampColumn("created_utc", setDefault: true));
        #endregion

        #region usersSnakeGamesTable
        public static DBTable usersSnakeGamesTable = new DBTable("users_snake_games")
            .AddColumn(DBColumn.MakeIdColumn())
            .AddColumn(DBColumn.MakeFKColumn("user_id", "users", "id"))
            .AddColumn(DBColumn.MakeFKColumn("game_id", "games", "id"));
        #endregion

        #region snakeGameLogs
        // Map logs
        //  snake_positions:
        //      format: [(y1,x1),(y2,x2),...,(yN,xN)]
        //          where N=snake_length
        public static DBTable snakeGameLogs = new DBTable("snake_game_logs")
            .AddColumn(DBColumn.MakeIdColumn())
            .AddColumn(DBColumn.MakeFKColumn("game_id", "games", "id"))
            .AddColumn(DBColumn.MakeFKColumn("user_id", "users", "id"))
            .AddColumn(DBColumn.MakeIntegerColumn("snake_length"))
            .AddColumn(DBColumn.MakeTextColumn("snake_positions"))
            .AddColumn(DBColumn.MakeTimestampColumn("created_utc", setDefault: true));
        #endregion

        // Create SQLite database
        public static void CreateDB()
        {
            // Create DB if not exist and then print the version
            string sqliteVersion = db.ExecuteScalar<string>("select sqlite_version()");
            Console.WriteLine($"Running SQLite version {sqliteVersion}");
        }

        // Reset the database
        public static void ResetDB()
        {
            File.Delete(Properties.Resources.dbPath);
            InitDB();
        }

        // Initialize database
        public static void InitDB()
        {
            // Initialize the .db file
            CreateDB();

            // Create tables
            db.CreateTable(usersTable);
            db.CreateTable(snakeGamesTable);
            db.CreateTable(usersSnakeGamesTable);
            db.CreateTable(snakeGameLogs);

            // Add first user
            string username = Properties.Resources.exampleUser;
            string email = Properties.Resources.exampleEmail;
            string password = Properties.Resources.examplePassword;
            AddUser(username, email, password);
        }

        public static string FormatDateTime(DateTime date)
        {
            return date.ToString("yyyy-MM-dd HH:mm-ss");
        }

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
        public static string? SnakeLoginUser(string username, string password)
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
                DateTime tokenExpire = DateTime.UtcNow.AddDays(1);

                db.Update(new SQLUpdate(usersTable.tableName)
                    .ColumnUpdate(
                        ("login_token", token),
                        ("login_token_expire_utc", FormatDateTime(tokenExpire))
                    )
                    .WhereEq("username", username)
                );
                return token;
            }
        }

        public static string? SnakeGetUsername(string loginToken)
        {
            // If token found & valid
            if (SnakeCheckToken(loginToken) == true)
            {
                string? username = db.SelectScalar<string>(new SQLSelect(usersTable.tableName)
                    .Columns("username")
                    .WhereEq("login_token", loginToken));
                return username;
            }
            return null;
        }

        public static bool SnakeCheckToken(string token)
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
                if (expires != null && DateTime.Parse(expires) < DateTime.UtcNow)
                {
                    return true;
                }
            }
            return false;
        }

        public static async Task SnakeAddMapLog(string gameGuid, Dictionary<Guid, Snake> players)
        {
            return;
            //string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

            //List<Task> tasks = new List<Task>(players.Count);

            //foreach(Snake player in players.Values)
            //{
            //    tasks.Add(new Task(delegate
            //        {
            //            db.InsertOrUpdateColumn(
            //                snakeLogTable,
            //                new string[] {
            //                    "username",
            //                    "game_guid"
            //                },
            //                new string[]
            //                {
            //                    player.username,
            //                    gameGuid
            //                },
            //                new string[]
            //                {
            //                    "game_guid",
            //                    "username",
            //                    "snake_length",
            //                    "next_move",
            //                    "timestamp_utc"
            //                },
            //                new string[]
            //                {
            //                    gameGuid,
            //                    player.username,
            //                    player.GetPositions().Length.ToString(),
            //                    "" + player.GetDirection(),
            //                    timestamp
            //                }
            //            );
            //        })
            //    );
            //    tasks.Last().Start();
            //}

            //await Task.WhenAll(tasks.ToArray());
        }

        public static DataTable SnakeGetHighScores()
        {
            return null;
            //DataTable highScores = db.GetTable(snakeLogTable);
            //return highScores;
        }

    }

}
