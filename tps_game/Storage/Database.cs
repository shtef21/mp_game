namespace tps_game
{

    // Database wrapper class
    public class Database
    {
        // When project loads, create a database handler
        private static DB.DbHandler db = new DB.DbHandler(Properties.Resources.dbPath);

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
            string tableName = "users";
            string[] tableColumns = new string[]
            {
                "id integer primary key autoincrement",
                "username varchar(10)",
                "hashed_pw char(88)",
                "login_token char(36)"
            };
            db.CreateTable(tableName, tableColumns);

            // Create example user
            string username = "shtef21";
            string password = "shtef21";

            Console.Write($"Adding user shtef21/shtef21... ");
            bool userAddFlag = AddUser(username, password);
            Console.WriteLine(userAddFlag ? "User created!" : "Username taken.");
        }

        public static bool AddUser(string username, string password)
        {
            long count = db.ExecuteScalarWhere<long>("select count(*) from users", "username", username);
            if (count > 0)
            {
                // User already exists
                return false;
            }

            // Make user
            string hashedPw = DB.Crypto.MakeSha512(password);
            db.AddRow(
                "users",
                new string[] { "username", "hashed_pw" },
                new string[] { username, hashedPw }
            );
            return true;
        }

        // Login the user and return his login token
        public static string? LoginUser(string username, string password)
        {
            string hashedPw = DB.Crypto.MakeSha512(password);

            var findUser = db.GetTable("users",
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

                db.UpdateColumn("users", "login_token", token, "id", userId.ToString());
                return token;
            }
        }

        public static bool CheckToken(string token)
        {
            var findUser = db.GetTable("users", null,
                new string[] { "login_token" },
                new string[] { token }
            );
            return findUser.Rows.Count > 0;
        }

    }

}
