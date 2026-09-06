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
    public class KmuDepartmentsController : Controller
    {
        private readonly KMUContext _context;

        public KmuDepartmentsController(KMUContext context)
        {
            _context = context;
        }

        // GET: Maintenance/KmuDepartments
        public IActionResult Index()
        {
            var department = _context.KmuDepartments.Where(d => d.DptParent != "").OrderBy(d=> d.DptCode).ToList();
            return View(department);
           // if (department == null)
           // {
           
           // }
          
           // var result = _context.KmuDepartments.Where(d => d.DptCategory.ToUpper().Contains(department.ToUpper()) & d.DptParent.ToUpper().Contains(DName.ToUpper()) & d.DptName.ToUpper().Contains(name.ToUpper()));
           //return View(result);
        }

        public IActionResult Close(string id)
        {
            var room = _context.ClinicSchedules.Where(r=> r.ScheDptCode == id && r.ScheOpenFlag == "Y").ToList();
            if (room.Any())
            {
                TempData["roomAlert"] = "go to clinicschedule and make off all the clinics has this room" +" "+ id;
                
            }
            else
            {
                var
                    result = _context.KmuDepartments.Find(id);
                if (result.DptStatus == "Y")
                {
                    result.DptStatus = "N";
                }
                else
                {
                    result.DptStatus = "Y";
                }
                var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
                result.ModifyUser = login.EMPCODE;
                _context.Update(result);
                _context.SaveChanges();
            }
     
             

            return RedirectToAction("Index");
        }

        public IActionResult Editt(string id, string name)
        {
            var found = _context.KmuDepartments.Find(id);
            found.DptName = name;
            _context.Update(found);
            _context.SaveChanges();

            return RedirectToAction("Index", new {id = found.DptCode});
        }


        public IActionResult Search(string department, string DName, string name)
        {

            var result = _context.KmuDepartments.Where(d => d.DptCategory.ToUpper().Contains(department.ToUpper()) & d.DptParent.ToUpper().Contains(DName.ToUpper()) & d.DptName.ToUpper().Contains(name.ToUpper()));
            return View(result);
        }

        // GET: Maintenance/KmuDepartments/Details/5
        public async Task<IActionResult> Details(string id)
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

        // GET: Maintenance/KmuDepartments/Create
        public IActionResult Create()
        {
            return View();
        }

        public IActionResult Department(string CDpName)
        {

            var deparment = _context.KmuDepartments.Where(d => d.DptParent == "" & d.DptCategory == CDpName).ToList();
            return Json(deparment);
        }

        // POST: Maintenance/KmuDepartments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KmuDepartment kmuDepartment)
        {
          

            var maxdptCode1 = _context.KmuDepartments.Where(d => d.DptParent == kmuDepartment.DptParent).Max(d => d.DptCode);
            int all = Convert.ToInt32(maxdptCode1);
            int last = all += 1;
            string lv = Convert.ToString(all);
            if (kmuDepartment.DptCategory == "OPD")
            {
                kmuDepartment.DptCode = "0" + lv;

            }
            if (kmuDepartment.DptCategory == "EMG")
            {
                kmuDepartment.DptCode = lv;

            }

            var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
            kmuDepartment.ModifyUser = login.EMPCODE;
            kmuDepartment.DptDepth = 2;
            kmuDepartment.ModifyTime = DateTime.Now;
            kmuDepartment.DptStatus = "Y";

            if (ModelState.IsValid)
            {
                _context.Add(kmuDepartment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(kmuDepartment);
        }

        // GET: Maintenance/KmuDepartments/Edit/5
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

        // POST: Maintenance/KmuDepartments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id,KmuDepartment kmuDepartment)
        {
            //var maxdptCode1 = _context.KmuDepartments.Where(d => d.DptParent == kmuDepartment.DptParent).Max(d => d.DptCode);
            //int all = Convert.ToInt32(maxdptCode1);
            //int last = all += 1;
            //string lv = Convert.ToString(all);
            //if (kmuDepartment.DptCategory == "OPD")
            //{
            //    kmuDepartment.DptCode = "0" + lv;

            //}
            //if (kmuDepartment.DptCategory == "EMG")
            //{
            //    kmuDepartment.DptCode = lv;

            //}
            var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
            kmuDepartment.ModifyUser = login.EMPCODE;
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
                return RedirectToAction("Index", "KmuDepartments");
            }
            return RedirectToAction("Index", "KmuDepartments");
        }

        // GET: Maintenance/KmuDepartments/Delete/5
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

        // POST: Maintenance/KmuDepartments/Delete/5
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
