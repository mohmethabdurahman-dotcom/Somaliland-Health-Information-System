using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KMU.HisOrder.MVC.Models;
using NuGet.Protocol;

namespace KMU.HisOrder.MVC.Areas.HisOrder.Controllers
{
    [Area("HisOrder")]
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
        public IActionResult Create(string? id)
        {
            var regtriage = _context.Registrations.SingleOrDefault(r => r.Inhospid == id).RegTriage;
            if (regtriage == null || regtriage == "")
            {
                TempData["inhospId"] = id;

            }
            return View();
        }

        // POST: HisOrder/PhysicalSigns/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public IActionResult savetriage(string id, string triageLeve, string triageScore, PhysicalSign physicalSign)
        {

            var regPatient = _context.Registrations.Where(p => p.Inhospid == id);
            foreach (var item in regPatient)
            {
                item.RegTriage = triageLeve;
                item.score = triageScore;
                item.physign_time = DateTime.Now;
                _context.Registrations.Update(item);
            }
            _context.SaveChanges();
            return Json(triageScore);

        }
        public async Task<IActionResult> savephs(string inhospId, string[] type, string[] value, PhysicalSign physicalSign)
        {
            foreach (var item in type)
            {
                physicalSign.PhyId = "";
                physicalSign.PhyType = item;
                physicalSign.Inhospid = inhospId;
                physicalSign.PhyValue = "3";
                physicalSign.ModifyTime = DateTime.Now;
                physicalSign.ModifyUser = "AOA77";
                await _context.PhysicalSigns.AddRangeAsync(physicalSign);

                await _context.SaveChangesAsync();
            }

            var findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId && p.PhyType == "Mobility");
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[0];
                _context.PhysicalSigns.Update(item);
            }
            findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId && p.PhyType == "AVPU");
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[1];
                _context.PhysicalSigns.Update(item);
            }

            findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId && p.PhyType == "Trauma");
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[2];
                _context.PhysicalSigns.Update(item);
            }

            findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId && p.PhyType == "RR");
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[3];
                _context.PhysicalSigns.Update(item);
            }

            findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId && p.PhyType == "Oxygen");
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[4];
                _context.PhysicalSigns.Update(item);
            }

            findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId && p.PhyType == "HR");
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[5];
                _context.PhysicalSigns.Update(item);
            }

            findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId && p.PhyType == "Systolic BP");
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[6];
                _context.PhysicalSigns.Update(item);
            }
            findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId && p.PhyType == "Diastolic BP");
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[7];
                _context.PhysicalSigns.Update(item);
            }
            findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId && p.PhyType == "Temperature");
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[8];
                _context.PhysicalSigns.Update(item);
            }
            findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId && p.PhyType == "RBS (Glucose)");
            foreach (var item in findinhospid)
            {
                item.PhyValue = value[9];
                _context.PhysicalSigns.Update(item);
            }

            await _context.SaveChangesAsync();
            return Json(type);
        }


    }
}
