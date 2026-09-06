using KMU.HisOrder.MVC.Areas.BloodBank.Models;
using KMU.HisOrder.MVC.Models;
using KMU.HisOrder.MVC.Models.BloodBank;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMU.HisOrder.MVC.Areas.BloodBank.Controllers
{
    public class DispenseController : BloodBankBaseController
    {
        public DispenseController(KMUContext context, BloodBankService service) : base(context, service) { }

        public async Task<IActionResult> Index()
        {
            var records = await Context.BloodBankDispenseRecords
                .OrderByDescending(d => d.DispenseDateTime)
                .Take(100)
                .ToListAsync();
            return View(records);
        }

        public async Task<IActionResult> Create(long unitId, long? requestId, string? patientId, string? ward)
        {
            var unit = await Context.BloodBankBloodUnits
                .Include(u => u.Request)
                .Include(u => u.Donor)
                .FirstOrDefaultAsync(u => u.UnitId == unitId);
            if (unit == null) return NotFound();

            if (unit.ScreeningResult != ScreeningResults.Passed)
            {
                TempData["Error"] = "This unit has not passed screening and cannot be dispensed.";
                return RedirectToAction("Index", "Inventory", new { area = "BloodBank" });
            }

            if (unit.Status != UnitStatuses.Available && unit.Status != UnitStatuses.Reserved)
            {
                TempData["Error"] = $"Unit status is {unit.Status}; only available units can be dispensed.";
                return RedirectToAction("Index", "Inventory", new { area = "BloodBank" });
            }

            if (unit.ExpiryDate <= DateTime.Now)
            {
                TempData["Error"] = "This unit is expired and cannot be dispensed.";
                return RedirectToAction("Index", "Inventory", new { area = "BloodBank" });
            }

            var linkedRequestId = requestId ?? unit.RequestId;
            var wardFilter = ward?.Trim();

            BloodBankPatientPanelViewModel? panel = null;
            var recipientMode = "request";

            if (linkedRequestId.HasValue)
            {
                panel = await Service.GetPatientPanelByRequestAsync(linkedRequestId.Value);
                recipientMode = "request";
            }
            else if (!string.IsNullOrWhiteSpace(patientId))
            {
                panel = await Service.GetPatientPanelAsync(patientId.Trim(), null);
                recipientMode = panel != null ? "his" : "manual";
                if (panel == null)
                {
                    panel = new BloodBankPatientPanelViewModel { PatientId = patientId.Trim() };
                }
            }
            else if (!string.IsNullOrWhiteSpace(unit.PatientId))
            {
                panel = await Service.GetPatientPanelAsync(unit.PatientId, unit.RequestId);
                recipientMode = "his";
            }

            var (_, userName) = GetCurrentUser();
            var wardBed = panel != null
                ? BloodBankService.FormatWardBed(panel.Ward, panel.BedLocation)
                : "";

            var form = new DispenseFormViewModel
            {
                UnitId = unitId,
                RequestId = linkedRequestId,
                SerialNumber = unit.SerialNumber,
                UnitBloodType = unit.BloodType,
                UnitComponentType = BloodBankLookups.NormalizeComponent(unit.ComponentType),
                UnitExpiryDate = unit.ExpiryDate,
                UnitStatus = unit.Status,
                PatientPanel = panel ?? new BloodBankPatientPanelViewModel(),
                DispensingStaffName = userName,
                DispenseDateTime = DateTime.Now,
                WardDestination = wardBed,
                WardFilter = wardFilter,
                RecipientMode = recipientMode,
                UnitsReleased = 1,
                CollectorName = userName,
                UnitDonorId = unit.DonorId,
                UnitDonorName = unit.Donor != null ? BloodBankService.FormatDonorFullName(unit.Donor) : null,
                UnitDonorBloodType = unit.Donor?.BloodType ?? unit.BloodType,
                UnitDonorPhone = unit.Donor?.Phone,
                SourceDonorId = unit.DonorId
            };

            var pending = await Service.SearchPendingBloodRequestsAsync(
                null, 50, unit.BloodType, wardFilter);

            return View(new InventoryDispensePageViewModel
            {
                Form = form,
                PendingRequests = pending
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind(Prefix = "Form")] DispenseFormViewModel model)
        {
            var (userId, userName) = GetCurrentUser();
            await ResolveRecipientForPostAsync(model, userName, userId);
            await ValidateRecipientAsync(model);
            if (string.IsNullOrWhiteSpace(model.CollectorName))
                ModelState.AddModelError("Form.CollectorName", "Enter who is collecting / receiving the blood for the ward.");

            if (!ModelState.IsValid)
            {
                await ReloadUnitDisplayFieldsAsync(model);
                var unit = await Context.BloodBankBloodUnits.FindAsync(model.UnitId);
                var pending = await Service.SearchPendingBloodRequestsAsync(
                    null, 50, unit?.BloodType, model.WardFilter);
                return View(new InventoryDispensePageViewModel { Form = model, PendingRequests = pending });
            }

            model.DispensingStaffName = userName;
            var result = await Service.DispenseUnitAsync(model, userId, userName);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            if (!result.Success)
            {
                await ReloadUnitDisplayFieldsAsync(model);
                var unit = await Context.BloodBankBloodUnits.FindAsync(model.UnitId);
                var pending = await Service.SearchPendingBloodRequestsAsync(
                    null, 50, unit?.BloodType, model.WardFilter);
                return View(new InventoryDispensePageViewModel { Form = model, PendingRequests = pending });
            }
            return RedirectToAction("Index", "Inventory", new { area = "BloodBank" });
        }

        [HttpGet]
        public async Task<IActionResult> SearchPendingRequests(long unitId, string? term, string? ward)
        {
            var unit = await Context.BloodBankBloodUnits.FindAsync(unitId);
            if (unit == null) return Json(new { success = false, message = "Unit not found." });

            var items = await Service.SearchPendingBloodRequestsAsync(
                term, 50, unit.BloodType, ward?.Trim());
            return Json(new { success = true, items });
        }

        [HttpGet]
        public async Task<IActionResult> GetRequestRecipient(long requestId)
        {
            var panel = await Service.GetPatientPanelByRequestAsync(requestId);
            if (panel == null)
                return Json(new { success = false, message = "Request not found." });

            return Json(new
            {
                success = true,
                requestId,
                data = PanelToJson(panel)
            });
        }

        [HttpGet]
        public async Task<IActionResult> LookupPatient(string patientId, long? requestId)
        {
            if (string.IsNullOrWhiteSpace(patientId))
                return Json(new { success = false, message = "Enter a patient ID." });

            var panel = await Service.GetPatientPanelAsync(patientId.Trim(), requestId);
            if (panel == null)
                return Json(new { success = false, message = "Patient not found in HIS. Use manual entry if they are not in HisOrder." });

            return Json(new { success = true, data = PanelToJson(panel) });
        }

        private static object PanelToJson(BloodBankPatientPanelViewModel panel) => new
        {
            patientId = panel.PatientId,
            patientName = panel.PatientName,
            inhospid = panel.Inhospid,
            ward = panel.Ward,
            bedLocation = panel.BedLocation,
            bloodType = panel.BloodType,
            componentType = panel.ComponentType,
            unitsRequested = panel.UnitsRequested,
            urgencyLevel = panel.UrgencyLevel,
            requestingDoctor = panel.RequestingDoctor,
            requestId = panel.RequestId,
            wardDestination = BloodBankService.FormatWardBed(panel.Ward, panel.BedLocation)
        };

        [HttpGet]
        public async Task<IActionResult> SearchExternalDonors(string? q)
        {
            var items = await Service.SearchExternalDonorsAsync(q);
            return Json(new { success = true, items });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterExternalDonor([FromBody] ExternalDonorQuickRegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid donor data." });

            var (userId, _) = GetCurrentUser();
            var result = await Service.RegisterExternalDonorQuickAsync(model, userId);
            if (!result.Success)
                return Json(new { success = false, message = result.Message });

            var donor = await Context.BloodBankDonors.FindAsync(result.DonorId);
            return Json(new
            {
                success = true,
                message = result.Message,
                isExisting = result.IsExisting,
                donor = new
                {
                    donorId = result.DonorId,
                    fullName = donor != null ? BloodBankService.FormatDonorFullName(donor) : model.DonorName.Trim(),
                    bloodType = donor?.BloodType ?? model.BloodType,
                    phone = donor?.Phone,
                    nationalId = donor?.NationalId
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkUnitDonor(long unitId, long donorId)
        {
            var (userId, _) = GetCurrentUser();
            var result = await Service.AssignUnitDonorAsync(unitId, donorId, userId);
            if (!result.Success)
                return Json(new { success = false, message = result.Message });

            var donor = await Context.BloodBankDonors.FindAsync(donorId);
            return Json(new
            {
                success = true,
                message = result.Message,
                donor = new
                {
                    donorId,
                    fullName = donor != null ? BloodBankService.FormatDonorFullName(donor) : "",
                    bloodType = donor?.BloodType,
                    phone = donor?.Phone
                }
            });
        }

        private async Task ResolveRecipientForPostAsync(DispenseFormViewModel model, string currentUserName, string userId)
        {
            var mode = (model.RecipientMode ?? "request").Trim().ToLowerInvariant();
            model.PatientPanel ??= new BloodBankPatientPanelViewModel();

            if (mode == "request" && model.RequestId.HasValue)
            {
                var panel = await Service.GetPatientPanelByRequestAsync(model.RequestId.Value);
                if (panel != null)
                    model.PatientPanel = panel;
            }
            else if (mode == "his" && !string.IsNullOrWhiteSpace(model.PatientPanel.PatientId))
            {
                model.RequestId = null;
                var panel = await Service.GetPatientPanelAsync(
                    model.PatientPanel.PatientId.Trim(), null);
                if (panel != null)
                    model.PatientPanel = panel;
            }
            else if (mode == "manual")
            {
                model.RequestId = null;
                model.PatientPanel.Inhospid ??= "";
                model.PatientPanel.PatientId = "EXTERNAL";
                if (!model.SourceDonorId.HasValue || model.SourceDonorId <= 0)
                {
                    var unitDonorId = await Context.BloodBankBloodUnits
                        .Where(u => u.UnitId == model.UnitId)
                        .Select(u => u.DonorId)
                        .FirstOrDefaultAsync();
                    if (unitDonorId > 0)
                        model.SourceDonorId = unitDonorId;
                }
                if (model.SourceDonorId.HasValue)
                {
                    var donor = await Context.BloodBankDonors.FindAsync(model.SourceDonorId.Value);
                    if (donor != null)
                    {
                        await Service.AssignUnitDonorAsync(model.UnitId, donor.DonorId, userId);
                    }
                }
                if (string.IsNullOrWhiteSpace(model.PatientPanel.PatientName))
                    model.PatientPanel.PatientName = "External recipient";

                if (BloodBankService.IsManualOutsideHospital(model))
                {
                    var built = BloodBankService.BuildManualOutsideDestination(
                        model.PatientPanel.Address,
                        model.PatientPanel.Ward,
                        model.PatientPanel.BedLocation);
                    if (!string.IsNullOrWhiteSpace(built))
                        model.WardDestination = built;
                }
                else if (string.IsNullOrWhiteSpace(model.WardDestination)
                    && !string.IsNullOrWhiteSpace(model.PatientPanel.Ward))
                {
                    model.WardDestination = BloodBankService.FormatWardBed(
                        model.PatientPanel.Ward, model.PatientPanel.BedLocation);
                }
                else if (!BloodBankService.IsManualOutsideHospital(model))
                {
                    model.PatientPanel.Address = null;
                }
            }

            if (mode != "manual"
                && string.IsNullOrWhiteSpace(model.WardDestination)
                && (!string.IsNullOrWhiteSpace(model.PatientPanel.Ward) || !string.IsNullOrWhiteSpace(model.PatientPanel.BedLocation)))
            {
                model.WardDestination = BloodBankService.FormatWardBed(
                    model.PatientPanel.Ward, model.PatientPanel.BedLocation);
            }

            if (string.IsNullOrWhiteSpace(model.WardDestination))
                model.WardDestination = "—";

            if (string.IsNullOrWhiteSpace(model.CollectorName) && !string.IsNullOrWhiteSpace(currentUserName))
                model.CollectorName = currentUserName.Trim();
            else if (string.IsNullOrWhiteSpace(model.CollectorName))
                model.CollectorName = model.DispensingStaffName?.Trim() ?? "";
        }

        private async Task ValidateRecipientAsync(DispenseFormViewModel model)
        {
            var mode = (model.RecipientMode ?? "request").Trim().ToLowerInvariant();

            if (mode == "request")
            {
                if (!model.RequestId.HasValue)
                    ModelState.AddModelError("Form.RequestId", "Select a doctor blood request from the list.");
                return;
            }

            if (mode == "manual")
            {
                if (string.IsNullOrWhiteSpace(model.PatientPanel?.PatientName))
                    ModelState.AddModelError("Form.PatientPanel.PatientName", "Enter the patient name.");
                if (BloodBankService.IsManualOutsideHospital(model))
                {
                    if (string.IsNullOrWhiteSpace(model.PatientPanel?.Address))
                        ModelState.AddModelError("Form.PatientPanel.Address", "Enter the hospital name.");
                    if (string.IsNullOrWhiteSpace(model.PatientPanel?.Ward))
                        ModelState.AddModelError("Form.PatientPanel.Ward", "Enter the ward at the hospital.");
                }
                if (string.IsNullOrWhiteSpace(model.WardDestination) || model.WardDestination == "—")
                    ModelState.AddModelError("Form.WardDestination", "Select ward / destination.");
                return;
            }

            if (mode == "his" && string.IsNullOrWhiteSpace(model.PatientPanel?.PatientId))
                ModelState.AddModelError("Form.PatientPanel.PatientId", "Enter a HIS patient ID.");

            if (mode == "his" && !string.IsNullOrWhiteSpace(model.PatientPanel?.PatientId))
            {
                var id = model.PatientPanel.PatientId.Trim();
                var exists = await Context.KmuCharts.AnyAsync(c => c.ChrHealthId == id);
                if (!exists)
                {
                    ModelState.AddModelError("Form.PatientPanel.PatientId",
                        "Patient not found in HIS. Switch to \"Not in HisOrder\" to enter details manually.");
                }
            }
        }

        private async Task ReloadUnitDisplayFieldsAsync(DispenseFormViewModel model)
        {
            var unit = await Context.BloodBankBloodUnits
                .Include(u => u.Donor)
                .FirstOrDefaultAsync(u => u.UnitId == model.UnitId);
            if (unit == null) return;
            model.SerialNumber = unit.SerialNumber;
            model.UnitBloodType = unit.BloodType;
            model.UnitComponentType = BloodBankLookups.NormalizeComponent(unit.ComponentType);
            model.UnitExpiryDate = unit.ExpiryDate;
            model.UnitStatus = unit.Status;
            model.UnitDonorId = unit.DonorId;
            model.SourceDonorId ??= unit.DonorId;
            if (unit.Donor != null)
            {
                model.UnitDonorName = BloodBankService.FormatDonorFullName(unit.Donor);
                model.UnitDonorBloodType = unit.Donor.BloodType;
                model.UnitDonorPhone = unit.Donor.Phone;
            }
        }
    }
}
