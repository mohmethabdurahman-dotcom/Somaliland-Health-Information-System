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
using KMU.HisOrder.MVC.Areas.HisOrder.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Data;

namespace KMU.HisOrder.MVC.Areas.Maintenance.Controllers
{
    [Area("Maintenance")]
    [Authorize(Roles = "Maintain_KmuMedfrequency")]//登入後可依據設定的 專案名稱 project_id 作為判斷依據
    public class KmuMedfrequencyController : Controller
    {
        private readonly KMUContext _context;

        public KmuMedfrequencyController(KMUContext context)
        {
            _context = context;
        }

        // GET: Maintenance/KmuMedfrequency
        public async Task<IActionResult> Index()
        {
            //SelectList selectList = new SelectList(this.GetCustomers(), "MedId", "GenericName");
            //ViewBag.SelectList = selectList;

            return View(await _context.KmuMedfrequencies.OrderBy(x => x.FrqSeqNo).ThenBy(c => c.FrqCode).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Search()
        {

            //var temp = _context.KmuMedicines.Where(x =>
            //(
            //    (MedTypeQuery == null) ? x.MedType == x.MedType :
            //        ((MedTypeQuery == "-") ? x.MedType == x.MedType : x.MedType == MedTypeQuery)
            //)
            //&& (
            //        (MedName == null) ? x.GenericName == x.GenericName :
            //        (x.GenericName.ToLower().Contains(MedName.ToLower()) || x.BrandName.ToLower().Contains(MedName.ToLower()))
            //    )
            //).OrderBy(x => x.GenericName).ToListAsync();

            //return View(await temp);

            return View(await _context.KmuMedfrequencies.OrderBy(x => x.FrqSeqNo).ThenBy(x => x.FrqCode).ToListAsync());
        }




        // GET: Maintenance/KmuMedfrequency/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.KmuMedfrequencies == null)
            {
                return NotFound();
            }

            var kmuMedfrequencies = await _context.KmuMedfrequencies
                .FirstOrDefaultAsync(m => m.FrqCode == id);
            if (kmuMedfrequencies == null)
            {
                return NotFound();
            }

            return View(kmuMedfrequencies);
        }

        // GET: Maintenance/KmuMedfrequency/Create
        public IActionResult Create()
        {
            return View();
        }


        // POST: Maintenance/KmuMedicines/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FrqCode,FreqDesc,FrqForDays,FrqForTimes,FrqOneDayTimes,EnableStatus,FrqSeqNo")] KmuMedfrequency kmuMedfrequency)
        {
            if (ModelState.IsValid)
            {
                if (!KmuMedfrequenciesExists(kmuMedfrequency.FrqCode))
                {
                    var intMed_id = _context.KmuMedicines.Select(x => int.Parse(x.MedId)).ToList().Max() + 1;

                    var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
                    kmuMedfrequency.CreateUser = login.EMPCODE;
                    kmuMedfrequency.ModifyUser = login.EMPCODE;
                    //kmuMedfrequency.MedId = intMed_id.ToString();

                    _context.Add(kmuMedfrequency);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ViewData["Msg"] = "ItemId Repeat";
                    return View(kmuMedfrequency);
                }

            }
            return View(kmuMedfrequency);
        }

        // GET: Maintenance/KmuMedfrequency/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.KmuMedfrequencies == null)
            {
                return NotFound();
            }

            var KmuMedfrequencies = await _context.KmuMedfrequencies.FindAsync(id);
            if (KmuMedfrequencies == null)
            {
                return NotFound();
            }
            return View(KmuMedfrequencies);
        }

        //public async Task<IActionResult> Edit(string id, [Bind("MedId,ProductName,Nomenclature,Sepc,Unit,StartDate,EndDate,Status,CreateUser,CreateDate,ModifyUser,ModifyDate")] KmuMedicine kmuMedicine)
        // POST: Maintenance/KmuMedfrequency/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("FrqCode,FreqDesc,FrqForDays,FrqForTimes,FrqOneDayTimes,EnableStatus,FrqSeqNo,CreateUser")] KmuMedfrequency kmuMedfrequency)
        {
            if (id != kmuMedfrequency.FrqCode)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

                    kmuMedfrequency.ModifyUser = login.EMPCODE;

                    _context.Update(kmuMedfrequency);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KmuMedfrequenciesExists(kmuMedfrequency.FrqCode))
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
            return View(kmuMedfrequency);
        }

        // GET: Maintenance/KmuMedfrequency/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.KmuMedfrequencies == null)
            {
                return NotFound();
            }

            var kmuMedfrequencies = await _context.KmuMedfrequencies
                .FirstOrDefaultAsync(m => m.FrqCode == id);
            if (kmuMedfrequencies == null)
            {
                return NotFound();
            }

            return View(kmuMedfrequencies);
        }

        // POST: Maintenance/KmuMedfrequency/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.KmuMedicines == null)
            {
                return Problem("Entity set 'KMUContext.KmuMedfrequency'  is null.");
            }
            var kmuMedfrequencies = await _context.KmuMedfrequencies.FindAsync(id);
            if (kmuMedfrequencies != null)
            {
                _context.KmuMedfrequencies.Remove(kmuMedfrequencies);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool KmuMedfrequenciesExists(string id)
        {
            return _context.KmuMedfrequencies.Any(e => e.FrqCode == id);
        }

        public string UpdateStatus(string inFrqCode)
        {
            string strR = "";
            string strStatus = "";

            if (inFrqCode == null || _context.KmuMedfrequencies == null)
            {
                strR = "修改出現錯誤!!";

            }
            else
            {
                if (!KmuMedfrequenciesExists(inFrqCode))
                {
                    strR = "修改出現錯誤!!";
                }
                else
                {
                    List<KmuMedfrequency> MedFList = new List<KmuMedfrequency>();
                    var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

                    MedFList = _context.KmuMedfrequencies.Where(e => e.FrqCode == inFrqCode).ToList();

                    if (MedFList.Any())
                    {
                        try
                        {
                            KmuMedfrequency medf = MedFList.First();
                            medf = MedFList.First();

                            strStatus = medf.EnableStatus.ToString();

                            if (strStatus == "1")
                            {
                                medf.EnableStatus = '2';
                            }
                            else
                            {
                                medf.EnableStatus = '1';
                            }

                            medf.ModifyUser = login.EMPCODE;
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
