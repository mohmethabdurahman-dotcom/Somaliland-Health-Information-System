using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KMU.HisOrder.MVC.Models;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using KMU.HisOrder.MVC.Extesion;

namespace KMU.HisOrder.MVC.Areas.Maintenance.Controllers
{
    [Area("Maintenance")]
    [Authorize(Roles = "Maintain_KmuDepartments")]
    public class BedController : Controller
    {
        private readonly KMUContext _context;

        public BedController(KMUContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var wardsWithDepartments = await (from bed in _context.beds
                                              join ward in _context.Wards
                                              on bed.wardId equals ward.wardid
                                              select new
                                              {
                                                  bed.bedId,
                                                  bed.wardId,
                                                  bed.bedName,
                                                  ward.wardName, 
                                                  bed.status,
                                                  bed.createBy,
                                                  bed.createAt
                                              }).ToListAsync();

            var wards = wardsWithDepartments.Select(w => new BedviewModel
            {
                bedId = w.bedId,
                bedName = w.bedName,
                wardId = w.wardId,  
                status = w.status,
                wardname = w.wardName,  

                createBy = w.createBy,
                createAt = w.createAt
            }).ToList();

            return View(wards);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

      
        [HttpPost]

        public async Task< ActionResult> create(Bed bed)
        {
            {
                if (string.IsNullOrWhiteSpace(bed.bedName))
                {
                    ModelState.AddModelError("bedName", "Bed Name cannot be empty.");
                }

                if (string.IsNullOrWhiteSpace(bed.wardId.ToString()))
                {
                    ModelState.AddModelError("wardId", "Ward must be selected.");
                }

                var existingWard = await _context.beds
                          .FirstOrDefaultAsync(w => w.bedName == bed.bedName && w.wardId == bed.wardId);

                if (existingWard != null)
                {

                    ModelState.AddModelError("bedName", "A Bed with this name already exists in the selected Ward.");
                }
                if (ModelState.IsValid)
                {
                    var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
                    bed.bedId = "";
                    bed.createBy =  login.EMPCODE;
                    bed.createAt = DateTime.Now;
                    bed.status = "Available";
                    _context.beds.Add(bed);

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                return View(bed);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var ward = await _context.beds.FindAsync(id);

            if (ward == null)
            {
                return NotFound();
            }



            return View(ward);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Bed ward)
        {
          
            if (id != ward.bedId)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                var existingWard = await _context.beds
                    .FirstOrDefaultAsync(w => w.bedName == ward.bedName && w.wardId == ward.wardId && w.wardId != id);

                if (existingWard != null)
                {
                    ModelState.AddModelError("bedName", "A Bed with this name already exists in the selected Ward.");
                }

                if (string.IsNullOrWhiteSpace(ward.bedName.ToString()    ))
                {
                    ModelState.AddModelError("bedName", "Bed Name cannot be empty.");
                }

                if (string.IsNullOrWhiteSpace(ward.wardId.ToString()))
                {
                    ModelState.AddModelError("wardId.", "ward. must be selected.");
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

                        ward.createBy =  login.EMPCODE;
                        ward.createAt = DateTime.Now;
                        ward.status = "Available";
                        _context.Update(ward);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!WardExists(ward.wardId))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }

                    return RedirectToAction(nameof(Index)); 
                }
            }


            return RedirectToAction(nameof(Index));
        }

        private bool WardExists(string id)
        {
            return _context.Wards.Any(e => e.wardid == id);
        }

    }
}
