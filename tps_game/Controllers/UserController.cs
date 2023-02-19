using Microsoft.AspNetCore.Mvc;

namespace tps_game.Controllers
{
    public class UserController : Controller
    {
        [HttpPost]
        public string Login()
        {
            using var reader = new StreamReader(Request.Body);
            string json = reader.ReadToEndAsync().Result;
            dynamic? body = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

            if (body == null)
            {
                return "N-body_missing";
            }

            string username = body["username"].ToString();
            string password = body["password"].ToString();
            bool rememberMe = body["rememberMe"].ToString() == "Y";
            string email = body["email"].ToString(); // Bot-checking

            string? token = tps_game.Database.LoginUser(username, password, rememberMe);
            if (token == null || string.IsNullOrWhiteSpace(email) == false)
            {
                return "N-not_found";
            }
            else
            {
                return token;
            }
        }

    }
}
