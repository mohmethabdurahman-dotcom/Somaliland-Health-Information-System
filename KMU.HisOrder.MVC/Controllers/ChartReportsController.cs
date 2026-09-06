using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KMU.HisOrder.MVC.Models;
using System.Drawing;

namespace KMU.HisOrder.MVC.Controllers
{
    public class ChartReportsController : Controller
    {
        private readonly KMUContext _context;

        public ChartReportsController(KMUContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            TempData["allPatients"] = _context.Registrations.Count();
            var male = from reg in _context.Set<Registration>()
                       join chart in _context.Set<KmuChart>()
                       on reg.RegHealthId equals chart.ChrHealthId
                       where chart.ChrSex == "M"
                       select chart;

            var Female = from reg in _context.Set<Registration>()
                       join chart in _context.Set<KmuChart>()
                       on reg.RegHealthId equals chart.ChrHealthId
                       where chart.ChrSex == "F"
                       select chart;

            TempData["allMale"] = male.Count();
            TempData["allfemale"] = Female.Count();
            TempData["finished"] = _context.Registrations.Where(r => r.RegStatus == "*" || r.RegStatus == "T" || r.RegStatus == "O").Count();

            return View();
        
       }

        public cardreload CardData(int Year, int Month)
        {
            cardreload cardata = new cardreload();
            TempData["year"] = Year;
            TempData["Month"] = Month;
            var days = DateTime.DaysInMonth(Year, Month);

            var allpatients = _context.Registrations.Where(reg => reg.RegDate >= DateOnly.FromDateTime(new DateTime(Year, Month, 1)) && reg.RegDate <= DateOnly.FromDateTime(new DateTime(Year, Month, days))).Count();

            var male = from reg in _context.Set<Registration>()
                       join chart in _context.Set<KmuChart>()
                       on reg.RegHealthId equals chart.ChrHealthId
                       where chart.ChrSex == "M" && (reg.RegDate >= DateOnly.FromDateTime(new DateTime(Year, Month, 1)) && reg.RegDate <= DateOnly.FromDateTime(new DateTime(Year, Month, days)))
                       select chart;

            var Female = from reg in _context.Set<Registration>()
                         join chart in _context.Set<KmuChart>()
                         on reg.RegHealthId equals chart.ChrHealthId
                         where chart.ChrSex == "F" && (reg.RegDate >= DateOnly.FromDateTime(new DateTime(Year, Month, 1)) && reg.RegDate <= DateOnly.FromDateTime(new DateTime(Year, Month, days)))
                         select chart;
            
            var finished = _context.Registrations.Where(reg => (reg.RegStatus == "*" || reg.RegStatus == "T" || reg.RegStatus == "O") && (reg.RegDate >= DateOnly.FromDateTime(new DateTime(Year, Month, 1)) && reg.RegDate <= DateOnly.FromDateTime(new DateTime(Year, Month, days)))).Count();
            var M = male.Count();
            var F = Female.Count();
           

          

            cardata.Total = allpatients;
            cardata.maleCount = M;
            cardata.femaleCount = F;
            cardata.finished = finished;

            return cardata;
        }

        public IActionResult ChartData(int Year, int Month)
        {
         
            TempData["year"]  = Year;
            TempData["Month"] = Month;
            var days = DateTime.DaysInMonth(Year, Month);
      
 
            var departmentPatientCounts = from reg in _context.Set<Registration>()
                                          join dpt in _context.Set<KmuDepartment>() on reg.RegDepartment equals dpt.DptCode
                                          where (reg.RegStatus == "*" || reg.RegStatus == "T" || reg.RegStatus == "O") && (reg.RegDate >= DateOnly.FromDateTime(new DateTime(Year, Month, 1)) && reg.RegDate <= DateOnly.FromDateTime(new DateTime(Year, Month, days)))

                                          group reg by new { dpt.DptCode, dpt.DptName } into groupedRegistrations
                                          select new
                                          {
                                              DepartmentCode = groupedRegistrations.Key.DptCode,
                                              DepartmentName = groupedRegistrations.Key.DptName,
                                              TotalPatients = groupedRegistrations.Count()
                                          };
            return Json(departmentPatientCounts.ToList());
        }



    }
}
