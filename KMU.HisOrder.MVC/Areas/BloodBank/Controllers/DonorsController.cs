using KMU.HisOrder.MVC.Areas.BloodBank.Models;
using KMU.HisOrder.MVC.Models;
using KMU.HisOrder.MVC.Models.BloodBank;
using Microsoft.AspNetCore.Mvc;

namespace KMU.HisOrder.MVC.Areas.BloodBank.Controllers
{
    public class DonorsController : BloodBankBaseController
    {
        public DonorsController(KMUContext context, BloodBankService service) : base(context, service) { }

        public async Task<IActionResult> Index()
        {
            ViewBag.Donors = await Service.ListDonorsAsync();
            return View();
        }

        /// <summary>Find donor — MRFind-style (donor ID, phone, name).</summary>
        public IActionResult Find()
        {
            ViewData["Title"] = "Blood Bank → Find Donor";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DonorSearch(
            string? donorId,
            string? phone,
            string? name,
            string? nationalId,
            string? bloodType,
            string? status,
            bool repeatOnly = false)
        {
            if (string.IsNullOrWhiteSpace(donorId)
                && string.IsNullOrWhiteSpace(phone)
                && string.IsNullOrWhiteSpace(name)
                && string.IsNullOrWhiteSpace(nationalId)
                && string.IsNullOrWhiteSpace(bloodType)
                && string.IsNullOrWhiteSpace(status)
                && !repeatOnly)
            {
                return PartialView("PartialViews/_DonorSearchTable",
                    new DonorSearchResultViewModel { EmptyParam = true });
            }

            var results = await Service.SearchDonorsMrFindAsync(
                donorId, phone, name, nationalId, bloodType, status, repeatOnly);
            return PartialView("PartialViews/_DonorSearchTable",
                new DonorSearchResultViewModel
                {
                    Results = results,
                    Truncated = results.Count >= 100
                });
        }

        /// <summary>Search donor lifetime donations — redirects to Find with table.</summary>
        public IActionResult History(string? q)
        {
            if (!string.IsNullOrWhiteSpace(q))
                return RedirectToAction(nameof(Find), new { donorId = q, phone = q });
            return RedirectToAction(nameof(Find));
        }

        public async Task<IActionResult> Profile(long? donorId, string? phone)
        {
            var profile = await Service.GetDonorHistoryProfileAsync(donorId, phone);
            if (profile == null)
            {
                TempData["Error"] = "Donor not found. Search by donor ID or phone number.";
                return RedirectToAction(nameof(History));
            }
            ViewBag.CanEditDonor = await Service.CanEditDonorAsync(profile.PrimaryDonorId);
            return View(profile);
        }

        public async Task<IActionResult> Edit(long donorId)
        {
            if (!await Service.CanEditDonorAsync(donorId))
            {
                TempData["Error"] = "This donor cannot be edited after bag assignment.";
                return RedirectToAction(nameof(Profile), new { donorId });
            }

            var donor = await Context.BloodBankDonors.FindAsync(donorId);
            if (donor == null) return NotFound();

            var model = MapDonorToRegistrationModel(donor, forNewVisit: false);
            HydrateDonorPhoneFields(model);
            if (!string.IsNullOrWhiteSpace(donor.PatientId))
                model.PatientPanel = await Service.GetPatientPanelAsync(donor.PatientId, donor.RequestId);

            SetBloodBankLookups();
            SetDonorRegistrationLookups();
            ViewBag.IsDonorEdit = true;
            return View(model);
        }

        /// <summary>HIS-style phone check during registration — returns existing donors for this mobile number.</summary>
        [HttpGet]
        public async Task<IActionResult> PhoneChecker(string? phone, string? nationalPhone, string? areaPhone, string? mobilePhone)
        {
            var composed = !string.IsNullOrWhiteSpace(phone)
                ? phone.Trim()
                : DonorPhoneHelper.ComposePhone(nationalPhone, areaPhone, mobilePhone);

            var mobile = mobilePhone?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(composed) && mobile.Length < 5)
                return Json(new { success = true, items = Array.Empty<object>() });

            var results = await Service.SearchDonorsMrFindAsync(null, composed, null);
            if (results.Count == 0 && mobile.Length >= 5)
                results = await Service.SearchDonorsMrFindAsync(null, mobile, null);

            return Json(new
            {
                success = true,
                items = results.Select(i => new
                {
                    i.PrimaryDonorId,
                    bloodBankDonorId = i.BloodBankDonorId,
                    i.FullName,
                    i.Phone,
                    i.Gender,
                    i.BloodType,
                    i.LifetimeVisits,
                    lastVisit = i.LastVisit?.ToString("dd/MM/yyyy HH:mm"),
                    i.LastStatus,
                    profileUrl = Url.Action(nameof(Profile), new { donorId = i.PrimaryDonorId }),
                    newVisitUrl = Url.Action(nameof(Create), new { donorId = i.PrimaryDonorId })
                })
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DonorRegistrationViewModel model)
        {
            if (!model.DonorId.HasValue)
                return NotFound();

            if (model.DonationType == DonationTypes.Directed && string.IsNullOrWhiteSpace(model.PatientId))
                ModelState.AddModelError(nameof(model.PatientId), "Patient ID is required for directed donation.");

            NormalizeDonorRegistrationModel(model);
            if (string.IsNullOrWhiteSpace(model.Phone))
                ModelState.AddModelError(nameof(model.Phone), "Mobile phone is required.");
            if (string.IsNullOrWhiteSpace(model.Gender))
                ModelState.AddModelError(nameof(model.Gender), "Sex is required.");

            if (!ModelState.IsValid)
            {
                SetBloodBankLookups();
                SetDonorRegistrationLookups();
                if (!string.IsNullOrWhiteSpace(model.PatientId))
                    model.PatientPanel = await Service.GetPatientPanelAsync(model.PatientId, model.RequestId);
                return View(model);
            }

            var (userId, _) = GetCurrentUser();
            var result = await Service.UpdateDonorAsync(model, userId);
            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                SetBloodBankLookups();
                SetDonorRegistrationLookups();
                ViewBag.IsDonorEdit = true;
                return View(model);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Profile), new { donorId = model.DonorId });
        }

