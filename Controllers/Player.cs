using Microsoft.AspNetCore.Mvc;

namespace team67.Controllers
{
    public class PlayerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}