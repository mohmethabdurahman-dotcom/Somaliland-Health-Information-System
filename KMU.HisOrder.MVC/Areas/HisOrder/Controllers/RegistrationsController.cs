using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KMU.HisOrder.MVC.Models;
using KMU.HisOrder.MVC.Areas.InPatient.ViewModels;
using KMU.HisOrder.MVC.Areas.VitalSign.Models;
using KMU.HisOrder.MVC.Areas.HisOrder.ViewModels;
using KMU.HisOrder.MVC.Extesion;

namespace KMU.HisOrder.MVC.Areas.HisOrder.Controllers
{
    [Area("HisOrder")]
    public class RegistrationsController : Controller
    {
        private readonly KMUContext _context;

        public RegistrationsController(KMUContext context)
        {
            _context = context;
        }

        // GET: HisOrder/Registrations
        public async Task<IActionResult> Index(string? id)
        {
            HttpContext.Session.SetString("PatientId", (string)id);
            if (id == null)
            {
                id = (string)HttpContext.Session.GetString("PatientId");

            }
            TempData["PatientId"] = (string)HttpContext.Session.GetString("PatientId");
            var registration = _context.Registrations.Where(r => r.RegHealthId == id && r.RegTriage != null && r.RegStatus != "C").ToList();
            return View(registration);
        }

        // GET: HisOrder/Registrations/Details/5
        public async Task<IActionResult> Details(string id)
        {
            var physicalSign = _context.PhysicalSigns.Where(p => p.Inhospid == id).ToList();
            if (physicalSign == null)
            {
                return NotFound();
            }

            return View(physicalSign);
        }

        public IActionResult WardVital(string? id)
        {

            //inhospId must be passed


            CommonService cService = new CommonService(_context);
            var result = _context.PhysicalSigns
    .Where(p => p.Inhospid == id)
    .AsEnumerable()
    .GroupBy(p => p.phy_version)
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
        Shift = cService.GetShiftbytime(group.FirstOrDefault()?.ModifyTime ?? DateTime.MinValue),
        by = group.FirstOrDefault()?.ModifyUser,
        version = group.FirstOrDefault()?.phy_version
    })
    .ToList();

            return View(result);
        }

        public IActionResult WardVitalDetail(string id, int version)
        {
            var result = _context.PhysicalSigns
                .Where(p => p.Inhospid == id && p.phy_version == version)
                .ToList();
            return View(result);
        }


    }
}
