using KMU.HisOrder.MVC.Areas.BloodBank.Models;
using KMU.HisOrder.MVC.Models;
using KMU.HisOrder.MVC.Models.BloodBank;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace KMU.HisOrder.MVC.Areas.BloodBank.Controllers
{
    public class ScreeningController : BloodBankBaseController
    {
        private readonly IConfiguration _configuration;

        public ScreeningController(KMUContext context, BloodBankService service, IConfiguration configuration)
            : base(context, service)
        {
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var queue = await Service.GetScreeningQueueAsync();
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            ViewBag.QueueCount = queue.Count;
            ViewBag.LegacyQueueCount = queue.Count(q => q.IsLegacyUnit);
            ViewBag.PassedToday = await Context.BloodBankScreeningRecords.CountAsync(s =>
                s.ScreeningDateTime >= today && s.ScreeningDateTime < tomorrow
                && s.OverallResult == ScreeningResults.Passed);
            ViewBag.FailedToday = await Context.BloodBankScreeningRecords.CountAsync(s =>
                s.ScreeningDateTime >= today && s.ScreeningDateTime < tomorrow
                && s.OverallResult == ScreeningResults.Failed);
            return View(queue);
        }

        public async Task<IActionResult> Create(long donorId, long? unitId)
        {
            var (_, userName) = GetCurrentUser();
            if (unitId.HasValue)
            {
                var unit = await Context.BloodBankBloodUnits.Include(u => u.Donor).FirstOrDefaultAsync(u => u.UnitId == unitId);
                if (unit == null) return NotFound();
                return View(new ScreeningFormViewModel
                {
                    DonorId = unit.DonorId,
                    BloodBankDonorId = BloodBankIds.FormatDonor(unit.DonorId),
                    UnitId = unit.UnitId,
                    DonorName = BloodBankService.FormatDonorFullName(unit.Donor),
                    PreDonationMalariaResult = unit.Donor.PreDonationMalariaResult,
                    RegistrationVitalsSummary = unit.Donor.ScreeningNotes,
                    DonorPhone = unit.Donor.Phone,
                    BloodType = unit.BloodType,
                    SerialNumber = unit.SerialNumber,
                    StaffName = userName
                });
            }

            var donor = await Context.BloodBankDonors.FindAsync(donorId);
            if (donor == null) return NotFound();
            return View(new ScreeningFormViewModel
            {
                DonorId = donor.DonorId,
                BloodBankDonorId = BloodBankIds.FormatDonor(donor.DonorId),
                DonorName = BloodBankService.FormatDonorFullName(donor),
                DonorPhone = donor.Phone,
                BloodType = donor.BloodType,
                PreDonationMalariaResult = donor.PreDonationMalariaResult,
                RegistrationVitalsSummary = donor.ScreeningNotes,
                StaffName = userName
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ScreeningFormViewModel model)
        {
            model.OverallResult = BloodBankService.DeriveOverallResult(
                model.HivResult, model.HepBResult, model.HepCResult, model.SyphilisResult);

            if (string.IsNullOrWhiteSpace(model.Hemoglobin))
                ModelState.AddModelError(nameof(model.Hemoglobin), "Hemoglobin is required at screening.");
            if (string.IsNullOrWhiteSpace(model.Wbc))
                ModelState.AddModelError(nameof(model.Wbc), "WBC is required at screening.");
            if (string.IsNullOrWhiteSpace(model.Rbc))
                ModelState.AddModelError(nameof(model.Rbc), "RBC is required at screening.");
            if (string.IsNullOrWhiteSpace(model.Platelet))
                ModelState.AddModelError(nameof(model.Platelet), "Platelet count is required at screening.");

            if (!ModelState.IsValid)
            {
                var donor = await Context.BloodBankDonors.FindAsync(model.DonorId);
                if (donor != null)
                    model.RegistrationVitalsSummary = donor.ScreeningNotes;
                return View(model);
            }

            var (userId, userName) = GetCurrentUser();
            var result = await Service.RecordScreeningAsync(model, userId, userName);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            if (result.Success && result.ScreeningId.HasValue)
                return RedirectToAction(nameof(Print), new { id = result.ScreeningId.Value });
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Print(long id, bool auto = true)
        {
            var cert = await Service.GetScreeningCertificateAsync(id);
            if (cert == null) return NotFound();

            cert.FacilityName = _configuration.GetSection("WebsiteSettings")["SystemTitle"]?.Trim()
                ?? "Hospital Blood Bank";
            ViewBag.AutoPrint = auto;
            return View(cert);
        }
    }
}
