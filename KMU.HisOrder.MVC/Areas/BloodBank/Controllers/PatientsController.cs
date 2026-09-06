using KMU.HisOrder.MVC.Areas.BloodBank.Models;
using KMU.HisOrder.MVC.Models;
using KMU.HisOrder.MVC.Models.BloodBank;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMU.HisOrder.MVC.Areas.BloodBank.Controllers
{
    public class PatientsController : BloodBankBaseController
    {
        public PatientsController(KMUContext context, BloodBankService service) : base(context, service) { }

        public IActionResult Find()
        {
            ViewData["Title"] = "Blood Bank → Find Patient";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PatientSearch(string? patientId, string? phone, string? name)
        {
            ViewBag.RequestsFilterBaseUrl = Url.Action("Index", "Requests", new { area = "BloodBank" });

            if (string.IsNullOrWhiteSpace(patientId)
                && string.IsNullOrWhiteSpace(phone)
                && string.IsNullOrWhiteSpace(name))
            {
                return PartialView("_PatientSearchResults",
                    new BloodBankPatientSearchResultViewModel { EmptyParam = true });
            }

            var query = Context.KmuCharts.AsQueryable();
            if (!string.IsNullOrWhiteSpace(patientId))
                query = query.Where(c => c.ChrHealthId.Contains(patientId.Trim()));
            if (!string.IsNullOrWhiteSpace(phone))
                query = query.Where(c => c.ChrMobilePhone != null && c.ChrMobilePhone.Contains(phone.Trim()));
            if (!string.IsNullOrWhiteSpace(name))
            {
                var n = name.Trim();
                query = query.Where(c =>
                    (c.ChrPatientFirstname + " " + c.ChrPatientMidname + " " + c.ChrPatientLastname).Contains(n));
            }

            var charts = await query.OrderBy(c => c.ChrHealthId).Take(100).ToListAsync();
            var patientIds = charts.Select(c => c.ChrHealthId.Trim()).ToList();
            var pendingByPatient = await Context.BloodBankRequests
                .Where(r => r.Status == RequestStatuses.Pending)
                .ToListAsync();
            pendingByPatient = pendingByPatient
                .Where(r => patientIds.Contains(r.PatientId.Trim(), StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(r => r.UrgencyLevel == UrgencyLevels.Emergency)
                .ThenByDescending(r => r.UrgencyLevel == UrgencyLevels.Urgent)
                .ThenByDescending(r => r.RequestDateTime)
                .ToList();

            var rows = charts.Select(c =>
            {
                var id = c.ChrHealthId.Trim();
                var pending = pendingByPatient.FirstOrDefault(r => r.PatientId == id);
                return new BloodBankPatientSearchRowViewModel
                {
                    Chart = c,
                    PendingRequestId = pending?.RequestId,
                    BloodType = pending?.BloodType,
                    ComponentType = pending?.ComponentType,
                    UrgencyLevel = pending?.UrgencyLevel,
                    UnitsRequested = pending?.UnitsRequested ?? 0
                };
            }).ToList();

            return PartialView("_PatientSearchResults",
                new BloodBankPatientSearchResultViewModel { Rows = rows });
        }

        public async Task<IActionResult> Profile(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(Find));

            var panel = await Service.GetPatientPanelAsync(id.Trim());
            if (panel == null)
            {
                TempData["Error"] = "Patient not found in HIS.";
                return RedirectToAction(nameof(Find));
            }

            var patientId = panel.PatientId;
            var requests = await Context.BloodBankRequests
                .Where(r => r.PatientId == patientId)
                .OrderByDescending(r => r.RequestDateTime)
                .ToListAsync();

            var events = await Context.BloodBankPatientEvents
                .Where(e => e.PatientId == patientId)
                .OrderByDescending(e => e.EventDateTime)
                .Take(50)
                .ToListAsync();

            ViewBag.Requests = requests;
            ViewBag.Events = events;
            ViewBag.PendingRequest = requests.FirstOrDefault(r => r.Status == RequestStatuses.Pending);
            return View(panel);
        }
    }
}
