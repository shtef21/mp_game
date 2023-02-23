using System.Net.WebSockets;
using tps_game.Code.Games;

namespace tps_game.Code
{
    // A class used for various tasks
    public class Static
    {
        public static Random Random = new Random();


#if DEBUG
        public static bool dbDeleteSetting = true;
#else
        public static bool deletePreviousDb = false;
#endif

        // Middleware used in Program.cs for handling cases when user has or doesn't have a login token
        public static async Task CheckTokenMiddleware(HttpContext context, Func<Task> next)
        {
            string requestPath = context.Request.Path.ToString().ToLower();
            bool tokenValid = false;

            // Check token
            string? userToken = context.Request.Cookies["token"];
            if (userToken != null)
            {
                tokenValid = tps_game.Database.CheckToken(userToken);
            }

            if (tokenValid == false)
            {
                // Token not found or invalid

                if (requestPath == "/user/login")
                {
                    // This is a path for user login
                    await next();
                }
                else if (requestPath.ToLower().StartsWith("/authorization/") == false)
                {
                    // No token, redirect to login
                    context.Response.Redirect("/authorization/login");
                }
                else
                {
                    await next();
                }
            }
            else
            {
                await next();
            }
        }

    }
}
