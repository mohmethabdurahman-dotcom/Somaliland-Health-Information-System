using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Areas.InPatient.ViewModels;
using KMU.HisOrder.MVC.Extesion;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;

namespace KMU.HisOrder.MVC.Areas.InPatient.Controllers
{
    [Area("InPatient")]
    public class ReceptionController : Controller
    {
        private readonly KMUContext _context;

        public ReceptionController(KMUContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {

            return View();
        }


        [HttpPost]
        public IActionResult SearchList(string? name, string? id, string? phone)
        {
            var chartList = new List<KmuChart>();
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(id) && string.IsNullOrEmpty(phone))
            {
                TempData["error"] = "please at least fill one input";
                return PartialView("_SearchResults", chartList);
            }
            chartList = _context.KmuCharts.Where(c => 1 == 1).ToList();
            //check if patient is admitted
            //join if InpatientReservation
            if (!string.IsNullOrEmpty(id))
            {
                chartList = chartList.Where(c => c.ChrHealthId.Contains(id.ToUpper().Trim())).Take(5).ToList();
            }

            if (!string.IsNullOrEmpty(phone))
            {
                chartList = chartList.Where(c => c.ChrMobilePhone.Contains(phone.Trim())).Take(5).ToList();
            }


            if (!string.IsNullOrEmpty(name))
            {
                chartList = chartList.Where(c => (c.ChrPatientFirstname.ToUpper().Contains(name.ToUpper().Trim()) ||
                                                  c.ChrPatientMidname.ToUpper().Contains(name.ToUpper().Trim()) ||
                                                  c.ChrPatientLastname.ToUpper().Contains(name.ToUpper().Trim()))).Take(5).ToList();
            }
            ViewBag.chartList = chartList;
            return PartialView("_SearchResults", chartList);
        }

        [HttpGet]
        public IActionResult TransferList()
        {
            var transferPatient = from kmu in _context.KmuCharts
                                  join inr in _context.InpatientReservations
                                  on kmu.ChrHealthId equals inr.healthId
                                  where inr.status == "transfer"
                                  select new
                                  {
                                      healthId = inr.healthId,
                                      ChrMobilePhone = kmu.ChrMobilePhone,
                                      fullname = kmu.ChrPatientFirstname + " " + kmu.ChrPatientMidname + " " + kmu.ChrPatientLastname,
                                      ChrAddress = kmu.ChrAddress,
                                      ChrSex = kmu.ChrSex,
                                      status = inr.status,
                                  };

            //var chartList = _context.InpatientReservations.Where(p => p.status == "transfer");

            ViewBag.TransferPatient = transferPatient.ToList();
            return PartialView("_TransferResults");
        }

        public IActionResult Reservation(string id)
        {
            // Fetch all wards from the database
            var wards = _context.Wards.ToList();
            var transfer = _context.KmuCoderefs
                .Where(r => r.RefCodetype == "transfere")
                .OrderBy(r => r.RefShowseq)
                .Select(r => new { r.RefCode, r.RefName })
                .ToList();
            ViewBag.Departments = _context.KmuDepartments.Where(d=>d.DptParent == "" && d.DptCode != "1700").ToList();

            // Get all distinct DptCode values from wards
            var distinctDptCodes = wards.Select(w => w.department).Distinct().ToList();
            // Fetch distinct departments from the database based on DptCode
            List<KmuDepartment> departments = _context.KmuDepartments
                .Where(d => distinctDptCodes.Contains(d.DptCode))
                .ToList();

            var patient = _context.KmuCharts.FirstOrDefault(u => u.ChrHealthId == id);
            var reservation = _context.InpatientReservations.FirstOrDefault(p => p.healthId == id);
            
            if (reservation != null)
            {
                var Ward = _context.Wards
                    .Where(w => w.wardid == reservation.wardId)
                    .Select(w => new
                    {
                        wardid = w.wardid,
                        wardName = w.wardName,
                    })
                    .FirstOrDefault();
                ViewBag.Ward = Ward;

                var Department = _context.KmuDepartments
                        .Where(d => d.DptCode == reservation.department)
                        .Select(d => new
                        {
                            departmentName = d.DptName,
                            departmentId = d.DptCode,

                        })
                        .FirstOrDefault();
                ViewBag.Department = Department;
            }
            CommonService cService = new CommonService(_context);

            var viewModel = new ReservationVM
            {
                Patient = patient,
                Departments = departments,
                Wards = _context.Wards.ToList(),
                AvailableBeds = _context.beds.ToList(),
                Reservation = reservation,
                Shift = cService.GetShiftbytime(DateTime.Now),
            };
            if (viewModel == null)
            {
                return Content("Reservation not found.");
            }
            return PartialView("_Reservation", viewModel);
        }

     

