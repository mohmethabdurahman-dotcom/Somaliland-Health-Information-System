using Humanizer;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Areas.InPatient.ViewModels;
using KMU.HisOrder.MVC.Areas.VitalSign.Models;
using KMU.HisOrder.MVC.Extesion;
using KMU.HisOrder.MVC.Extesion;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Data.SqlClient.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using NuGet.Protocol.Plugins;
using System;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Intrinsics.Arm;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
namespace KMU.HisOrder.MVC.Areas.InPatient.Controllers
{
    [Area("InPatient")]
    public class WardController : Controller
    {

        private readonly KMUContext _context;

        public WardController(KMUContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {

            //get all the wards
            var wards = _context.Wards.ToList();

            //get all the distinct dpt_code from the wards
            //var dptCodes = _context.Wards.Select(w => w.department).ToList();

            //List<KmuDepartment> departments = _context.KmuDepartments.Where(d=> dptCodes.Contains(d.DptCode)).ToList();
            return View(wards);
        }

        public IActionResult Duty(string healthid, string inhospid)
        {
            var findreservation = _context.InpatientReservations.Where(p=> p.healthId == healthid && p.inhospId == inhospid).FirstOrDefault();

            return View(findreservation);
        }

        [HttpPost]
        public IActionResult EditDuty(string inhospid, string doctor, string nurse)
        {
            var record = _context.InpatientReservations.FirstOrDefault(x => x.inhospId == inhospid);

            if (record == null)
                return NotFound();

            // Update ONLY allowed fields
            record.doctor = doctor;
            record.nurse = nurse;

            _context.SaveChanges();

            return Json(record);
        }

        public IActionResult Ward(string wardId)
        {
            // get all the patient ids from inpatient reservation
            var reservations = _context.InpatientReservations.Where(w => w.wardId == wardId && w.status == "admitted").Select(p => p.healthId).ToList();

            //get All Patients in the Ward Count()
            var patients = _context.KmuCharts.Where(r => reservations.Contains(r.ChrHealthId)).Count();

            //get All Beds in the Ward Count()
            var beds = _context.beds.Where(b => b.wardId == wardId).Count();

            //get all the Available Beds in the Ward Count()
            var availableBeds = _context.beds.Where(b => b.wardId == wardId && b.status == "Available").Count();

            //get all the Occupied Beds in the Ward Count()
            var occupiedBeds = _context.beds.Where(b => b.wardId == wardId && b.status == "Occupied").Count();

            //get all the patient reserved for the ward
            var results = (from ir in _context.InpatientReservations
                           join kc in _context.KmuCharts
                           on ir.healthId equals kc.ChrHealthId
                           join bd in _context.beds
                           on ir.bedId equals bd.bedId
                           where ir.wardId == wardId && ir.status == "admitted"
                           select new patientlist
                           {
                               reserveDate = ir.reserveDate,
                               healthid = ir.healthId,
                               inhospId = ir.inhospId,
                               fullname = kc.ChrPatientFirstname + " " + kc.ChrPatientMidname + " " + kc.ChrPatientLastname,
                               age = kc.ChrBirthDate,
                               address = kc.ChrAddress,
                               phone = kc.ChrMobilePhone,
                               sex = kc.ChrSex,
                               status = ir.status,
                               bed = bd.bedName,
                               doctor = ir.doctor,
                               nurse = ir.nurse,
                           }).ToList(); ;

            var WardName = _context.Wards.FirstOrDefault(b => b.wardid == wardId);
            var Department = _context.KmuDepartments
                       .Where(d => d.DptCode == WardName.department)
                       .Select(d => new {
                           departmentName = d.DptName,
                           departmentId = d.DptCode,

                       })
                       .FirstOrDefault();
            ViewBag.WardName = WardName;
            ViewBag.Department = Department;
            ViewBag.ReservationList = results;
            ViewBag.AllPatients = patients;
            ViewBag.AllBeds = beds;
            ViewBag.AvailableBeds = availableBeds;
            ViewBag.OccupiedBeds = occupiedBeds;
            return View(results);
        }


        public IActionResult Medication(string healthid,string inhospid)
        {
            
            var patient = _context.KmuCharts.FirstOrDefault(u => u.ChrHealthId == healthid);
            if (patient == null)
                return NotFound();

            MedicalAdministrationVM medicalAdministrationVM = new MedicalAdministrationVM
            {
                administeredMedication = _context.Hisorderplans.Where(m => m.HealthId == healthid && m.Inhospid == inhospid && m.HplanType == "Med" && m.DcStatus!='2').ToList(),
                Patient = patient,
                MilkAdministration = _context.MedicalAdministrations.Where(m => m.healthId == healthid && m.inhospId == inhospid && m.medicalType == "milk").OrderByDescending(m => m.admisteredAt).Take(3).ToList(),
                MedicalAdministration = _context.MedicalAdministrations.Where(m => m.healthId == healthid && m.inhospId == inhospid &&  m.medicalType == "medication").OrderByDescending(m => m.admisteredAt).Take(3).ToList(),
                inhospid = inhospid,
            };
            return PartialView("_Medication", medicalAdministrationVM);
        }
        [HttpPost]
        public IActionResult SaveMilk([FromBody] MedicalAdministration model)
        {
            CommonService cService = new CommonService(_context);

            var loginUser = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

            if (ModelState.IsValid)
            {
                model.shift = cService.GetShift();
                model.administeredBy = loginUser.EMPCODE;
                model.inhospId = model.inhospId;
                model.admisteredAt = DateTime.UtcNow;
                _context.MedicalAdministrations.Add(model);
                _context.SaveChanges();
                return Json(new { success = true, message = "Milk created successfully." });
            }
            return Json(new { success = false, message = "Error occured while saving!." });
        }

        [HttpPost]
        public IActionResult SaveMedication([FromBody] List<MedicalAdministration> model)
        {
            var loginUser = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

            CommonService cService = new CommonService(_context);
            
            if (ModelState.IsValid)
            {
                foreach (var med in model)
                {
                    var MedicalAdministration = new MedicalAdministration
                    {
                        healthId = med.healthId,
                        admisteredAt = DateTime.Now,
                        shift = cService.GetShift(),
                        medDes = med.medDes,
                        medicalType = med.medicalType,
                        medCode = med.medCode,
                        administeredBy = loginUser.EMPCODE,
                        inhospId = med.inhospId,
                    };
                    _context.MedicalAdministrations.Add(MedicalAdministration);

                }
                _context.SaveChanges();

                return Json(new { success = true, message = "Medication created successfully." });
            }

            return Json(new { success = false, message = "Error occured while saving!." });

        }
        public IActionResult VitalSign(string healthid, string inhospid)
        {
            var patient = _context.KmuCharts.FirstOrDefault(u => u.ChrHealthId == healthid);
            var reservation = _context.InpatientReservations.FirstOrDefault(r => r.healthId == healthid && r.inhospId == inhospid);

            CommonService cService = new CommonService(_context);

            var result = _context.PhysicalSigns
                .Where(p => p.Inhospid == inhospid)
                .AsEnumerable()
                .GroupBy(p => p.ModifyTime)
                .OrderBy(g => g.Key)
                .Select(group => new vitalsignMV
                {
                    at = group.FirstOrDefault()?.ModifyTime,
                    RR = group.FirstOrDefault(x => x.PhyType == "RR")?.PhyValue,
                    Oxygen = group.FirstOrDefault(x => x.PhyType == "Oxygen")?.PhyValue,
                    HR = group.FirstOrDefault(x => x.PhyType == "HR")?.PhyValue,
                    Input = group.FirstOrDefault(x => x.PhyType == "INput")?.PhyValue,
                    Output = group.FirstOrDefault(x => x.PhyType == "OUTput")?.PhyValue,
                    Temperature = group.FirstOrDefault(x => x.PhyType == "Temperature")?.PhyValue,
                    RBS = group.FirstOrDefault(x => x.PhyType == "RBS")?.PhyValue,
                    Systolic = group.FirstOrDefault(x => x.PhyType == "Systolic BP")?.PhyValue,
                    Diastolic = group.FirstOrDefault(x => x.PhyType == "Diastolic BP")?.PhyValue,
                    PainScore = group.FirstOrDefault(x => x.PhyType == "PainScore")?.PhyValue,
                    Shift = cService.GetShiftbytime(group.FirstOrDefault()?.ModifyTime ?? DateTime.MinValue),
                    by = group.FirstOrDefault()?.ModifyUser
                })
                .ToList();

            ViewBag.Patient = patient;
            ViewBag.Reservation = reservation;
            ViewBag.VitalSign = result;
            return PartialView("_VitalSign");
        }

      
        public async Task<IActionResult> SaveVital(string inhospId, string[] types, string[] values, DateTime date)
        {
            try
            {
                var start = date;
                var end = date.AddMinutes(1);

                var existingVital = _context.PhysicalSigns
                    .Where(p => p.Inhospid == inhospId
                             && p.ModifyTime >= start
                             && p.ModifyTime < end)
                    .FirstOrDefault();

      

                if(existingVital != null)
                {
                    return Json(new { success = true, message = "this patiet has vitalsign of this time." });

                }



                var loginUser = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

                if (string.IsNullOrEmpty(inhospId) || types == null || values == null || types.Length != values.Length)
                {
                    return Json(new { success = false, message = "Invalid input data." });
                }

                for (int i = 0; i < types.Length; i++)
                {
                    string type = types[i];
                    string value = values[i];

                    // Count existing entries for this patient and type
                    int existingCount = _context.PhysicalSigns
                        .Where(p => p.Inhospid == inhospId && p.PhyType == type)
                        .Count();

                    var newVital = new PhysicalSign
                    {
                        Inhospid = inhospId,
                        PhyType = type,
                        PhyValue = value,
                        phy_version = existingCount + 1,
                        ModifyTime = date,
                        ModifyUser = loginUser?.EMPCODE
                    };

                    _context.PhysicalSigns.Add(newVital);
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Vital signs saved successfully with versioning." });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditVital(string inhospId, string[] types, string[] values, string date)
        {
            try
            {
                var loginUser = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

                if (string.IsNullOrEmpty(inhospId) || types == null || values == null || types.Length != values.Length)
                {
                    return Json(new { success = false, message = "Invalid input data." });
                }

                // Parse front-end date
                if (!DateTime.TryParse(date, out DateTime frontEndDate))
                    return Json(new { success = false, message = "Invalid date format." });

                for (int i = 0; i < types.Length; i++)
                {
                    string type = types[i];
                    string value = values[i];

                    // Find the existing vital record by type and approximate date (ignore seconds)
                    var start = frontEndDate;
                    var end = frontEndDate.AddMinutes(1);

                    var existingVital = _context.PhysicalSigns
                        .FirstOrDefault(p => p.Inhospid == inhospId
                            && p.PhyType == type
                            && p.ModifyTime.HasValue
                            && p.ModifyTime.Value >= start
                            && p.ModifyTime.Value < end);


                    if (existingVital != null)
                    {
                        existingVital.PhyValue = value;
                        existingVital.ModifyUser = loginUser?.EMPCODE;
                        existingVital.ModifyTime = frontEndDate; // Use front-end date
                        _context.PhysicalSigns.UpdateRange(existingVital);
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Vital signs updated successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        [HttpGet]
      
        public IActionResult Delete(string healthid, string inhospid, DateTime date)
        {
            try
            {
                // Remove the matching PhysicalSign record
                var start = date;
                var end = date.AddMinutes(1);

                var existingVital = _context.PhysicalSigns.Where(p =>
                    p.Inhospid == inhospid &&
                    p.ModifyTime.HasValue &&
                    p.ModifyTime.Value >= start &&
                    p.ModifyTime.Value < end
                );

                if (existingVital != null)
                {
                    _context.PhysicalSigns.RemoveRange(existingVital);
                    _context.SaveChanges();
                }

                // After delete, reload the vitals and return the partial view
                var patient = _context.KmuCharts.FirstOrDefault(u => u.ChrHealthId == healthid);
                var reservation = _context.InpatientReservations.FirstOrDefault(r => r.healthId == healthid && r.inhospId == inhospid);

                CommonService cService = new CommonService(_context);

                var result = _context.PhysicalSigns
                    .Where(p => p.Inhospid == inhospid)
                    .AsEnumerable()
                    .GroupBy(p => p.ModifyTime)
                    .OrderBy(g => g.Key)
                    .Select(group => new vitalsignMV
                    {
                        at = group.FirstOrDefault()?.ModifyTime,
                        RR = group.FirstOrDefault(x => x.PhyType == "RR")?.PhyValue,
                        Oxygen = group.FirstOrDefault(x => x.PhyType == "Oxygen")?.PhyValue,
                        HR = group.FirstOrDefault(x => x.PhyType == "HR")?.PhyValue,
                        Input = group.FirstOrDefault(x => x.PhyType == "INput")?.PhyValue,
                        Output = group.FirstOrDefault(x => x.PhyType == "OUTput")?.PhyValue,
                        Temperature = group.FirstOrDefault(x => x.PhyType == "Temperature")?.PhyValue,
                        RBS = group.FirstOrDefault(x => x.PhyType == "RBS")?.PhyValue,
                        Systolic = group.FirstOrDefault(x => x.PhyType == "Systolic BP")?.PhyValue,
                        Diastolic = group.FirstOrDefault(x => x.PhyType == "Diastolic BP")?.PhyValue,
                        PainScore = group.FirstOrDefault(x => x.PhyType == "PainScore")?.PhyValue,
                        Shift = cService.GetShiftbytime(group.FirstOrDefault()?.ModifyTime ?? DateTime.MinValue),
                        by = group.FirstOrDefault()?.ModifyUser
                    })
                    .ToList();

                ViewBag.Patient = patient;
                ViewBag.Reservation = reservation;
                ViewBag.VitalSign = result;

                // Return the partial view directly so it updates the vitals list
                return PartialView("_VitalSign");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }




        [HttpGet]
        public IActionResult GetAvailableBeds(string healthid, string inhospid, string currentBed)
        {
            try
            {
                var reservation = _context.InpatientReservations
                    .FirstOrDefault(r => r.healthId == healthid && r.inhospId == inhospid);
                Console.WriteLine("avail bed");

                if (reservation == null)
                {
                    return Json(new { success = false, message = "Reservation not found." });
                }

                var patient = _context.KmuCharts.FirstOrDefault(p => p.ChrHealthId == healthid);

                var ward = _context.Wards.FirstOrDefault(w => w.wardid == reservation.wardId);

                var currentBedInfo = _context.beds.FirstOrDefault(b => b.bedId == reservation.bedId);

                var availableBeds = _context.beds
                    .Where(b => b.wardId == reservation.wardId &&
                               b.status == "Available" &&
                               b.bedId != reservation.bedId)
                    .Select(b => new {
                        bedId = b.bedId,
                        bedName = b.bedName
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    patientName = patient?.ChrPatientFirstname + " " + patient?.ChrPatientLastname,
                    wardName = ward?.wardName,
                    currentBedId = currentBedInfo?.bedId,
                    availableBeds = availableBeds
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public IActionResult ChangeBedPartial(string healthid, string inhospid)
        {
            var patient = _context.KmuCharts.FirstOrDefault(p => p.ChrHealthId == healthid);
            if (patient == null)
                return NotFound();

            var reservation = _context.InpatientReservations
                .FirstOrDefault(r => r.healthId == healthid && r.inhospId == inhospid);
            if (reservation == null)
                return NotFound();

            var currentBed = _context.beds.FirstOrDefault(b => b.bedId == reservation.bedId);
            var currentWard = _context.Wards.FirstOrDefault(b => b.wardid == reservation.wardId);
            if (currentBed == null || currentWard == null)
                return NotFound();

            var ward = _context.Wards.Where(w=> w.wardid == "WI001" || w.wardid == "WI000").ToList();
            if (ward == null)
                return NotFound();

            var availableBeds = _context.beds
                .Where(b =>/* b.wardId == reservation.wardId &&*/
                           b.status == "Available" &&
                           b.bedId != reservation.bedId)
                .ToList();

            var viewModel = new BedChangeVM
            {
                Patient = patient,
                Reservation = reservation,       
                CurrentBed = currentBed,
                Ward = ward,
                AvailableBeds = availableBeds
            };

            return PartialView("_ChangeBed", viewModel);
        }
        [HttpPost]
        public IActionResult ChangeBed(string healthid, string inhospid, string currentBedId, string newBedId, string newWardId)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var loginUser = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
                  
                    var reservation = _context.InpatientReservations
                        .FirstOrDefault(r => r.healthId == healthid && r.inhospId == inhospid);

                    if (reservation == null)
                    {
                        return Json(new { success = false, message = "Reservation not found." });
                    }

           
                    var newWard = _context.Wards.FirstOrDefault(b => b.wardid == newWardId);

                    var currentBed = _context.beds.FirstOrDefault(b => b.bedId == currentBedId);
                    var newBed = _context.beds.FirstOrDefault(b => b.bedId == newBedId);

                    if (newWard == null)
                    {
                        return Json(new { success = false, message = "Ward not found." });
                    }
                    if (currentBed == null || newBed == null)
                    {
                        return Json(new { success = false, message = "Bed not found." });
                    }

                    reservation.wardId = newWardId;
                    reservation.bedId = newBedId;
                    _context.InpatientReservations.Update(reservation);

                    currentBed.status = "Available";
                    newBed.status = "Occupied";

                    _context.beds.Update(currentBed);
                    _context.beds.Update(newBed);

                    _context.SaveChanges();
                    transaction.Commit();

                    return Json(new
                    {
                        success = true,
                        message = $"Bed changed successfully from {currentBed.bedName} to {newBed.bedName}."
                    });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = $"Error changing bed: {ex.Message}" });
                }
            }
        }



    

        [HttpGet]
        public IActionResult WeeklyVitals(string healthid, string inhospid)
        {
            var patient = _context.KmuCharts.FirstOrDefault(u => u.ChrHealthId == healthid);


            ViewData["Patient"] = patient;
            ViewData["inhospid"] = inhospid;

            return View(patient);  
        }

        private class DailyIO
        {
            public double IntakeSum { get; set; }
            public double OutputSum { get; set; }
            public bool HasLossIn { get; set; }
            public bool HasLossOut { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> GetWeeklyVitalsJson(string inhospId)
        {
            if (string.IsNullOrEmpty(inhospId))
                return BadRequest("inhospId required");

            DateTime endDate = DateTime.Now;
            DateTime startDate = endDate.Date.AddDays(-6);

            // === 7 days × 6 slots/day ===
            var labels = new List<string>();
            for (int d = 0; d < 7; d++)
            {
                var day = startDate.AddDays(d).Date;
                for (int s = 0; s < 6; s++)
                    labels.Add(day.AddHours(s * 4).ToString("MM-dd\nHH:mm"));
            }

            int totalSlots = 42;
            double?[] hr = new double?[totalSlots];
            double?[] rr = new double?[totalSlots];
            double?[] temp = new double?[totalSlots];
            double?[] sbp = new double?[totalSlots];
            double?[] dbp = new double?[totalSlots];
            double?[] spo2 = new double?[totalSlots];
            double?[] pain = new double?[totalSlots];

            var records = await _context.PhysicalSigns
                .Where(p => p.Inhospid == inhospId && p.ModifyTime >= startDate && p.ModifyTime <= endDate.AddDays(1))
                .AsNoTracking()
                .ToListAsync();

            int GetSlotIndex(DateTime when)
            {
                if (when < startDate) when = startDate;
                int dayDiff = (when.Date - startDate.Date).Days;
                if (dayDiff < 0) dayDiff = 0;
                if (dayDiff > 6) dayDiff = 6;
                int slot = when.Hour / 4;
                if (slot > 5) slot = 5;
                return dayDiff * 6 + slot;
            }

            // === Fill vitals ===
            foreach (var r in records)
            {
                if (!DateTime.TryParse(r.ModifyTime.ToString(), out DateTime when))
                    continue;

                int idx = GetSlotIndex(when);
                string t = (r.PhyType ?? "").Trim().ToUpperInvariant();
                string val = (r.PhyValue ?? "").Trim();

                if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
                {
                    switch (t)
                    {
                        case "HR": hr[idx] = num; break;
                        case "RR": rr[idx] = num; break;
                        case "TEMPERATURE": temp[idx] = num; break;
                        case "SYSTOLIC BP": sbp[idx] = num; break;
                        case "DIASTOLIC BP": dbp[idx] = num; break;
                        case "OXYGEN": spo2[idx] = num; break;
                        case "PAINSCORE":
                        case "PAIN SCORE":
                            if (num >= 0 && num <= 10)
                                pain[idx] = num;
                            break;
                    }
                }
            }

            // === Daily I/O Handling ===
            var ioRecords = records
                .Where(r => (r.PhyType ?? "").ToUpperInvariant().Contains("IN") ||
                            (r.PhyType ?? "").ToUpperInvariant().Contains("OUT") ||
                            (r.PhyType ?? "").ToUpperInvariant().Contains("INPUT") ||
                            (r.PhyType ?? "").ToUpperInvariant().Contains("OUTPUT"))
                .ToList();

            var ioByDate = new Dictionary<DateTime, DailyIO>();
            for (int d = 0; d < 7; d++)
                ioByDate[startDate.AddDays(d).Date] = new DailyIO();

            foreach (var r in ioRecords)
            {
                if (!DateTime.TryParse(r.ModifyTime.ToString(), out DateTime when)) continue;
                var day = when.Date;
                if (!ioByDate.ContainsKey(day)) continue;

                var cur = ioByDate[day];
                string raw = (r.PhyValue ?? "").Trim().ToUpperInvariant();
                string type = (r.PhyType ?? "").ToUpperInvariant();

                // Extract numeric values
                var matches = System.Text.RegularExpressions.Regex.Matches(raw, @"-?\d+(\.\d+)?");

                if (type.Contains("INPUT"))
                {
                    if (raw.Contains("LOSS")) cur.HasLossIn = true;
                    foreach (System.Text.RegularExpressions.Match m in matches)
                        if (double.TryParse(m.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double n))
                            cur.IntakeSum += n;
                }
                else if (type.Contains("OUTPUT"))
                {
                    if (raw.Contains("LOSS")) cur.HasLossOut = true;
                    foreach (System.Text.RegularExpressions.Match m in matches)
                        if (double.TryParse(m.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double n))
                            cur.OutputSum += n;
                }

                ioByDate[day] = cur;
            }

            // === Compute daily totals ===
            var dailyTotals = ioByDate.Select(kv =>
            {
                var cur = kv.Value;

                string intakeLabel = Math.Round(cur.IntakeSum, 2).ToString("0.##", CultureInfo.InvariantCulture);
                if (cur.HasLossIn) intakeLabel += "+LOSS";

                string outputLabel = Math.Round(cur.OutputSum, 2).ToString("0.##", CultureInfo.InvariantCulture);
                if (cur.HasLossOut) outputLabel += "+LOSS";

                double balanceVal = cur.IntakeSum - cur.OutputSum;
                string balanceLabel = Math.Round(balanceVal, 2).ToString("0.##", CultureInfo.InvariantCulture);
                if (cur.HasLossIn || cur.HasLossOut) balanceLabel += "+LOSS";

                return new
                {
                    date = kv.Key.ToString("yyyy-MM-dd"),
                    intake = intakeLabel,
                    output = outputLabel,
                    balance = balanceLabel
                };
            }).OrderBy(x => x.date).ToList();

            return Json(new
            {
                labels,
                datasets = new { hr, rr, temp, sbp, dbp, spo2, pain },
                dailyTotals
            });
        }





    }

    
}
