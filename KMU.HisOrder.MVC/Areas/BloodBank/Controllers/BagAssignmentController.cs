using KMU.HisOrder.MVC.Areas.BloodBank.Models;
using KMU.HisOrder.MVC.Models;
using KMU.HisOrder.MVC.Models.BloodBank;
using Microsoft.AspNetCore.Mvc;

namespace KMU.HisOrder.MVC.Areas.BloodBank.Controllers
{
    public class BagAssignmentController : BloodBankBaseController
    {
        public BagAssignmentController(KMUContext context, BloodBankService service) : base(context, service) { }

        public async Task<IActionResult> Index()
        {
            var queue = await Service.GetBagAssignmentQueueAsync();
            return View(queue);
        }

        public async Task<IActionResult> Create(long donorId)
        {
            var donor = await Context.BloodBankDonors.FindAsync(donorId);
            if (donor == null) return NotFound();

            var collection = DateTime.Now;
            var model = new BagAssignmentFormViewModel
            {
                DonorId = donor.DonorId,
                BloodBankDonorId = BloodBankIds.FormatDonor(donor.DonorId),
                DonorName = $"{donor.FirstName} {donor.LastName}",
                DonorPhone = donor.Phone,
                BloodType = donor.BloodType,
                DonationType = donor.DonationType,
                PatientId = donor.PatientId,
                RequestId = donor.RequestId,
                CollectionDate = collection,
                ComponentType = BloodComponents.PackedRbc
            };
            SetBloodBankLookups();
            ViewBag.Components = BloodBankLookups.Components;
            ViewBag.SuggestedSerial = await Service.GenerateBagSerialAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BagAssignmentFormViewModel model)
        {
            if (model.UseGeneratedSerial)
                ModelState.Remove(nameof(BagAssignmentFormViewModel.SerialNumber));

            if (!ModelState.IsValid)
            {
                SetBloodBankLookups();
                ViewBag.Components = BloodBankLookups.Components;
                ViewBag.SuggestedSerial = await Service.GenerateBagSerialAsync();
                return View(model);
            }

            var (userId, userName) = GetCurrentUser();
            var result = await Service.AssignBagAsync(model, userId, userName);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            if (!result.Success)
            {
                ViewBag.Components = BloodBankLookups.Components;
                return View(model);
            }
            return RedirectToAction("Index", "Inventory", new { area = "BloodBank" });
        }

        [HttpGet]
        public IActionResult CalcExpiry(string componentType, DateTime collectionDate)
        {
            var days = BloodBankService.GetExpiryDays(componentType);
            return Json(new { expiryDate = collectionDate.AddDays(days) });
        }
    }
}
