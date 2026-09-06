using KMU.HisOrder.MVC;
using KMU.HisOrder.MVC.Areas.BloodBank.Filters;
using KMU.HisOrder.MVC.Areas.BloodBank.Models;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Models;
using KMU.HisOrder.MVC.Models.BloodBank;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMU.HisOrder.MVC.Areas.BloodBank.Controllers
{
    [Area("BloodBank")]
    [BloodBankAccess]
    public abstract class BloodBankBaseController : Controller
    {
        protected readonly KMUContext Context;
        protected readonly BloodBankService Service;

        protected BloodBankBaseController(KMUContext context, BloodBankService service)
        {
            Context = context;
            Service = service;
        }

        protected LoginDTO? GetLogin()
        {
            return HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
        }

        protected (string UserId, string UserName) GetCurrentUser()
        {
            var login = GetLogin();
            return (login?.EMPCODE ?? "SYSTEM", login?.EMPNAME ?? "Unknown");
        }

        /// <summary>Blood Bank Admin — full access including Staff Audit.</summary>
        protected bool IsAdmin =>
            User.IsInRole(BloodBankRoles.Admin)
            || User.HasClaim("User_auth_page_Name「" + BloodBankRoles.Admin + "」", BloodBankRoles.Admin);

        protected IActionResult AdminOnly()
        {
            if (!IsAdmin)
                return Forbid();
            return null!;
        }

        protected void SetBloodBankLookups(
            string? selectedBloodType = null,
            string? selectedComponent = null,
            string? selectedUrgency = null,
            string? selectedStatus = null)
        {
            ViewBag.BloodTypes = BloodBankLookups.BloodTypes;
            ViewBag.Components = BloodBankLookups.Components;
            ViewBag.UrgencyLevels = BloodBankLookups.UrgencyLevelOptions;
            ViewBag.Statuses = BloodBankLookups.UnitStatusesList;
            ViewBag.PatientTypes = BloodBankLookups.PatientTypes;
            ViewBag.SelectedBloodType = selectedBloodType;
            ViewBag.SelectedComponent = selectedComponent;
            ViewBag.SelectedUrgency = selectedUrgency;
            ViewBag.SelectedStatus = selectedStatus;
        }

        protected void SetDonorRegistrationLookups()
        {
            var lookups = new DonorRegistrationLookups
            {
                GenderList = Enum.GetValues(typeof(EnumClass.EnumGender)).Cast<EnumClass.EnumGender>().ToList(),
                NationPhoneList = Context.KmuCoderefs
                    .Where(c => c.RefCodetype == "NationalPhone" && c.RefCasetype == "Y")
                    .OrderBy(c => c.RefShowseq)
                    .ToList(),
                AreaList = Context.KmuCoderefs
                    .Where(c => c.RefCodetype == "Chart_Area" && c.RefCasetype == "Y")
                    .OrderBy(c => c.RefShowseq)
                    .ToList()
            };
            ViewBag.DonorRegistrationLookups = lookups;
        }

        protected static void NormalizeDonorRegistrationModel(DonorRegistrationViewModel model)
        {
            model.Phone = DonorPhoneHelper.ComposePhone(model.NationalPhone, model.AreaPhone, model.MobilePhone);
            if (model.AgeType == "Date" && model.BirthDate.HasValue)
                model.Age = DonorPhoneHelper.AgeFromBirthDate(model.BirthDate);
            if (model.Gender == "M") model.Gender = "Male";
            else if (model.Gender == "F") model.Gender = "Female";
        }

        protected static void HydrateDonorPhoneFields(DonorRegistrationViewModel model)
        {
            DonorPhoneHelper.SplitPhone(model.Phone, out var national, out var area, out var mobile);
            model.NationalPhone = string.IsNullOrWhiteSpace(national) ? model.NationalPhone : national;
            model.AreaPhone = string.IsNullOrWhiteSpace(area) ? (model.AreaPhone ?? "063") : area;
            model.MobilePhone = mobile;
            if (string.IsNullOrWhiteSpace(model.NationalPhone))
                model.NationalPhone = "+252";
        }

        protected static DonorRegistrationViewModel MapDonorToRegistrationModel(BloodBankDonor donor, bool forNewVisit)
        {
            var address = donor.Address;
            var areaCode = "";
            if (!string.IsNullOrWhiteSpace(address) && address.Contains(" — "))
            {
                var parts = address.Split(" — ", 2);
                areaCode = parts[0];
                address = parts.Length > 1 ? parts[1] : "";
            }

            var model = new DonorRegistrationViewModel
            {
                FirstName = donor.FirstName,
                MiddleName = donor.MiddleName,
                LastName = donor.LastName,
                Age = donor.Age,
                AgeType = donor.Age.HasValue ? "Age" : "Date",
                JobDescription = donor.JobDescription,
                Gender = donor.Gender,
                Phone = donor.Phone,
                NationalId = donor.NationalId,
                Address = address,
                AreaCode = areaCode,
                RefugeeFlag = donor.DonorNotes?.Contains("Refugee", StringComparison.OrdinalIgnoreCase) == true,
                BloodType = donor.BloodType,
            };

            if (!forNewVisit)
            {
                model.DonorId = donor.DonorId;
                model.DonationType = donor.DonationType;
                model.PatientId = donor.PatientId;
                model.Inhospid = donor.Inhospid;
                model.RequestId = donor.RequestId;
            }

            return model;
        }

        protected async Task SetDashboardMetricsAsync()
        {
            await Service.ExpireOverdueUnitsAsync();
            ViewBag.PendingRequests = await Context.BloodBankRequests.CountAsync(r => r.Status == RequestStatuses.Pending);
            ViewBag.AvailableUnits = await Context.BloodBankBloodUnits.CountAsync(u => u.Status == UnitStatuses.Available);
            ViewBag.PendingScreening = await Context.BloodBankDonors.CountAsync(d =>
                d.Status == DonorStatuses.Registered || d.Status == DonorStatuses.Screening);
            ViewBag.PendingBagAssignment = (await Service.GetBagAssignmentQueueAsync()).Count;
            ViewBag.ExpiringSoon = await Context.BloodBankBloodUnits.CountAsync(u =>
                u.Status == UnitStatuses.Available && u.ExpiryDate <= DateTime.Now.AddDays(7));
            ViewBag.IsAdmin = IsAdmin;
        }
    }
}
