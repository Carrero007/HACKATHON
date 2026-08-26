using Microsoft.AspNetCore.Mvc;

namespace hackathon.Controllers
{
    public class HomeController : Controller
    {
        [Route("/Home/Error")]
        public IActionResult Error()
        {
            return View();
        }

        [Route("/Home/NaoEncontrado")]
        public IActionResult NaoEncontrado()
        {
            Response.StatusCode = 404;
            return View();
        }
    }
}