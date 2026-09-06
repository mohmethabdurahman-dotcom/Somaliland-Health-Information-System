using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Models;
using KMU.HisOrder.MVC.Models.BloodBank;
using Microsoft.EntityFrameworkCore;

namespace KMU.HisOrder.MVC.Areas.BloodBank.Models
{
    /// <summary>
    /// Bridges HIS clinical orders (Hisorderplan) with the Blood Bank bounded context.
    /// </summary>
    public class BloodBankIntegrationService
    {
        private readonly KMUContext _context;
        private readonly BloodBankService _bloodBankService;

        public BloodBankIntegrationService(KMUContext context, BloodBankService bloodBankService)
        {
            _context = context;
            _bloodBankService = bloodBankService;
        }

        public async Task CreateRequestFromBloodOrderAsync(Hisorderplan order, GlobalVariableDTO grv)
        {
            if (order.HplanType != "Blood") return;

            if (await _context.BloodBankRequests.AnyAsync(r => r.Orderplanid == order.Orderplanid))
                return;

            var patient = grv.Patient;
            var clinic = grv.Clinic;
            var login = grv.Login;

            var chart = await _context.KmuCharts.FirstOrDefaultAsync(c => c.ChrHealthId == order.HealthId);
            var patientName = chart != null
                ? $"{chart.ChrPatientFirstname} {chart.ChrPatientMidname} {chart.ChrPatientLastname}".Trim()
                : patient?.RegPatientId ?? order.HealthId;

            var reg = await _context.Registrations.FirstOrDefaultAsync(r => r.Inhospid == order.Inhospid);
            var ward = clinic?.WardId ?? "";
            var bed = patient?.bed ?? reg?.RegBedNo ?? "";
            var patientType = clinic?.InhospType ?? (string.IsNullOrEmpty(ward) ? "OPD" : "IPD");

            var request = new BloodBankRequest
            {
                Orderplanid = order.Orderplanid,
                PatientId = order.HealthId.Trim(),
                Inhospid = order.Inhospid,
                PatientName = patientName,
                Ward = ward,
                BedLocation = bed,
                RequestingDoctorId = order.OrderDr ?? login?.EMPCODE ?? "SYSTEM",
                RequestingDoctorName = clinic?.DoctorName ?? order.OrderDr ?? login?.EMPNAME ?? "Unknown",
                BloodType = BloodOrderRequestMapper.ResolveRequestedBloodType(order),
                ComponentType = BloodOrderRequestMapper.ResolveComponent(order),
                UnitsRequested = (int)(order.QtyDose ?? order.TotalQty ?? 1),
                UrgencyLevel = BloodOrderRequestMapper.MapUrgency(order.UrgFlag),
                PatientType = patientType,
                Status = RequestStatuses.Pending,
                RequestDateTime = order.CreateDate ?? DateTime.Now,
                CreateUser = order.CreateUser,
                CreateDate = DateTime.Now
            };

            _context.BloodBankRequests.Add(request);
            await _context.SaveChangesAsync();

            await _bloodBankService.WritePatientEventAsync(
                request.PatientId,
                request.Inhospid,
                PatientEventTypes.DoctorBloodRequest,
                $"Doctor blood request by {request.RequestingDoctorName}. Type: {request.BloodType}, Component: {request.ComponentType}, Units: {request.UnitsRequested}, Urgency: {request.UrgencyLevel}.",
                order.CreateUser);
        }

        /// <summary>
        /// Create Dr Requests for any HIS blood orders that are not yet linked (no Fin/confirm required).
        /// </summary>
        public async Task ImportUnlinkedBloodOrdersFromHisAsync()
        {
            var linkedIds = await _context.BloodBankRequests
                .Where(r => r.Orderplanid != null)
                .Select(r => r.Orderplanid!.Value)
                .ToListAsync();

            var linkedSet = linkedIds.ToHashSet();
            var orders = await _context.Hisorderplans
                .Where(o => o.HplanType == "Blood"
                    && o.DcDate == null
                    && (o.Status == '0' || o.Status == '2')
                    && !string.IsNullOrWhiteSpace(o.DosePath))
                .OrderByDescending(o => o.CreateDate)
                .Take(500)
                .ToListAsync();

            foreach (var order in orders.Where(o => !linkedSet.Contains(o.Orderplanid)))
            {
                var grv = new GlobalVariableDTO
                {
                    Patient = new PatientDTO
                    {
                        RegPatientId = order.HealthId,
                        Inhospid = order.Inhospid
                    },
                    Clinic = new ClinicDTO(),
                    Login = new LoginDTO { EMPCODE = order.CreateUser ?? "SYSTEM", EMPNAME = order.CreateUser ?? "SYSTEM" }
                };
                await CreateRequestFromBloodOrderAsync(order, grv);
            }
        }

        /// <summary>
        /// Updates the blood bank request linked to a HIS order after NonMed blood type / urgency changes.
        /// </summary>
        public async Task SyncRequestForOrderplanAsync(long orderplanid)
        {
            var request = await _context.BloodBankRequests
                .FirstOrDefaultAsync(r => r.Orderplanid == orderplanid);
            if (request == null) return;

            var order = await _context.Hisorderplans.FindAsync(orderplanid);
            if (order == null || order.HplanType != "Blood") return;

            if (BloodOrderRequestMapper.ApplyHisOrder(request, order))
            {
                request.ModifyDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Refreshes pending requests from linked HIS blood orders (NonMed DosePath / UrgFlag).
        /// </summary>
        public async Task SyncRequestsFromHisOrdersAsync(IEnumerable<BloodBankRequest> requests)
        {
            var linked = requests.Where(r => r.Orderplanid.HasValue).ToList();
            if (linked.Count == 0) return;

            var orderIds = linked.Select(r => r.Orderplanid!.Value).Distinct().ToList();
            var orders = await _context.Hisorderplans
                .Where(o => orderIds.Contains(o.Orderplanid))
                .ToDictionaryAsync(o => o.Orderplanid);

            var changed = false;
            foreach (var request in linked)
            {
                if (request.Orderplanid.HasValue
                    && orders.TryGetValue(request.Orderplanid.Value, out var order)
                    && BloodOrderRequestMapper.ApplyHisOrder(request, order))
                {
                    request.ModifyDate = DateTime.Now;
                    changed = true;
                }
            }

            if (changed)
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}
