using Microsoft.AspNetCore.Mvc;

namespace KMU.HisOrder.MVC.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();

        }
    }
}
