using KMU.HisOrder.MVC.Areas.BloodBank.Models;
using KMU.HisOrder.MVC.Models;
using KMU.HisOrder.MVC.Models.BloodBank;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMU.HisOrder.MVC.Areas.BloodBank.Controllers
{
    public class InventoryController : BloodBankBaseController
    {
        public InventoryController(KMUContext context, BloodBankService service) : base(context, service) { }

        public async Task<IActionResult> Index(InventoryListViewModel filters)
        {
            await Service.ExpireOverdueUnitsAsync();
            filters.Units = await Service.QueryInventory(filters).ToListAsync();
            SetBloodBankLookups(filters.FilterBloodType, filters.FilterComponent, null, filters.FilterStatus);
            return View(filters);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(long unitId)
        {
            var (userId, _) = GetCurrentUser();
            var result = await Service.ApproveUnitAsync(unitId, userId);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(long unitId)
        {
            var (userId, _) = GetCurrentUser();
            var result = await Service.RejectUnitAsync(unitId, userId);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> History(long id)
        {
            var vm = await Service.GetUnitHistoryDetailAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        public IActionResult Dispense(long id) =>
            RedirectToAction("Create", "Dispense", new { area = "BloodBank", unitId = id });
    }
}
