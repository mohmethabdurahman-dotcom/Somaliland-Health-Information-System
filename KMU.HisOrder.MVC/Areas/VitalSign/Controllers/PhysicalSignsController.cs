using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KMU.HisOrder.MVC.Models;
using NuGet.Protocol;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using System.Net;
using KMU.HisOrder.MVC.Areas.VitalSign.Models;

namespace KMU.HisOrder.MVC.Areas.VitalSign.Controllers
{
    [Area("VitalSign")]
    public class PhysicalSignsController : Controller
    {
        private readonly KMUContext _context;

        public PhysicalSignsController(KMUContext context)
        {
            _context = context;
        }

        // GET: HisOrder/PhysicalSigns
        public async Task<IActionResult> Index()
        {
            return View(await _context.PhysicalSigns.ToListAsync());
        }

        // GET: HisOrder/PhysicalSigns/Create
        [HttpGet]
        public IActionResult Create(string id)
        {
            var regtriage = _context.Registrations.SingleOrDefault(r => r.Inhospid == id).RegTriage;
            if (regtriage == null || regtriage == "")
            {
                TempData["inhospId"] = id;

            }
            return View();
        }

        public async Task<IActionResult> getPatient(string InhospID)
        {
            var dpt = _context.KmuDepartments.Where(e => e.DptParent == "1600").ToList();
            List<string> dptCodes = new List<string>();
            foreach (var a in dpt)
            {
                dptCodes.Add(a.DptCode);
            }
            Console.WriteLine(InhospID);
            string correctInhospid = InhospID.PadLeft(17, '0');

            var dateNow = DateOnly.FromDateTime(DateTime.Now);

            var inhospital = await _context.Registrations.FirstOrDefaultAsync(e => e.Inhospid == correctInhospid && e.RegDate == dateNow && dptCodes.Contains(e.RegDepartment) && e.RegStatus != "C");
            if (inhospital != null)
            {
                var getPatient = _context.KmuCharts.FirstOrDefault(e => e.ChrHealthId == inhospital.RegHealthId);

                if (getPatient == null)
                {
                    var Nodata = "This patient does'nt exits.";
                    return Json(Nodata);
                }

                var patient = new KmuChart();

                patient.ChrAddress = getPatient.ChrAddress;
                patient.ChrMobilePhone = getPatient.ChrMobilePhone;
                patient.ChrPatientFirstname = getPatient.ChrPatientFirstname;
                patient.ChrPatientMidname = getPatient.ChrPatientMidname;
                patient.ChrPatientLastname = getPatient.ChrPatientLastname;
                patient.ChrHealthId = getPatient.ChrHealthId;
                patient.ChrEmgContact = getPatient.ChrBirthDate.ToString();
                patient.ChrSex = getPatient.ChrSex;

                if (patient.ChrSex == "M")
                {
                    patient.ChrSex = "Male";
                }
                else if (patient.ChrSex == "F")
                {
                    patient.ChrSex = "Female";
                }

                return Json(patient);
            }
            var validmessage = "This patient does'nt have appointment for today.";
            return Json(validmessage);
        }