        [HttpGet]
        public IActionResult GetWardsByDepartment(string dpt_code)
        {
            Console.WriteLine(dpt_code + "here");
            // Check if dpt_code is null or empty before querying
            if (string.IsNullOrEmpty(dpt_code))
            {
                return Json(new { success = false, message = "Department code is required." });

            }
            //dpt_code = "0401";

            // Query the database to get wards that match the department code
            var wards = _context.Wards
                                .Where(w => w.department == dpt_code)  // Make sure this matches your column name
                                .Select(w => new { w.wardid, w.wardName })
                                .ToList();


            // Check if no wards were found
            if (!wards.Any())
            {
                return Json(new { success = false, message = "No wards found for the selected department!." });
            }

            return Json(wards); ;
        }

        [HttpGet]
        public IActionResult GetBedsByWard(string wardid)
        {
            if (wardid == null)
            {
                return Json(new { success = false, message = "Ward code is required." });
            }
            // Fetch beds from the database based on wardId
            var beds = _context.beds
                               .Where(b => b.wardId == wardid && b.status == "Available")  // Assuming you have a WardId in your Bed table
                               .Select(b => new { b.bedId, b.bedName })  // Select BedId and BedName to return
                               .ToList();

            if (!beds.Any())
            {
                return Json(new { success = false, message = "No beds found for the selected ward" });
            }
            return Json(beds);
        }

        [HttpPost]
        public async Task<IActionResult> SaveReservations([FromBody] InpatientReservation reservation)
        {
            // Retrieve the login info from session
            var loginUser = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

            // Asynchronously check if the patient is already admitted or if a transfer exists
            bool isAdmitted = await _context.InpatientReservations
                .AnyAsync(p => p.healthId == reservation.healthId && p.status == "admitted");
            bool isTransfer = await _context.InpatientReservations
                .AnyAsync(p => p.healthId == reservation.healthId && p.status == "transfer");

            // Retrieve the bed and verify its existence
            var bed = await _context.beds.FirstOrDefaultAsync(b => b.bedId == reservation.bedId);
            if (bed == null)
            {
                return Json(new { success = false, message = "Bed not found." });
            }

            var existingReservation = await _context.InpatientReservations
                .FirstOrDefaultAsync(r => r.healthId == reservation.healthId && r.inhospId == reservation.inhospId);

            if (isAdmitted)
            {
                return Json(new { success = false, message = "Patient is already admitted." });
            }

            // For transfer cases, update the existing reservation.
            if (isTransfer)
            {
                await using var transactionT = await _context.Database.BeginTransactionAsync();
                try
                {
                    if (existingReservation == null)
                        return Json(new { success = false, message = "Reservation not found." });

                    // Update the bed status to "Occupied"
                    bed.status = "Occupied";
                    _context.beds.Update(bed);

                    // Update the existing reservation with new bed details and timestamps.
                    existingReservation.bedId = reservation.bedId;
                    existingReservation.status = reservation.status ?? "admitted";//Needs to be changed to Enum
                    existingReservation.modifyBy = loginUser.EMPCODE;
                    existingReservation.modifyAt = reservation.reserveDate;
                    _context.InpatientReservations.Update(existingReservation);

                    // Perform a single SaveChanges call for both updates
                    await _context.SaveChangesAsync();
                    await transactionT.CommitAsync();

                    return Json(new { success = true, message = "Reservation updated successfully." });
                }
                catch (Exception)
                {
                    await transactionT.RollbackAsync();
                    // Consider logging the exception details here for diagnostics.
                    return Json(new { success = false, message = "An unexpected error occurred." });
                }
            }

            // For new reservations, add a new record.
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update bed status
                bed.status = "Occupied";
                _context.beds.Update(bed);

                // Set default values for reservation
                reservation.reserveDate = reservation.reserveDate;
                reservation.status ??= "admitted";
                reservation.inhospId = "";
                
                reservation.createBy ??= loginUser.EMPCODE;

                // Add the new reservation
                _context.InpatientReservations.Add(reservation);

                // Save all changes together in one call.
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Reservation created successfully." });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                // Consider logging the exception details here for diagnostics.
                return Json(new { success = false, message = "An unexpected error occurred." });
            }
        }

    }
}
