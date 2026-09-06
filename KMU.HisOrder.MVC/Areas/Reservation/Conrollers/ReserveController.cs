using AutoMapper.Configuration.Annotations;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Data;
using System.Linq;
using System.Reflection.Emit;
using static KMU.HisOrder.MVC.Models.EnumClass;

namespace KMU.HisOrder.MVC.Areas.Reservation.Conrollers
{
    [Area("Reservation")]
    [Authorize(Roles = "Reservation,OPDReserve,EMGReserve")]
    public class ReserveController : Controller
    {
        private readonly KMUContext _context;

        public ReserveController(KMUContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Close(string id)
        {

            var result = await _context.Registrations.SingleOrDefaultAsync(r => r.Inhospid == id);
            if (result.RegStatus == "N")
            {
                result.RegStatus = "C";
                result.ModifyTime = DateTime.Now;
            }

            _context.Update(result);
            _context.SaveChanges();

            return RedirectToAction("List");
        }

        public async Task<IActionResult> ErClose(string id)
        {

            var result = await _context.Registrations.SingleOrDefaultAsync(r => r.Inhospid == id);
            if (result.RegStatus == "N")
            {
                result.RegStatus = "C";
                result.ModifyTime = DateTime.Now;
            }

            _context.Update(result);
            _context.SaveChanges();

            return RedirectToAction("ErList");
        }

        public IActionResult Details(string? id, string? cancelList, string? hideList)
        {

            TempData["DepName"] = _context.KmuDepartments.SingleOrDefault(e => e.DptCode == id).DptName;
            TempData["DeptId"] = _context.KmuDepartments.SingleOrDefault(e => e.DptCode == id).DptCode;

            if (id == null || id == "")
            {
                return NotFound();
            }

            DateOnly d = DateOnly.FromDateTime(DateTime.Today);

            if (cancelList != null)
            {
                var kan = _context.Registrations.Where(R => R.RegDepartment == id && R.RegDate == d).ToList();
                return View(kan);
            }

            var result = _context.Registrations.Where(R => R.RegDepartment == id && R.RegDate == d && R.RegStatus != "C").ToList();


            return View(result);
        }

        public IActionResult List(string? firstdate, string? seconddate, string? status, List<string>? ENT)
        {


            //Duration Search Limit.

            var between = (Convert.ToDateTime(seconddate) - Convert.ToDateTime(firstdate)).Days;

            // Duration With Month.
            //var totalDays = (Convert.ToDateTime(between) - Convert.ToDateTime(firstdate));

            if (between < 32)
            {
                if (firstdate != null && seconddate != null && status != null)
                {
                    if (status == "All")
                    {
                        var kan = _context.Registrations.Where(R => R.ModifyTime >= Convert.ToDateTime(firstdate) && R.ModifyTime <= Convert.ToDateTime(seconddate) && ENT.Contains(R.RegDepartment)).ToList();
                        return View(kan);
                    }
                    else
                    {
                        var kan = _context.Registrations.Where(R => R.ModifyTime >= Convert.ToDateTime(firstdate) && R.RegStatus == status && R.ModifyTime <= Convert.ToDateTime(seconddate) && ENT.Contains(R.RegDepartment)).ToList();
                        return View(kan);
                    }
                }
            }
            else
            {
                // Eror Message Display When the duration is Over 1 months.

                TempData["Eror"] = "You can not filter data more than 1 month. You selected " + between + " Days";
                var list = _context.Registrations.Where(e => e.Inhospid == null);
                return View(list);
            }

            var result = _context.Registrations.Where(R => R.ModifyTime >= DateTime.Today && R.RegStatus == "N" && Convert.ToInt32(R.RegDepartment) != Convert.ToInt32("1600") && Convert.ToInt32(R.RegDepartment) != Convert.ToInt32("1604") && Convert.ToInt32(R.RegDepartment) != Convert.ToInt32("1601") && Convert.ToInt32(R.RegDepartment) != Convert.ToInt32("1602") && Convert.ToInt32(R.RegDepartment) != Convert.ToInt32("1603")).ToList();

            return View(result);

        }

        public IActionResult ErList(string? firstdate, string? seconddate, string? status, List<string>? ENT)
        {


            //Duration Search Limit.

            var between = (Convert.ToDateTime(seconddate) - Convert.ToDateTime(firstdate)).Days;

            // Duration With Month.
            //var totalDays = (Convert.ToDateTime(between) - Convert.ToDateTime(firstdate));

            if (between < 32)
            {
                if (firstdate != null && seconddate != null && status != null)
                {
                    if (status == "All")
                    {
                        var kan = _context.Registrations.Where(R => R.ModifyTime >= Convert.ToDateTime(firstdate) && R.ModifyTime <= Convert.ToDateTime(seconddate) && ENT.Contains(R.RegDepartment)).ToList();
                        return View(kan);
                    }
                    else
                    {
                        var kan = _context.Registrations.Where(R => R.ModifyTime >= Convert.ToDateTime(firstdate) && R.RegStatus == status && R.ModifyTime <= Convert.ToDateTime(seconddate) && ENT.Contains(R.RegDepartment)).ToList();
                        return View(kan);
                    }
                }
            }
            else
            {
                // Eror Message Display When the duration is Over 1 months.

                TempData["Eror"] = "You can not filter data more than 1 month. You selected " + between + " Days";
                var list = _context.Registrations.Where(e => e.Inhospid == null);
                return View("ErList", list);
            }

            var result = _context.Registrations.Where(R => R.ModifyTime >= DateTime.Today && R.RegStatus == "N" && Convert.ToInt32(R.RegDepartment) >= Convert.ToInt32("1600") && Convert.ToInt32(R.RegDepartment) <= Convert.ToInt32("1604")).ToList();

            return View(result);

        }

        public IActionResult Reserve(string reserveType)
        {
            List<EnumAnonymous> anonymousList = new List<EnumAnonymous>();

            foreach (EnumClass.EnumAnonymous anonymous in Enum.GetValues(typeof(EnumAnonymous)))
            {
                if (EnumClass.EnumAnonymous.Normal != anonymous)
                {
                    anonymousList.Add(anonymous);
                }
            }

            ViewData["reserveType"] = reserveType;
            ViewData["anonymousList"] = anonymousList;

            if (reserveType == "OPD")
            {
                ViewData["Title"] = "Appointment";
            }
            else if (reserveType == "EMG")
            {
                ViewData["Title"] = "Emergency";
            }

            return View();
        }
    }
}