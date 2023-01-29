using Microsoft.AspNetCore.Mvc;

namespace tps_game.Controllers
{
    public class GameController : Controller
    {
     
        public IActionResult Snake()
        {
            return View();
        }

    }
}