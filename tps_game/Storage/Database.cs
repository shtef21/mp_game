using System.Data;
using System.Globalization;
using tps_game.Code.Games;

namespace tps_game
{

    // Database wrapper class
    public class Database
    {
        // When project loads, create a database handler
        private static DB.DbHandler db = new DB.DbHandler(Properties.Resources.dbPath);

        // Users
        static string snakeUsersTable = "snake_users";
        static string[] SnakeUsersColumns = new string[]
        {
            "id integer primary key autoincrement",
            "guid char(36)",
            "username varchar(10)",
            "hashed_pw char(88)",
            "login_token char(36)"
        };

        // Map logs
        static string snakeLogTable = "snake_log";
        static string[] snakeLogColumns = new string[]
        {
            "id integer primary key autoincrement",
            "game_guid char(36)",
            "username varchar(10)",
            "snake_length integer",
            "next_move char(1)",
            "timestamp_utc char(23)"
        };

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

            // Create users table if not exists
            db.CreateTable(snakeUsersTable, SnakeUsersColumns);

            // Create map_log table if not exists
            db.CreateTable(snakeLogTable, snakeLogColumns);

            // Create example user
            string username = tps_game.Properties.Resources.exampleUser;
            string password = tps_game.Properties.Resources.examplePassword;

            Console.Write($"Adding user shtef21/shtef21... ");
            bool userAddFlag = SnakeAddUser(username, password);
            Console.WriteLine(userAddFlag ? "User created!" : "Username taken.");
        }

        public static bool SnakeAddUser(string username, string password)
        {
            long count = db.ExecuteScalarWhere<long>($"select count(*) from {snakeUsersTable}", "username", username);
            if (count > 0)
            {
                // User already exists
                return false;
            }

            // Make user
            string hashedPw = DB.Crypto.MakeSha512(password);
            db.AddRow(
                snakeUsersTable,
                new string[] { "username", "hashed_pw", "guid" },
                new string[] { username, hashedPw, Guid.NewGuid().ToString() }
            );
            return true;
        }

        // Login the user and return his login token
        public static string? SnakeLoginUser(string username, string password)
        {
            string hashedPw = DB.Crypto.MakeSha512(password);

            var findUser = db.GetTable(
                snakeUsersTable,
                null,
                new string[]
                {
                    "username",
                    "hashed_pw"
                },
                new string[]
                {
                    username,
                    hashedPw
                });

            if (findUser.Rows.Count == 0)
            {
                return null;
            }
            else
            {
                // Generate and save token
                string token = Guid.NewGuid().ToString();
                long userId = (long)findUser.Rows[0]["id"];

                db.UpdateColumn(snakeUsersTable, "login_token", token, "id", userId.ToString());
                return token;
            }
        }

        public static string? SnakeGetUsername(string loginToken)
        {
            DataTable user = db.GetTable(
                snakeUsersTable,
                new string[] { "username" },
                new string[] { "login_token" },
                new string[] { loginToken }
            );

            // Return username if available
            return user.Rows.Count > 0 ? user.Rows[0]["username"].ToString() : null;
        }

        public static bool SnakeCheckToken(string token)
        {
            var findUser = db.GetTable(snakeUsersTable, null,
                new string[] { "login_token" },
                new string[] { token }
            );
            return findUser.Rows.Count > 0;
        }

        public static async Task SnakeAddMapLog(string gameGuid, Dictionary<Guid, Snake> players)
        {
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

            List<Task> tasks = new List<Task>(players.Count);

            foreach(Snake player in players.Values)
            {
                tasks.Add(new Task(delegate
                    {
                        db.InsertOrUpdateColumn(
                            snakeLogTable,
                            new string[] {
                                "username",
                                "game_guid"
                            },
                            new string[]
                            {
                                player.username,
                                gameGuid
                            },
                            new string[]
                            {
                                "game_guid",
                                "username",
                                "snake_length",
                                "next_move",
                                "timestamp_utc"
                            },
                            new string[]
                            {
                                gameGuid,
                                player.username,
                                player.GetPositions().Length.ToString(),
                                "" + player.GetDirection(),
                                timestamp
                            }
                        );
                    })
                );
                tasks.Last().Start();
            }

            await Task.WhenAll(tasks.ToArray());
        }

        public static DataTable SnakeGetHighScores()
        {
            DataTable highScores = db.GetTable(snakeLogTable);
            return highScores;
        }

    }

}