        [HttpGet]
        public async Task<IActionResult> SearchHistory(string? q)
        {
            var items = await Service.SearchDonorHistoryAsync(q);
            return Json(new { success = true, items = items.Select(i => ToDonorLookupJson(i)) });
        }

        /// <summary>HIS-style donor lookup by BB-DONOR id, numeric id, or phone.</summary>
        [HttpGet]
        public async Task<IActionResult> LookupDonor(string? term, string? q)
        {
            var query = (term ?? q)?.Trim();
            if (string.IsNullOrWhiteSpace(query))
                return Json(new { success = false, message = "Enter Blood Bank donor ID (BB-DONOR) or phone number." });

            var hits = await Service.SearchDonorHistoryAsync(query);
            if (hits.Count == 0)
                return Json(new { success = false, message = "No donor found. Check BB-DONOR id or phone and try again." });

            if (hits.Count == 1)
                return Json(new { success = true, single = true, data = ToDonorLookupJson(hits[0]) });

            return Json(new { success = true, single = false, items = hits.Select(i => ToDonorLookupJson(i)) });
        }

        private object ToDonorLookupJson(DonorHistorySearchHitViewModel i) => new
        {
            i.PrimaryDonorId,
            bloodBankDonorId = BloodBankIds.FormatDonor(i.PrimaryDonorId),
            i.FullName,
            i.Phone,
            i.BloodType,
            i.LifetimeVisits,
            i.SuccessfulDonations,
            lastVisit = i.LastVisit?.ToString("dd/MM/yyyy HH:mm"),
            i.LastStatus,
            profileUrl = Url.Action(nameof(Profile), "Donors", new { area = "BloodBank", donorId = i.PrimaryDonorId })
        };