        // POST: HisOrder/PhysicalSigns/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public IActionResult SaveTriage(string id, string triageLevel, string triageScore, PhysicalSign physicalSign)
        {

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(triageLevel) || string.IsNullOrEmpty(triageScore))
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { responseText = "Invalid input data." });
            }

            string correctInhospid = id.PadLeft(17, '0');

            try
            {
                // Check if the patient exists
                var registeredPatient = _context.Registrations.FirstOrDefault(p => p.Inhospid == correctInhospid);
                if (registeredPatient == null)
                {
                    Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return Json(new { responseText = "Patient not found." });
                }

                // Check if the physical signs already exist
                var existingVitals = _context.PhysicalSigns.Any(e => e.Inhospid == correctInhospid);
                if (existingVitals)
                {
                    Response.StatusCode = (int)HttpStatusCode.Conflict;
                    return Json(new { responseText = "This patient already has recorded vital signs." });
                }

                // Update the patient's triage information
                registeredPatient.RegTriage = triageLevel;
                registeredPatient.score = triageScore;
                registeredPatient.physign_time = DateTime.Now;

                _context.Registrations.Update(registeredPatient);
                _context.SaveChanges();

                return Json(new { success = true, triageScore });
            }
            catch (Exception ex)
            {
                // Log the exception (add a logging mechanism here)
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Json(new { responseText = "An error occurred while saving triage data.", error = ex.Message });
            }
        }


        public async Task<IActionResult> savephs(string inhospID, string[] type, string[] value, PhysicalSign physicalSign)
        {
            var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

            string correctInhospid = inhospID.PadLeft(17, '0');

            // if exit before
            var exitVital = _context.PhysicalSigns.Where(e => e.Inhospid == correctInhospid).ToList();

            if (exitVital.Any())
            {
                var errorMessage = "This patient have a vital sign.";
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Json(new { responseText = errorMessage });
            }
            else
            {
                foreach (var item in type)
                {
                    physicalSign.PhyId = "";
                    physicalSign.PhyType = item;
                    physicalSign.Inhospid = correctInhospid;
                    physicalSign.PhyValue = "3";
                    physicalSign.ModifyTime = DateTime.Today;
                    physicalSign.ModifyUser = login.EMPCODE;
                    await _context.PhysicalSigns.AddRangeAsync(physicalSign);

                    await _context.SaveChangesAsync();
                }

                var findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == correctInhospid && p.PhyType == "Mobility");
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[0];
                    _context.PhysicalSigns.Update(item);
                }
                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == correctInhospid && p.PhyType == "AVPU");
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[1];
                    _context.PhysicalSigns.Update(item);
                }

                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == correctInhospid && p.PhyType == "Trauma");
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[2];
                    _context.PhysicalSigns.Update(item);
                }

                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == correctInhospid && p.PhyType == "RR");
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[3];
                    _context.PhysicalSigns.Update(item);
                }

                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == correctInhospid && p.PhyType == "Oxygen");
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[4];
                    _context.PhysicalSigns.Update(item);
                }

                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == correctInhospid && p.PhyType == "Physical HR");
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[5];
                    _context.PhysicalSigns.Update(item);
                }

                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == correctInhospid && p.PhyType == "Systolic BP");
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[6];
                    _context.PhysicalSigns.Update(item);
                }
                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == correctInhospid && p.PhyType == "Diastolic BP");
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[7];
                    _context.PhysicalSigns.Update(item);
                }
                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == correctInhospid && p.PhyType == "Temperature");
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[8];
                    _context.PhysicalSigns.Update(item);
                }
                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == correctInhospid && p.PhyType == "Physical RBS (Glucose)");
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[9];
                    _context.PhysicalSigns.Update(item);
                }

                await _context.SaveChangesAsync();
                return Json(type);
            }

        }

        public IActionResult NurseFunction(string reserveType)
        {
            //ENR - NR
            ViewData["reserveType"] = reserveType;

            if (reserveType == "NR")
            {
                ViewData["Title"] = "NCD VitalSign";
            }
            else if (reserveType == "ENR")
            {
                ViewData["Title"] = "ER VitalSign";
            }

            return View();
        }

        public async Task<IActionResult> getData(string PatientID)
        {
            var dateNow = DateOnly.FromDateTime(DateTime.Now);
            var dpt = _context.KmuDepartments.Where(e => e.DptParent == "6000" || e.DptParent == "3000").ToList();
            List<string> dptCodes = new List<string>();
            foreach (var a in dpt)
            {
                dptCodes.Add(a.DptCode);
            }

            var getPatient = await _context.Registrations.FirstOrDefaultAsync(e => e.RegHealthId == PatientID && e.RegDate == dateNow && dptCodes.Contains(e.RegDepartment));

            if (getPatient != null)
            {
                DateTime dat = DateTime.Today;

                var getVital = _context.PhysicalSigns.Where(e => e.Inhospid == getPatient.Inhospid).ToList();
                return Json(getVital);

            }

            TempData["inhosId"] = getPatient.Inhospid;

            return Json(null);
        }

        public async Task<IActionResult> NR(string PatientID)
        {
            var dpt = _context.KmuDepartments.Where(e => e.DptParent == "6000" || e.DptParent == "3000").ToList();
            List<string> dptCodes = new List<string>();
            foreach (var a in dpt)
            {
                dptCodes.Add(a.DptCode);
            }

            var getPatient = _context.KmuCharts.FirstOrDefault(e => e.ChrHealthId == PatientID);

            if (getPatient == null)
            {
                var errorMessage = "This patient does'nt exits.";
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Json(new { responseText = errorMessage });
            }

            var dateNow = DateOnly.FromDateTime(DateTime.Now);

            var inhospital = await _context.Registrations.FirstOrDefaultAsync(e => e.RegHealthId == getPatient.ChrHealthId && e.RegDate == dateNow && dptCodes.Contains(e.RegDepartment) && e.RegStatus != "C");


            var patient = new KmuChart();

            patient.ChrAddress = getPatient.ChrAddress;
            patient.ChrMobilePhone = getPatient.ChrMobilePhone;
            patient.ChrPatientFirstname = getPatient.ChrPatientFirstname;
            patient.ChrPatientMidname = getPatient.ChrPatientMidname;
            patient.ChrPatientLastname = getPatient.ChrPatientLastname;
            patient.ChrHealthId = getPatient.ChrHealthId;
            patient.ChrEmgContact = getPatient.ChrBirthDate.ToString();
            patient.ChrSex = getPatient.ChrSex;

            if (patient.ChrSex == "M")
            {
                patient.ChrSex = "Male";
            }
            else if (patient.ChrSex == "F")
            {
                patient.ChrSex = "Female";
            }

            if (inhospital != null)
            {
                return Json(patient);
            }
            var validmessage = "This patient does'nt have appointment for today.";
            Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return Json(new { responseText = validmessage });
        }

        [HttpPost]
        public async Task<IActionResult> vitalSign(string healthId, string[] type, string[] value, PhysicalSign physicalSign)
        {
            var date = DateOnly.FromDateTime(DateTime.Now);
            var dpt = _context.KmuDepartments.Where(e => e.DptParent == "6000" || e.DptParent == "3000").ToList();
            List<string> dptCodes = new List<string>();
            foreach (var a in dpt)
            {
                dptCodes.Add(a.DptCode);
            }

            var inhospId = await _context.Registrations.SingleOrDefaultAsync(e => e.RegHealthId == healthId && e.RegDate == date && dptCodes.Contains(e.RegDepartment) && e.RegStatus != "C");
            if (inhospId.Inhospid == null)
            {
                Console.WriteLine("InhospitalID is null...");
            }

            var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
            var exitVital = _context.PhysicalSigns.Where(e => e.Inhospid == inhospId.Inhospid && e.ModifyTime == DateTime.Today).ToList();


            if (exitVital.Any())
            {
                var error = "This patient have a vital sign.";
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Json(new { responseText = error });
            }
            else
            {
                foreach (var item in type)
                {
                    physicalSign.PhyId = "";
                    physicalSign.PhyType = item;
                    physicalSign.Inhospid = inhospId.Inhospid;
                    physicalSign.PhyValue = "";
                    physicalSign.ModifyTime = DateTime.Today;
                    physicalSign.ModifyUser = login.EMPCODE;
                    //physicalSign.PhyDept = "Ncd_Physical";
                    await _context.PhysicalSigns.AddRangeAsync(physicalSign);

                    await _context.SaveChangesAsync();
                }
                DateTime dat = DateTime.Today;
                var findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Height" && p.ModifyTime == dat);
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[0];
                    _context.PhysicalSigns.Update(item);
                }

                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Weight" && p.ModifyTime == dat);
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[1];
                    _context.PhysicalSigns.Update(item);
                }

                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Temp" && p.ModifyTime == dat);
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[2];
                    _context.PhysicalSigns.Update(item);
                }

                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == Convert.ToString(type[3]) && p.ModifyTime == dat);
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[3];
                    _context.PhysicalSigns.Update(item);
                }

                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "SpO2" && p.ModifyTime == dat);
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[4];
                    _context.PhysicalSigns.Update(item);
                }

                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Systolic (Sys)" && p.ModifyTime == dat);
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[5];
                    _context.PhysicalSigns.Update(item);
                }

                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Diastolic (Dias)" && p.ModifyTime == dat);
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[6];
                    _context.PhysicalSigns.Update(item);
                }

                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "HR" && p.ModifyTime == dat);
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[7];
                    _context.PhysicalSigns.Update(item);
                }
                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "RBS" && p.ModifyTime == dat);
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[8];
                    _context.PhysicalSigns.Update(item);
                }
                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "FBS" && p.ModifyTime == dat);
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[9];
                    _context.PhysicalSigns.Update(item);
                }

                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Pulse" && p.ModifyTime == dat);
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[10];
                    _context.PhysicalSigns.Update(item);
                }

                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "MUAC" && p.ModifyTime == dat);
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[11];
                    _context.PhysicalSigns.Update(item);
                }

                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "HBA1C" && p.ModifyTime == dat);
                foreach (var item in findinhospid)
                {
                    item.PhyValue = value[12];
                    _context.PhysicalSigns.Update(item);
                }

                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Headache" && p.ModifyTime == dat);
                foreach (var item in findinhospid)
                {
                    item.PhyValue = "Headache";
                    _context.PhysicalSigns.Update(item);
                }

                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Tiredness" && p.ModifyTime == dat);
                foreach (var item in findinhospid)
                {
                    item.PhyValue = "Tiredness";
                    _context.PhysicalSigns.Update(item);
                }

                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Nausea" && p.ModifyTime == dat);
                foreach (var item in findinhospid)
                {
                    item.PhyValue = "Nausea";
                    _context.PhysicalSigns.Update(item);
                }

                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Fatigue" && p.ModifyTime == dat);
                foreach (var item in findinhospid)
                {
                    item.PhyValue = "Fatigue";
                    _context.PhysicalSigns.Update(item);
                }

                findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Confusion" && p.ModifyTime == dat);
                foreach (var item in findinhospid)
                {
                    item.PhyValue = "Confusion";
                    _context.PhysicalSigns.Update(item);
                }

                await _context.SaveChangesAsync();
                return Json(type);
            }

            return null;
        }

        [HttpPost]
        public async Task<IActionResult> update(string healthId, string[] type, string[] value, PhysicalSign physicalSign)
        {
            var date = DateOnly.FromDateTime(DateTime.Now);
            var dpt = _context.KmuDepartments.Where(e => e.DptParent == "6000" || e.DptParent == "3000").ToList();
            List<string> dptCodes = new List<string>();
            foreach (var a in dpt)
            {
                dptCodes.Add(a.DptCode);
            }

            var inhospId = await _context.Registrations.SingleOrDefaultAsync(e => e.RegHealthId == healthId && e.RegDate == date && dptCodes.Contains(e.RegDepartment) && e.RegStatus != "C");
            if (inhospId.Inhospid == null)
            {
                Console.WriteLine("InhospitalID is null...");
            }

            var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
            var exitVital = _context.PhysicalSigns.Where(e => e.Inhospid == inhospId.Inhospid && e.ModifyTime == DateTime.Today).ToList();

            foreach (var data in exitVital)
            {
                _context.Remove(data);
                _context.SaveChanges();
            }


            foreach (var item in type)
            {
                physicalSign.PhyId = "";
                physicalSign.PhyType = item;
                physicalSign.Inhospid = inhospId.Inhospid;
                physicalSign.PhyValue = "";
                physicalSign.ModifyTime = DateTime.Today;
                physicalSign.ModifyUser = login.EMPCODE;
                //physicalSign.PhyDept = "Ncd_Physical";
                await _context.PhysicalSigns.AddRangeAsync(physicalSign);

                await _context.SaveChangesAsync();
            }
            DateTime dat = DateTime.Today;
            var findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Height" && p.ModifyTime == dat);
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[0];
                _context.PhysicalSigns.Update(item);
            }

            findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Weight" && p.ModifyTime == dat);
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[1];
                _context.PhysicalSigns.Update(item);
            }

            findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Temp" && p.ModifyTime == dat);
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[2];
                _context.PhysicalSigns.Update(item);
            }

            findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == Convert.ToString(type[3]) && p.ModifyTime == dat);
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[3];
                _context.PhysicalSigns.Update(item);
            }

            findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "SpO2" && p.ModifyTime == dat);
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[4];
                _context.PhysicalSigns.Update(item);
            }

            findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Systolic (Sys)" && p.ModifyTime == dat);
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[5];
                _context.PhysicalSigns.Update(item);
            }

            findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Diastolic (Dias)" && p.ModifyTime == dat);
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[6];
                _context.PhysicalSigns.Update(item);
            }
            findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "HR" && p.ModifyTime == dat);
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[7];
                _context.PhysicalSigns.Update(item);
            }
            findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "RBS" && p.ModifyTime == dat);
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[8];
                _context.PhysicalSigns.Update(item);
            }
            findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "FBS" && p.ModifyTime == dat);
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[9];
                _context.PhysicalSigns.Update(item);
            }

            findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Pulse" && p.ModifyTime == dat);
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[10];
                _context.PhysicalSigns.Update(item);
            }

            findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "MUAC" && p.ModifyTime == dat);
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[11];
                _context.PhysicalSigns.Update(item);
            }

            findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "HBA1C" && p.ModifyTime == dat);
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[12];
                _context.PhysicalSigns.Update(item);
            }

            await _context.SaveChangesAsync();
            return Json(type);

        }

        public async Task<IActionResult> HomeVitalSign(home_physicalsign data, string healthId, string type)
        {
            var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");


            var date = DateOnly.FromDateTime(DateTime.Now);
            var dpt = _context.KmuDepartments.Where(e => e.DptParent == "6000" || e.DptParent == "3000").ToList();
            List<string> dptCodes = new List<string>();
            foreach (var a in dpt)
            {
                dptCodes.Add(a.DptCode);
            }

            var inhospId = await _context.Registrations.SingleOrDefaultAsync(e => e.RegHealthId == healthId && e.RegDate == date && dptCodes.Contains(e.RegDepartment));
            if (inhospId.Inhospid == null)
            {
                return View();
            }

            data.inhospid = inhospId.Inhospid;
            if (type == "Hyper")
            {
                data.category = "Hypertension";
            }
            else if (type == "Diab")
            {
                data.category = "diabetes";
            }
            data.modify_time = DateTime.Today;
            data.modify_user = login.EMPCODE;
            await _context.home_physicalsign.AddAsync(data);
            _context.SaveChanges();
            return Json(data);
        }

    }
}
