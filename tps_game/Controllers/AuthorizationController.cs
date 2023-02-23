using Microsoft.AspNetCore.Mvc;

namespace tps_game.Controllers
{
    public class AuthorizationController : Controller
    {

        // Users get here when they start using the website
        public IActionResult Login()
        {
            return View();
        }

        // Users get here if they want to make an account
        public IActionResult SignUp()
        {
            return View();
        }

        // Users get here if they want to reqest password reset
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // Users get here when they open the password-reset link on their e-mails
        public IActionResult ResetPassword(string resetToken)
        {
            return View();
        }

    }
}