        public async Task<IActionResult> Create(long? donorId, string? patientId, long? requestId)
        {
            DonorRegistrationViewModel model;

            if (donorId.HasValue)
            {
                var donor = await Context.BloodBankDonors.FindAsync(donorId.Value);
                if (donor == null) return NotFound();

                model = MapDonorToRegistrationModel(donor, forNewVisit: true);
                ViewBag.IsReturnVisit = true;
                ViewBag.SourceDonorDisplayId = BloodBankIds.FormatDonor(donor.DonorId);
                ViewData["Title"] = "Blood Bank → New Visit — " + BloodBankService.FormatDonorFullName(donor);
            }
            else
            {
                model = new DonorRegistrationViewModel();
                if (!string.IsNullOrWhiteSpace(patientId))
                {
                    model.PatientId = patientId;
                    model.DonationType = DonationTypes.Directed;
                    model.RequestId = requestId;
                    model.PatientPanel = await Service.GetPatientPanelAsync(patientId, requestId);
                    if (model.PatientPanel != null)
                    {
                        model.Inhospid = model.PatientPanel.Inhospid;
                        if (!string.IsNullOrWhiteSpace(model.PatientPanel.BloodType))
                            model.BloodType = model.PatientPanel.BloodType;
                    }
                }
                else if (requestId.HasValue)
                {
                    var req = await Context.BloodBankRequests.FindAsync(requestId.Value);
                    if (req != null && req.Status == RequestStatuses.Pending)
                    {
                        model.PatientId = req.PatientId;
                        model.Inhospid = req.Inhospid;
                        model.RequestId = req.RequestId;
                        model.DonationType = DonationTypes.Directed;
                        model.BloodType = req.BloodType;
                        model.PatientPanel = await Service.GetPatientPanelAsync(req.PatientId, req.RequestId);
                    }
                }
            }

            HydrateDonorPhoneFields(model);
            if (string.IsNullOrWhiteSpace(model.NationalPhone))
                model.NationalPhone = "+252";
            if (string.IsNullOrWhiteSpace(model.AreaPhone))
                model.AreaPhone = "063";
            SetBloodBankLookups();
            SetDonorRegistrationLookups();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DonorRegistrationViewModel model)
        {
            if (model.DonationType == DonationTypes.Directed && string.IsNullOrWhiteSpace(model.PatientId))
                ModelState.AddModelError(nameof(model.PatientId), "Patient ID is required for directed donation.");

            NormalizeDonorRegistrationModel(model);
            if (string.IsNullOrWhiteSpace(model.Phone))
                ModelState.AddModelError(nameof(model.Phone), "Mobile phone is required.");
            if (string.IsNullOrWhiteSpace(model.Gender))
                ModelState.AddModelError(nameof(model.Gender), "Sex is required.");
            if (string.IsNullOrWhiteSpace(model.PreDonationBloodPressure))
                ModelState.AddModelError(nameof(model.PreDonationBloodPressure), "Blood pressure is required at registration.");

            if (!ModelState.IsValid)
            {
                SetBloodBankLookups();
                SetDonorRegistrationLookups();
                if (!string.IsNullOrWhiteSpace(model.PatientId))
                    model.PatientPanel = await Service.GetPatientPanelAsync(model.PatientId, model.RequestId);
                return View(model);
            }

            var (userId, _) = GetCurrentUser();
            var result = await Service.RegisterDonorAsync(model, userId, userName: userId);
            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                SetBloodBankLookups();
                SetDonorRegistrationLookups();
                return View(model);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction("Index", "Screening", new { area = "BloodBank" });
        }

        [HttpGet]
        public async Task<IActionResult> LookupPatient(string patientId, string? term)
        {
            var id = (patientId ?? term)?.Trim();
            if (string.IsNullOrWhiteSpace(id))
                return Json(new { success = false, message = "Enter a patient ID." });

            var panel = await Service.GetPatientPanelAsync(id);
            if (panel == null)
                return Json(new { success = false, message = "Patient not found in HIS." });
            return Json(new { success = true, data = panel });
        }

        /// <summary>Pending HIS blood requests for directed donor linkage.</summary>
        [HttpGet]
        public async Task<IActionResult> SearchPendingRequests(string? term)
        {
            var items = await Service.SearchPendingBloodRequestsAsync(term);
            foreach (var item in items)
            {
                item.ComponentType = BloodBankLookups.NormalizeComponent(item.ComponentType);
            }
            return Json(new { success = true, items });
        }

        /// <summary>Pre-donation screening deferral — compliance audit log.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogPreDonationDeferral([FromBody] PreDonationDeferralViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid deferral payload." });

            var (userId, _) = GetCurrentUser();
            var result = await Service.LogPreDonationDeferralAsync(model, userId);
            return Json(new { success = result.Success, message = result.Message });
        }
    }
}
