using KMU.HisOrder.MVC.Areas.BloodBank.Models;
using KMU.HisOrder.MVC.Models;
using KMU.HisOrder.MVC.Models.BloodBank;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMU.HisOrder.MVC.Areas.BloodBank.Controllers
{
    public class AuditController : BloodBankBaseController
    {
        public AuditController(KMUContext context, BloodBankService service) : base(context, service) { }

        public async Task<IActionResult> Index(AuditLogViewModel filters)
        {
            if (!IsAdmin)
                return Forbid();

            var screeningQ = Context.BloodBankScreeningRecords
                .Include(s => s.Unit)
                .AsQueryable();
            var dispenseQ = Context.BloodBankDispenseRecords.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filters.FilterStaff))
            {
                screeningQ = screeningQ.Where(s => s.StaffUserId.Contains(filters.FilterStaff) || s.StaffName.Contains(filters.FilterStaff));
                dispenseQ = dispenseQ.Where(d => d.DispensingStaffId.Contains(filters.FilterStaff) || d.DispensingStaffName.Contains(filters.FilterStaff));
            }
            if (!string.IsNullOrWhiteSpace(filters.FilterPatient))
                dispenseQ = dispenseQ.Where(d => d.PatientId.Contains(filters.FilterPatient) || d.PatientName.Contains(filters.FilterPatient));
            if (!string.IsNullOrWhiteSpace(filters.FilterBloodType))
                screeningQ = screeningQ.Where(s => s.Unit.BloodType == filters.FilterBloodType);
            if (filters.DateFrom.HasValue)
            {
                screeningQ = screeningQ.Where(s => s.ScreeningDateTime >= filters.DateFrom.Value);
                dispenseQ = dispenseQ.Where(d => d.DispenseDateTime >= filters.DateFrom.Value);
            }
            if (filters.DateTo.HasValue)
            {
                screeningQ = screeningQ.Where(s => s.ScreeningDateTime <= filters.DateTo.Value);
                dispenseQ = dispenseQ.Where(d => d.DispenseDateTime <= filters.DateTo.Value);
            }

            filters.Screenings = await screeningQ.OrderByDescending(s => s.ScreeningDateTime).Take(500).ToListAsync();
            filters.Dispenses = await dispenseQ.OrderByDescending(d => d.DispenseDateTime).Take(500).ToListAsync();
            return View(filters);
        }
    }
}
