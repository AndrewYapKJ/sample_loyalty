using Microsoft.AspNetCore.Mvc;

namespace gussmann_loyalty_program.Controllers
{
    public class LoginController : Controller
    {
        [Route("/")]
        [Route("/login")]
        public IActionResult Index()
        {
            return View();
        }

        [Route("/redirect-to-dashboard")]
        public IActionResult RedirectToDashboard()
        {
            return Redirect("/dashboard");
        }
    }
}