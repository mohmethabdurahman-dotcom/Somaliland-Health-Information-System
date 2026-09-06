using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KMU.HisOrder.MVC.Models;
using System.Text.RegularExpressions;
using System.Drawing;

namespace KMU.HisOrder.MVC.Areas.Statistic.Controllers
{
    [Area("Statistic")]
    public class ReportController : Controller
    {
        private readonly KMUContext _context;

        public ReportController(KMUContext context)
        {
            _context = context;
        }

        // GET: Statistic/Report
        public async Task<IActionResult> Index(string? month)
        {
            TempData["month"] = "";
            if(month==""|| month == null)
            {
                TempData["month"] = 5;
              
                return View(month);
            }
            else
            {

           TempData["month"] = month;
           var department = _context.KmuDepartments.ToList();
           return View(month);
            }
        }

   

        // GET: Statistic/Report/Details/5
        public async Task<IActionResult> Details()
        {
          return View();

        }

        // GET: Statistic/Report/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Statistic/Report/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DptCode,DptName,DptCategory,DptDepth,DptStatus,DptRemark,DptDefaultAttr,ModifyUser,ModifyTime,DptParent")] KmuDepartment kmuDepartment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(kmuDepartment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(kmuDepartment);
        }

        // GET: Statistic/Report/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.KmuDepartments == null)
            {
                return NotFound();
            }

            var kmuDepartment = await _context.KmuDepartments.FindAsync(id);
            if (kmuDepartment == null)
            {
                return NotFound();
            }
            return View(kmuDepartment);
        }

        // POST: Statistic/Report/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("DptCode,DptName,DptCategory,DptDepth,DptStatus,DptRemark,DptDefaultAttr,ModifyUser,ModifyTime,DptParent")] KmuDepartment kmuDepartment)
        {
            if (id != kmuDepartment.DptCode)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(kmuDepartment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KmuDepartmentExists(kmuDepartment.DptCode))
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
            return View(kmuDepartment);
        }

        // GET: Statistic/Report/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.KmuDepartments == null)
            {
                return NotFound();
            }

            var kmuDepartment = await _context.KmuDepartments
                .FirstOrDefaultAsync(m => m.DptCode == id);
            if (kmuDepartment == null)
            {
                return NotFound();
            }

            return View(kmuDepartment);
        }

        // POST: Statistic/Report/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.KmuDepartments == null)
            {
                return Problem("Entity set 'KMUContext.KmuDepartments'  is null.");
            }
            var kmuDepartment = await _context.KmuDepartments.FindAsync(id);
            if (kmuDepartment != null)
            {
                _context.KmuDepartments.Remove(kmuDepartment);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool KmuDepartmentExists(string id)
        {
          return _context.KmuDepartments.Any(e => e.DptCode == id);
        }
    }
}
