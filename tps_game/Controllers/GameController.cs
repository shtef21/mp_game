using Microsoft.AspNetCore.Mvc;

namespace tps_game.Controllers
{
    public class GameController : Controller
    {
#if DEBUG
        public void ResetDB()
        {
            Database.ResetDB();
        }
#endif

        public IActionResult Snake()
        {
            return View();
        }

        public IActionResult SnakeHighScores()
        {
            return null;
            var highScores = tps_game.Code.Games.SnakeHighScore.FetchHighScores();
            return Json(highScores);
        }

    }
}