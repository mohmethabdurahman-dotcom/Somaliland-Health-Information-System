using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KMU.HisOrder.MVC.Models;

namespace KMU.HisOrder.MVC.Areas.Radiology.Controllers
{
    [Area("Radiology")]
    public class TechniciansController : Controller
    {
        private readonly KMUContext _context;

        public TechniciansController(KMUContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get all active rooms grouped by modality
            var rooms = await _context.rooms
                .Where(r => r.IsActive)
                .OrderBy(r => r.modality)
                .ThenBy(r => r.room_number)
                .Select(r => new
                {
                    r.roomid,
                    r.room_number,
                    r.modality,
                    r.department,
                    r.description
                })
                .ToListAsync();

            // Get today's scheduled count per room (exclude cancelled) - count distinct patients (orders)
            var today = DateOnly.FromDateTime(DateTime.Now);
            var scheduledCounts = await _context.radiologyexamrequests
                .Where(r => r.roomid.HasValue && r.ordereddate == today && r.status == "SCHEDULED" && r.status != "CANCELLED")
                .GroupBy(r => r.roomid.Value)
                .Select(g => new { RoomId = g.Key, Count = g.Select(r => r.orderid).Distinct().Count() })
                .ToDictionaryAsync(x => x.RoomId, x => x.Count);

            var viewModel = rooms.Select(r => new
            {
                r.roomid,
                r.room_number,
                r.modality,
                r.department,
                r.description,
                ScheduledCount = scheduledCounts.ContainsKey(r.roomid) ? scheduledCounts[r.roomid] : 0
            }).ToList();

            return View(viewModel);
        }

        public async Task<IActionResult> RoomQueue(int roomId)
        {
            var room = await _context.rooms
                .FirstOrDefaultAsync(r => r.roomid == roomId);

            if (room == null)
            {
                return NotFound();
            }

            ViewBag.RoomNumber = room.room_number;
            ViewBag.Modality = room.modality;
            ViewBag.RoomId = roomId;

            // Get scheduled patients for this room (exclude cancelled)
            var today = DateOnly.FromDateTime(DateTime.Now);
            var requests = await _context.radiologyexamrequests
                .Where(r => r.roomid.HasValue && r.roomid.Value == roomId && r.status == "SCHEDULED" && r.status != "CANCELLED" && r.ordereddate == today)
                .OrderBy(r => r.seq_no)
                .Select(r => new
                {
                    r.id,
                    r.orderid,
                    r.patientid,
                    r.item_id,
                    r.seq_no,
                    r.status,
                    r.remark,
                    r.createdat
                })
                .ToListAsync();

            // Get patient details
            var patientIds = requests.Select(r => r.patientid).Distinct().ToList();
            var patients = await _context.KmuCharts
                .Where(p => patientIds.Contains(p.ChrHealthId))
                .ToDictionaryAsync(p => p.ChrHealthId);

            // Get body part names
            var itemIds = requests.Where(r => !string.IsNullOrEmpty(r.item_id))
                .Select(r => r.item_id).ToList();
            var bodyParts = await _context.KmuNonMedicines
                .Where(nm => itemIds.Contains(nm.ItemId))
                .ToDictionaryAsync(nm => nm.ItemId, nm => nm.ItemName);

            var viewModel = requests.GroupBy(r => r.orderid).Select(g =>
            {
                var firstRequest = g.First();
                var patient = patients.ContainsKey(firstRequest.patientid) ? patients[firstRequest.patientid] : null;

                var orderBodyParts = g.Where(r => !string.IsNullOrEmpty(r.item_id))
                    .Select(r => bodyParts.ContainsKey(r.item_id) ? bodyParts[r.item_id] : "Unknown")
                    .ToList();

                var age = patient?.ChrBirthDate.HasValue == true
                    ? DateTime.Now.Year - patient.ChrBirthDate.Value.Year
                    : (int?)null;

                return new
                {
                    OrderId = firstRequest.orderid,
                    PatientId = firstRequest.patientid,
                    PatientName = patient != null
                        ? $"{patient.ChrPatientFirstname} {patient.ChrPatientMidname} {patient.ChrPatientLastname}".Trim()
                        : "Unknown",
                    Age = age,
                    Sex = patient?.ChrSex ?? "N/A",
                    BodyParts = string.Join(", ", orderBodyParts),
                    TicketNumber = firstRequest.seq_no,
                    Remark = firstRequest.remark,
                    ExamCount = g.Count(),
                    RequestIds = g.Select(r => r.id).ToList()
                };
            }).ToList();

            return View(viewModel);
        }
    }
}
