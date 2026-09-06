using Microsoft.AspNetCore.Mvc;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Areas.Maintenance.ViewModels;
using KMU.HisOrder.MVC.Extesion;
using KMU.HisOrder.MVC.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using KMU.HisOrder.MVC.Areas.HisOrder.ViewModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualBasic;
using System.Security.Cryptography.Xml;
using System;
using Microsoft.AspNetCore.Authorization;
using System.Data;

namespace KMU.HisOrder.MVC.Areas.Statistic.Controllers
{
    [Area("Statistic")]
    [Authorize(Roles = "Statistic")]
    public class DHIS2Controller : Controller
    {

        private readonly KMUContext _context;

        public DHIS2Controller(KMUContext context)
        {
            _context = context;
        }
        
        public IActionResult Index()
        {
            #region Login Session Check
            var checklogin = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
            if (checklogin == null || string.IsNullOrWhiteSpace(checklogin.EMPCODE))
            {
                return RedirectToAction("Login/Index");
            }

            #endregion


            return View();
        }

        public List<DhisTwoCounting> DHisTable(int year, int mon)
        {
            List<DhisTwo> hisplans = GetOpdHisplan(year, mon).ToList();
            List<DhisTwo> dhisData = GetDhisTwos().OrderBy(c=>c.ShowSeq).ToList();
            List<DhisTwoCounting> Result = new List<DhisTwoCounting>();
            foreach (DhisTwo items in dhisData)
            {
                DhisTwoCounting CountDhisRow = new DhisTwoCounting();
                int Mt5 = 0,Lt5 = 0,Total = 0;

                Mt5 = hisplans.Where(c => c.Dhis2Code == items.Dhis2Code && c.age >= 5).ToList().Count();
                Lt5 = hisplans.Where(c => c.Dhis2Code == items.Dhis2Code && c.age < 5).ToList().Count();
                Total = hisplans.Where(c => c.Dhis2Code == items.Dhis2Code).ToList().Count();
                
                CountDhisRow.overFive = Mt5;
                CountDhisRow.underFive = Lt5;
                CountDhisRow.total = Total;
                CountDhisRow.diseases = items.Diseases;
                CountDhisRow.dhis2Code = items.Dhis2Code;
                CountDhisRow.showSeq = items.ShowSeq;


                Result.Add(CountDhisRow);
            }
            return Result;
        }
        public List<DhisTwo> GetOpdHisplan(int year,int mon )
        {
            //string aaa = year + "/" + mon + "/01";

            DateTime firstDayOfMonth = new DateTime(year, mon, 1);
            DateTime lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);


            var ptData = from h in _context.Set<Hisorderplan>()
                      join i in _context.Set<KmuIcd>()
                      on h.PlanCode equals i.IcdCode
                      join chart in _context.Set<KmuChart>()
                      on h.HealthId equals chart.ChrHealthId
                      join r in _context.Set<Registration>()
                      on h.Inhospid equals r.Inhospid
                      join p in _context.Set<KmuDepartment>()
                      on r.RegDepartment equals p.DptCode
                      where h.HplanType == "ICD"
                      where h.Status == '2'
                      where h.DcDate == null
                      where p.DptCategory == "OPD"
                      where r.RegDate >= DateOnly.FromDateTime(firstDayOfMonth) && r.RegDate <= DateOnly.FromDateTime(lastDayOfMonth)
                      where h.CreateDate >= firstDayOfMonth && h.CreateDate <= lastDayOfMonth
                         select new DhisTwo
                      {
                          age = chart.ChrBirthDate == null ? -1 : DateOnly.FromDateTime(firstDayOfMonth).Year - chart.ChrBirthDate.Value.Year,
                          IcdCode = i.IcdCode,
                          Dhis2Code = i.Dhis2Code,
                          ChrPatientId = chart.ChrHealthId,
                          //Inhospid = h.Inhospid
                      };

            ptData = ptData.Distinct();

            if (ptData.Any())
            {
                return ptData.ToList();
            }
            else
            {
                return new List<DhisTwo>();
            }
        }

        public List<DhisTwo> GetDhisTwos()
        {
            var dhisData = from d in _context.Set<Dhis2Disease>()
                           orderby d.ShowSeq
                         select new DhisTwo
                         {

                             Dhis2Code = d.Dhis2Code,
                             Diseases = d.Diseases,
                             ShowSeq = d.ShowSeq
                         };

            dhisData = dhisData.Distinct();

            if (dhisData.Any())
            {
                return dhisData.ToList();
            }
            else
            {
                return new List<DhisTwo>();
            }
        }
    }
}
