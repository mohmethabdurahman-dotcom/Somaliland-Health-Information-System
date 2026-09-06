using KMU.HisOrder.MVC.Hubs;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace KMU.HisOrder.MVC.Areas.Radiology.Controllers
{
    [Area("Radiology")]
    [AllowAnonymous]
    public class RisCallingsController : Controller
    {
        private readonly KMUContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public RisCallingsController(KMUContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Display calling monitor for radiology rooms
        /// </summary>
        public async Task<IActionResult> Monitor()
        {
            // Get all active rooms
            var rooms = await _context.rooms
                .Where(r => r.IsActive)
                .OrderBy(r => r.department)
                .ThenBy(r => r.modality)
                .ThenBy(r => r.room_number)
                .Select(r => new
                {
                    r.roomid,
                    r.room_number,
                    r.modality,
                    r.department
                })
                .ToListAsync();

            // Map rooms to HIS Calling format
            var roomList = rooms.Select(r => new
            {
                ScheRoom = $"ROOM_{r.roomid}",
                RoomNumber = r.room_number,
                Modality = r.modality,
                Department = r.department,
                ScheCallNo = 0 // Initial call number
            }).ToList();

            ViewData["VD_clinic"] = roomList;
            ViewData["VD_clock"] = true; // Show clock
            
            return View();
        }

        /// <summary>
        /// Call a patient - broadcast via SignalR (no database update)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CallPatient(int roomId, int ticketNumber)
        {
            try
            {
                // Get room info
                var room = await _context.rooms.FindAsync(roomId);
                if (room == null)
                {
                    return Json(new { success = false, message = "Room not found." });
                }

                // Broadcast via SignalR (no database update)
                var message = new
                {
                    iRoom = $"ROOM_{roomId}",
                    iNO = ticketNumber.ToString()
                };

                await _hubContext.Clients.All.SendAsync("UpdContent", JsonConvert.SerializeObject(message));

                return Json(new 
                { 
                    success = true, 
                    ticketNumber = ticketNumber,
                    roomNumber = room.room_number
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

    }
}
