using KMU.HisOrder.MVC.Areas.BloodBank.Models;
using KMU.HisOrder.MVC.Models;
using KMU.HisOrder.MVC.Models.BloodBank;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMU.HisOrder.MVC.Areas.BloodBank.Controllers
{
    public class RequestsController : BloodBankBaseController
    {
        private readonly BloodBankIntegrationService _integration;

        public RequestsController(
            KMUContext context,
            BloodBankService service,
            BloodBankIntegrationService integration) : base(context, service)
        {
            _integration = integration;
        }

        public async Task<IActionResult> Index(
            string? status,
            string? urgencyFilter,
            string? bloodTypeFilter,
            string? patientIdFilter,
            string? patientNameFilter,
            string? phoneFilter,
            string? inhospidFilter)
        {
            var allForCounts = await Context.BloodBankRequests.ToListAsync();
            await _integration.ImportUnlinkedBloodOrdersFromHisAsync();
            await _integration.SyncRequestsFromHisOrdersAsync(allForCounts);
            allForCounts = await Context.BloodBankRequests.ToListAsync();
            ViewBag.PendingCount = allForCounts.Count(r => r.Status == RequestStatuses.Pending);
            ViewBag.FulfilledCount = allForCounts.Count(r => r.Status == RequestStatuses.Fulfilled);
            ViewBag.RejectedCount = allForCounts.Count(r => r.Status == RequestStatuses.Rejected);
            ViewBag.EmergencyCount = allForCounts.Count(r =>
                r.Status == RequestStatuses.Pending && r.UrgencyLevel == UrgencyLevels.Emergency);

            var q = Context.BloodBankRequests.AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(r => r.Status == status);
            if (!string.IsNullOrWhiteSpace(urgencyFilter))
                q = q.Where(r => r.UrgencyLevel == urgencyFilter);
            if (!string.IsNullOrWhiteSpace(patientIdFilter))
            {
                var pid = patientIdFilter.Trim().ToUpperInvariant();
                q = q.Where(r => r.PatientId != null && r.PatientId.ToUpper().Contains(pid));
            }
            if (!string.IsNullOrWhiteSpace(patientNameFilter))
            {
                var name = patientNameFilter.Trim();
                q = q.Where(r => r.PatientName.Contains(name));
            }
            if (!string.IsNullOrWhiteSpace(phoneFilter))
            {
                var phone = phoneFilter.Trim();
                var patientIdsByPhone = await Context.KmuCharts
                    .Where(c => c.ChrMobilePhone != null && c.ChrMobilePhone.Contains(phone))
                    .Select(c => c.ChrHealthId)
                    .ToListAsync();
                var phoneIdSet = patientIdsByPhone
                    .Select(id => id.Trim())
                    .Where(id => id.Length > 0)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (phoneIdSet.Count == 0)
                    q = q.Where(_ => false);
                else
                    q = q.Where(r => phoneIdSet.Contains(r.PatientId));
            }
            if (!string.IsNullOrWhiteSpace(inhospidFilter))
            {
                var inh = inhospidFilter.Trim();
                q = q.Where(r => r.Inhospid.Contains(inh));
            }

            var requests = await q.ToListAsync();
            await _integration.SyncRequestsFromHisOrdersAsync(requests);

            if (!string.IsNullOrWhiteSpace(bloodTypeFilter))
                requests = requests.Where(r => r.BloodType == bloodTypeFilter).ToList();
            requests = requests
                .OrderByDescending(r => r.RequestDateTime)
                .ThenByDescending(r => r.RequestId)
                .ToList();

            SetBloodBankLookups(bloodTypeFilter, null, urgencyFilter, status);
            return View(new RequestQueueViewModel
            {
                StatusFilter = status,
                UrgencyFilter = urgencyFilter,
                BloodTypeFilter = bloodTypeFilter,
                PatientIdFilter = patientIdFilter?.Trim(),
                PatientNameFilter = patientNameFilter?.Trim(),
                PhoneFilter = phoneFilter?.Trim(),
                InhospidFilter = inhospidFilter?.Trim(),
                Requests = requests
            });
        }

        [HttpPost]
        public async Task<IActionResult> PatientSearch(string? patientId, string? phone, string? name)
        {
            ViewBag.RequestsFilterBaseUrl = Url.Action(nameof(Index), "Requests", new { area = "BloodBank" });

            if (string.IsNullOrWhiteSpace(patientId)
                && string.IsNullOrWhiteSpace(phone)
                && string.IsNullOrWhiteSpace(name))
            {
                return PartialView("~/Areas/BloodBank/Views/Patients/_PatientSearchResults.cshtml",
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
                var pending = pendingByPatient.FirstOrDefault(r =>
                    string.Equals(r.PatientId.Trim(), id, StringComparison.OrdinalIgnoreCase));
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

            return PartialView("~/Areas/BloodBank/Views/Patients/_PatientSearchResults.cshtml",
                new BloodBankPatientSearchResultViewModel { Rows = rows });
        }

        [HttpGet]
        public async Task<IActionResult> Create(string? patientId)
        {
            var (userId, userName) = GetCurrentUser();
            var model = new BloodRequestFormViewModel
            {
                RequestingDoctorId = userId,
                RequestingDoctorName = userName,
                BloodType = "O+",
                ComponentType = BloodComponents.PackedRbc,
                UrgencyLevel = UrgencyLevels.Routine,
                PatientType = "IPD",
                UnitsRequested = 1
            };

            if (!string.IsNullOrWhiteSpace(patientId))
            {
                var panel = await Service.GetPatientPanelAsync(patientId.Trim());
                if (panel == null)
                {
                    TempData["Error"] = "Patient not found in HIS.";
                    return RedirectToAction(nameof(Index));
                }

                model.PatientId = panel.PatientId;
                model.PatientName = panel.PatientName;
                model.Inhospid = panel.Inhospid ?? "";
                model.Ward = panel.Ward;
                model.BedLocation = panel.BedLocation;
                if (!string.IsNullOrWhiteSpace(panel.BloodType))
                    model.BloodType = panel.BloodType;
                if (!string.IsNullOrWhiteSpace(panel.ComponentType))
                    model.ComponentType = panel.ComponentType;
                if (!string.IsNullOrWhiteSpace(panel.PatientType))
                    model.PatientType = panel.PatientType;
                if (panel.UnitsRequested > 0)
                    model.UnitsRequested = panel.UnitsRequested;
                model.PatientPanel = panel;
            }

            SetBloodBankLookups(model.BloodType, model.ComponentType, model.UrgencyLevel);
            ViewBag.ShowUnitsField = true;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BloodRequestFormViewModel model)
        {
            var (userId, userName) = GetCurrentUser();
            if (string.IsNullOrWhiteSpace(model.RequestingDoctorId))
                model.RequestingDoctorId = userId;
            if (string.IsNullOrWhiteSpace(model.RequestingDoctorName))
                model.RequestingDoctorName = userName;
            if (string.IsNullOrWhiteSpace(model.Inhospid))
                model.Inhospid = "";

            if (!ModelState.IsValid)
            {
                if (!string.IsNullOrWhiteSpace(model.PatientId))
                    model.PatientPanel = await Service.GetPatientPanelAsync(model.PatientId);
                SetBloodBankLookups(model.BloodType, model.ComponentType, model.UrgencyLevel);
                ViewBag.ShowUnitsField = true;
                return View(model);
            }

            var result = await Service.CreateManualRequestAsync(model, userId);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            if (!result.Success)
            {
                if (!string.IsNullOrWhiteSpace(model.PatientId))
                    model.PatientPanel = await Service.GetPatientPanelAsync(model.PatientId);
                SetBloodBankLookups(model.BloodType, model.ComponentType, model.UrgencyLevel);
                ViewBag.ShowUnitsField = true;
                return View(model);
            }

            return RedirectToAction(nameof(Details), new { id = result.RequestId });
        }

        [HttpGet]
        public async Task<IActionResult> LookupPatient(string patientId)
        {
            var panel = await Service.GetPatientPanelAsync(patientId);
            if (panel == null)
                return Json(new { success = false, message = "Patient not found in HIS." });
            return Json(new { success = true, data = panel });
        }

        public async Task<IActionResult> Details(long id)
        {
            var request = await Context.BloodBankRequests.FindAsync(id);
            if (request == null) return NotFound();

            await _integration.SyncRequestsFromHisOrdersAsync(new[] { request });

            ViewBag.PatientPanel = await Service.GetPatientPanelByRequestAsync(id);
            ViewBag.Dispenses = await Context.BloodBankDispenseRecords
                .Where(d => d.RequestId == id)
                .OrderByDescending(d => d.DispenseDateTime)
                .ToListAsync();
            return View(request);
        }

        public async Task<IActionResult> Dispense(long id)
        {
            var request = await Context.BloodBankRequests.FindAsync(id);
            if (request != null)
            {
                await _integration.SyncRequestsFromHisOrdersAsync(new[] { request });
            }

            var (_, userName) = GetCurrentUser();
            var panel = await Service.BuildDispensePanelAsync(id, userName);
            if (panel == null) return NotFound();
            return View(panel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dispense(DispensePanelViewModel model)
        {
            var (userId, userName) = GetCurrentUser();

            model.PatientPanel = await Service.GetPatientPanelByRequestAsync(model.RequestId)
                ?? model.PatientPanel ?? new BloodBankPatientPanelViewModel();

            if (string.IsNullOrWhiteSpace(model.CollectorName))
                model.CollectorName = userName?.Trim() ?? model.DispensingStaffName?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(model.WardDestination))
            {
                model.WardDestination = BloodBankService.FormatWardBed(
                    model.PatientPanel.Ward, model.PatientPanel.BedLocation);
            }
            if (string.IsNullOrWhiteSpace(model.WardDestination))
                model.WardDestination = "—";

            if (string.IsNullOrWhiteSpace(model.CollectorName))
                ModelState.AddModelError(nameof(DispensePanelViewModel.CollectorName),
                    "Collector name could not be determined. Enter who is collecting for the ward.");

            if (!ModelState.IsValid)
            {
                var rebuilt = await Service.BuildDispensePanelAsync(model.RequestId, userName);
                if (rebuilt != null)
                {
                    rebuilt.CollectorName = model.CollectorName;
                    rebuilt.WardDestination = model.WardDestination;
                    rebuilt.SourceMode = model.SourceMode;
                    rebuilt.SelectedUnitIds = model.SelectedUnitIds;
                    rebuilt.SelectedDirectedDonorId = model.SelectedDirectedDonorId;
                    rebuilt.Confirmed = model.Confirmed;
                }
                if (rebuilt == null)
                    return NotFound();
                return View(rebuilt);
            }

            model.DispensingStaffName = userName;
            var result = await Service.DispenseUnitsAsync(model, userId, userName);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            if (!result.Success)
            {
                var rebuilt = await Service.BuildDispensePanelAsync(model.RequestId, userName);
                if (rebuilt != null)
                {
                    rebuilt.CollectorName = model.CollectorName;
                    rebuilt.WardDestination = model.WardDestination;
                    rebuilt.SourceMode = model.SourceMode;
                    rebuilt.SelectedUnitIds = model.SelectedUnitIds;
                    rebuilt.SelectedDirectedDonorId = model.SelectedDirectedDonorId;
                    rebuilt.Confirmed = model.Confirmed;
                }
                if (rebuilt == null)
                    return NotFound();
                return View(rebuilt);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(long id, string reason)
        {
            var request = await Context.BloodBankRequests.FindAsync(id);
            if (request == null) return NotFound();

            var (userId, _) = GetCurrentUser();
            request.Status = RequestStatuses.Rejected;
            request.RejectReason = reason;
            request.ModifyUser = userId;
            request.ModifyDate = DateTime.Now;
            await Context.SaveChangesAsync();

            TempData["Success"] = "Request rejected.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult RegisterDonor(long id) =>
            RedirectToAction("Create", "Donors", new { area = "BloodBank", requestId = id });
    }
}
