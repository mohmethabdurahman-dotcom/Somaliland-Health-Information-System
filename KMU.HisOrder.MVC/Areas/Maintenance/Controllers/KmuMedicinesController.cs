using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KMU.HisOrder.MVC.Models;
using System.ComponentModel.DataAnnotations;
using KMU.HisOrder.MVC.Areas.Maintenance.ViewModels;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;

namespace KMU.HisOrder.MVC.Areas.Maintenance.Controllers
{
    [Area("Maintenance")]
    [Authorize(Roles = "Maintain_KmuMedicines")]//登入後可依據設定的 專案名稱 project_id 作為判斷依據
    public class KmuMedicinesController : Controller
    {
        private readonly KMUContext _context;

        public KmuMedicinesController(KMUContext context)
        {
            _context = context;
        }

        // GET: Maintenance/KmuMedicines
        public async Task<IActionResult> Index()
        {
            //SelectList selectList = new SelectList(this.GetCustomers(), "MedId", "GenericName");
            //ViewBag.SelectList = selectList;

            return View(await _context.KmuMedicines.OrderBy(x => x.GenericName).ThenBy(x => x.BrandName).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Search(string MedTypeQuery, string MedName)
        {
            if (string.IsNullOrWhiteSpace(MedName))
            {
                var temp = _context.KmuMedicines.OrderBy(x => x.GenericName).ThenBy(x => x.BrandName).ToListAsync();

                return View(await temp);
            }
            else
            {
                MedName = MedName.Trim().ToLower();

                var temp = _context.KmuMedicines.Where(x =>
                (
                    (MedTypeQuery == null) ? x.MedType == x.MedType :
                        ((MedTypeQuery == "-") ? x.MedType == x.MedType : x.MedType == MedTypeQuery)
                )
                && (
                        (MedName == null) ? x.GenericName == x.GenericName :
                        (x.GenericName.ToLower().Contains(MedName) || x.BrandName.ToLower().Contains(MedName))
                    )
                ).OrderBy(x => x.GenericName).ThenBy(x => x.BrandName).ToListAsync();

                return View(await temp);
            }
        }


        //private IEnumerable<KmuMedicine> GetCustomers()
        //{

        //    var query = _context.KmuMedicines.OrderBy(x => x.GenericName);
        //    return query.ToList();

        //}

        // GET: Maintenance/KmuMedicines
        //public async Task<IActionResult> Index(string MedTypeQuery)
        //{
        //    return View(await _context.KmuMedicines.Where(x => x.MedType == MedTypeQuery).OrderBy(x => x.GenericName).ToListAsync());
        //    //return View(await _context.KmuMedicines.ToListAsync());
        //@Html.DropDownList("KmuMedicines", (SelectList) ViewBag.SelectList, "請選擇客戶", new { id = "Customers" })
        //}

        // GET: Maintenance/KmuMedicines/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.KmuMedicines == null)
            {
                return NotFound();
            }

            var kmuMedicine = await _context.KmuMedicines
                .FirstOrDefaultAsync(m => m.MedId == id);
            if (kmuMedicine == null)
            {
                return NotFound();
            }

            return View(kmuMedicine);
        }

        // GET: Maintenance/KmuMedicines/Create
        public IActionResult Create()
        {
            return View();
        }

        //public async Task<IActionResult> Create([Bind("MedId,ProductName,Nomenclature,Sepc,Unit,StartDate,EndDate,Status,CreateUser,CreateDate,ModifyUser,ModifyDate")] KmuMedicine kmuMedicine)
        // POST: Maintenance/KmuMedicines/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MedId,MedType,GenericName,BrandName,UnitSpec,PackSpec,DefaultFreq,RefDuration,Remarks,StartDate,EndDate,CreateUser,ModifyUser,Status")] KmuMedicine kmuMedicine)
        {
            if (ModelState.IsValid)
            {
                var intMed_id = _context.KmuMedicines.Select(x => int.Parse(x.MedId)).ToList().Max() + 1;

                var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
                kmuMedicine.ModifyUser = login.EMPCODE;
                kmuMedicine.CreateUser = login.EMPCODE;
                kmuMedicine.MedId = intMed_id.ToString();

                _context.Add(kmuMedicine);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(kmuMedicine);
        }

        // GET: Maintenance/KmuMedicines/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.KmuMedicines == null)
            {
                return NotFound();
            }

            var kmuMedicine = await _context.KmuMedicines.FindAsync(id);
            if (kmuMedicine == null)
            {
                return NotFound();
            }
            return View(kmuMedicine);
        }

        //public async Task<IActionResult> Edit(string id, [Bind("MedId,ProductName,Nomenclature,Sepc,Unit,StartDate,EndDate,Status,CreateUser,CreateDate,ModifyUser,ModifyDate")] KmuMedicine kmuMedicine)
        // POST: Maintenance/KmuMedicines/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MedId,MedType,GenericName,BrandName,UnitSpec,PackSpec,DefaultFreq,RefDuration,Remarks,StartDate,EndDate,CreateUser,ModifyUser,Status")] KmuMedicine kmuMedicine)
        {
            if (id != kmuMedicine.MedId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

                    kmuMedicine.ModifyUser = login.EMPCODE;

                    _context.Update(kmuMedicine);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KmuMedicineExists(kmuMedicine.MedId))
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
            return View(kmuMedicine);
        }

        // GET: Maintenance/KmuMedicines/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.KmuMedicines == null)
            {
                return NotFound();
            }

            var kmuMedicine = await _context.KmuMedicines
                .FirstOrDefaultAsync(m => m.MedId == id);
            if (kmuMedicine == null)
            {
                return NotFound();
            }

            return View(kmuMedicine);
        }

        // POST: Maintenance/KmuMedicines/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.KmuMedicines == null)
            {
                return Problem("Entity set 'KMUContext.KmuMedicines'  is null.");
            }
            var kmuMedicine = await _context.KmuMedicines.FindAsync(id);
            if (kmuMedicine != null)
            {
                _context.KmuMedicines.Remove(kmuMedicine);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool KmuMedicineExists(string id)
        {
            return _context.KmuMedicines.Any(e => e.MedId == id);
        }

        public string UpdateStatus(string inMedId)
        {
            string strR = "";
            string strStatus = "";

            if (inMedId == null || _context.KmuMedicines == null)
            {
                strR = "修改出現錯誤!!";

            }
            else
            {
                if (!KmuMedicineExists(inMedId))
                {
                    strR = "修改出現錯誤!!";
                }
                else
                {
                    List<KmuMedicine> MedList = new List<KmuMedicine>();
                    var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

                    MedList = _context.KmuMedicines.Where(e => e.MedId == inMedId).ToList();

                    if (MedList.Any())
                    {
                        try
                        {
                            KmuMedicine med = MedList.First();
                            med = MedList.First();

                            strStatus = med.Status.ToString();

                            if (strStatus == "1")
                            {
                                med.Status = '2';
                            }
                            else
                            {
                                med.Status = '1';
                            }

                            med.ModifyDate = DateTime.Now;
                            med.ModifyUser = login.EMPCODE;
                            _context.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            strR = ex.ToString();
                        }
                    }
                    else
                    {
                        strR = "修改出現錯誤!!";
                    }

                }
            }
            return strR;
        }

    }
}
