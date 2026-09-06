using KMU.HisOrder.MVC.Areas.BloodBank.Models;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Mvc;

namespace KMU.HisOrder.MVC.Areas.BloodBank.Controllers
{
    public class HomeController : BloodBankBaseController
    {
        public HomeController(KMUContext context, BloodBankService service) : base(context, service) { }

        public async Task<IActionResult> Index()
        {
            await SetDashboardMetricsAsync();
            return View();
        }

        /// <summary>Find donor from dashboard — redirects to profile or history results.</summary>
        [HttpGet]
        public IActionResult FindDonor(string? q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return RedirectToAction("Find", "Donors", new { area = "BloodBank" });
            return RedirectToAction("Find", "Donors", new { area = "BloodBank", donorId = q, phone = q });
        }
    }
}
