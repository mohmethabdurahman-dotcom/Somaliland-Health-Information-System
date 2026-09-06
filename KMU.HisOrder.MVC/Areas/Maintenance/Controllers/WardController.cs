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
    public class WardController : Controller
    {
        private readonly KMUContext _context;

        public WardController(KMUContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var wardsWithDepartments = await (from ward in _context.Wards
                                             join department in _context.KmuDepartments
                                             on ward.department equals department.DptCode
                                             select new
                                             {
                                                 ward.wardid,
                                                 ward.wardName,
                                                 ward.capacity,
                                                 department.DptName, 
                                                 ward.createBy,
                                                 ward.createAt
                                             }).ToListAsync();

            var wards = wardsWithDepartments.Select(w => new Ward
            {
                wardid = w.wardid,
                capacity = w.capacity,
                wardName = w.wardName,
                department = w.DptName, 
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

        public async Task< ActionResult> create(Ward ward)
        {
            {
                if (string.IsNullOrWhiteSpace(ward.wardName))
                {
                    ModelState.AddModelError("wardName", "Ward Name cannot be empty.");
                }

                if (string.IsNullOrWhiteSpace(ward.department))
                {
                    ModelState.AddModelError("department", "Department must be selected.");
                }

                var existingWard = await _context.Wards
                          .FirstOrDefaultAsync(w => w.wardName == ward.wardName && w.department == ward.department);

                if (existingWard != null)
                {
                    ModelState.AddModelError("wardName", "A ward with this name already exists in the selected department.");
                }
                if (ModelState.IsValid)
                {
                    var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
                    ward.wardid = "";
                    ward.createBy =  login.EMPCODE;
                    ward.createAt = DateTime.Now;
                    _context.Wards.Add(ward);

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                return View(ward);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var ward = await _context.Wards.FindAsync(id);

            if (ward == null)
            {
                return NotFound();
            }

            var departments = await _context.KmuDepartments.Where(d => d.DptParent == "")
                .OrderBy(d => d.DptName)
                .Select(d => new SelectListItem
                {
                    Value = d.DptCode,
                    Text = d.DptName
                })
                .ToListAsync();

            ViewData["Departments"] = departments;

            return View(ward);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Ward ward)
        {
            if (id != ward.wardid)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingWard = await _context.Wards
                    .FirstOrDefaultAsync(w => w.wardName == ward.wardName && w.department == ward.department && w.wardid != id);

                if (existingWard != null)
                {
                    ModelState.AddModelError("wardName", "A ward with this name already exists in the selected department.");
                }

                if (string.IsNullOrWhiteSpace(ward.wardName))
                {
                    ModelState.AddModelError("wardName", "Ward Name cannot be empty.");
                }

                if (string.IsNullOrWhiteSpace(ward.department))
                {
                    ModelState.AddModelError("department", "Department must be selected.");
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

                        ward.modifyBy =  login.EMPCODE;
                        ward.createAt = DateTime.Now;
                        _context.Update(ward);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!WardExists(ward.wardid))
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

            var departments = await _context.KmuDepartments.Where(d => d.DptParent == "")
                .OrderBy(d => d.DptName)
                .Select(d => new SelectListItem
                {
                    Value = d.DptCode,
                    Text = d.DptName
                })
                .ToListAsync();

            ViewData["Departments"] = departments;
            return View(ward);
        }

        private bool WardExists(string id)
        {
            return _context.Wards.Any(e => e.wardid == id);
        }

    }
}
