using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KMU.HisOrder.MVC.Models;
using KMU.HisOrder.MVC.Areas.Maintenance.ViewModels;
using KMU.HisOrder.MVC.Areas.HisOrder.ViewModels;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;

namespace KMU.HisOrder.MVC.Areas.Maintenance.Controllers
{
    [Area("Maintenance")]
    [Authorize(Roles = "Maintain_KmuNonMedicines")]//登入後可依據設定的 專案名稱 project_id 作為判斷依據
    public class KmuNonMedicinesController : Controller
    {
        private readonly KMUContext _context;

        public KmuNonMedicinesController(KMUContext context)
        {
            _context = context;
        }

        // GET: Maintenance/KmuNonMedicines
        public async Task<IActionResult> Index()
        {
            //return View(await _context.KmuNonMedicines.ToListAsync());
            return View(await _context.KmuNonMedicines.OrderBy(x => x.ItemName).ToListAsync());
        }

        // GET: Maintenance/KmuNonMedicines/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.KmuNonMedicines == null)
            {
                return NotFound();
            }

            var kmuNonMedicine = await _context.KmuNonMedicines
                .FirstOrDefaultAsync(m => m.ItemId == id);
            if (kmuNonMedicine == null)
            {
                return NotFound();
            }

            return View(kmuNonMedicine);
        }

        [HttpPost]
        public async Task<IActionResult> Search(string ItemName)
        {
            ItemName = ItemName.Trim();

            return View(await _context.KmuNonMedicines.Where(x =>
                    (ItemName == null) ? x.ItemId == x.ItemId :
                    (x.ItemName.ToLower().Contains(ItemName.ToLower()))
                ).OrderBy(x => x.ItemName).ToListAsync());
        }


        // GET: Maintenance/KmuNonMedicines/Create
        public IActionResult Create()
        {
            ViewData["GroupCodeData"] = _context.KmuCoderefs.Where(c => c.RefCodetype == "group_code").OrderBy(c => c.RefShowseq);

            return View();
        }

        // POST: Maintenance/KmuNonMedicines/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ItemId,ItemName,ItemType,ItemSpec,StartDate,EndDate,Remark,ShowSeq,GroupCode,Status")] KmuNonMedicine kmuNonMedicine)
        {
            if (ModelState.IsValid)
            {
                //KmuNonMedicineExists
                if (!KmuNonMedicineExists(kmuNonMedicine.ItemId.Trim()))
                {
                    var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
                    kmuNonMedicine.ItemId = kmuNonMedicine.ItemId.Trim();
                    kmuNonMedicine.ModifyUser = login.EMPCODE;
                    kmuNonMedicine.CreateUser = login.EMPCODE;

                    _context.Add(kmuNonMedicine);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ViewData["Msg"] = "ItemId Repeat";
                    return View(kmuNonMedicine);
                }

            }
            return View(kmuNonMedicine);
        }

        // GET: Maintenance/KmuNonMedicines/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.KmuNonMedicines == null)
            {
                return NotFound();
            }

            var kmuNonMedicine = await _context.KmuNonMedicines.FindAsync(id);
            if (kmuNonMedicine == null)
            {
                return NotFound();
            }

            ViewData["GroupCodeData"] = _context.KmuCoderefs.Where(c => c.RefCodetype == "group_code").OrderBy(c => c.RefShowseq);

            return View(kmuNonMedicine);
        }

        // POST: Maintenance/KmuNonMedicines/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("ItemId,ItemName,ItemType,ItemSpec,StartDate,EndDate,Status,CreateUser,CreateDate,Remark,ShowSeq,GroupCode")] KmuNonMedicine kmuNonMedicine)
        {
            if (id != kmuNonMedicine.ItemId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
                    kmuNonMedicine.ModifyUser = login.EMPCODE;
                    kmuNonMedicine.ModifyDate = DateTime.Now;

                    _context.Update(kmuNonMedicine);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KmuNonMedicineExists(kmuNonMedicine.ItemId))
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
            return View(kmuNonMedicine);
        }

        // GET: Maintenance/KmuNonMedicines/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.KmuNonMedicines == null)
            {
                return NotFound();
            }

            var kmuNonMedicine = await _context.KmuNonMedicines
                .FirstOrDefaultAsync(m => m.ItemId == id);
            if (kmuNonMedicine == null)
            {
                return NotFound();
            }

            return View(kmuNonMedicine);
        }

        // POST: Maintenance/KmuNonMedicines/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.KmuNonMedicines == null)
            {
                return Problem("Entity set 'KMUContext.KmuNonMedicines' is null.");
            }
            var kmuNonMedicine = await _context.KmuNonMedicines.FindAsync(id);
            if (kmuNonMedicine != null)
            {
                _context.KmuNonMedicines.Remove(kmuNonMedicine);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool KmuNonMedicineExists(string id)
        {
            return _context.KmuNonMedicines.Any(e => e.ItemId == id);
        }

        public string UpdateStatus(string inItemId)
        {
            string strR = "";
            string strStatus = "";

            if (inItemId == null || _context.KmuNonMedicines == null)
            {
                strR = "修改出現錯誤!!";

            }
            else
            {
                if (!KmuNonMedicineExists(inItemId))
                {
                    strR = "修改出現錯誤!!";
                }
                else
                {
                    List<KmuNonMedicine> NonMedList = new List<KmuNonMedicine>();
                    var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

                    NonMedList = _context.KmuNonMedicines.Where(e => e.ItemId == inItemId).ToList();

                    if (NonMedList.Any())
                    {
                        try
                        {
                            KmuNonMedicine NonMed = NonMedList.First();
                            NonMed = NonMedList.First();

                            strStatus = NonMed.Status.ToString();

                            if (strStatus == "1")
                            {
                                NonMed.Status = '2';
                            }
                            else
                            {
                                NonMed.Status = '1';
                            }

                            NonMed.ModifyDate = DateTime.Now;
                            NonMed.ModifyUser = login.EMPCODE;
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
