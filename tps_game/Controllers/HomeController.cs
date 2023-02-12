using Microsoft.AspNetCore.Mvc;

namespace tps_game.Controllers
{
    public class HomeController : Controller
    {
     
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }

    }
}