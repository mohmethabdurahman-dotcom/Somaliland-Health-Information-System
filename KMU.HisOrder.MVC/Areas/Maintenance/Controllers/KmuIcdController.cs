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
using KMU.HisOrder.MVC.Areas.HisOrder.ViewModels;
using Newtonsoft.Json.Linq;
using System.Collections;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Areas.Maintenance.Models;
using Newtonsoft.Json;
using KMU.HisOrder.MVC.Extesion;
using Microsoft.AspNetCore.Authorization;
using System.Data;

namespace KMU.HisOrder.MVC.Areas.Maintenance.Controllers
{
    [Area("Maintenance")]
    [Authorize(Roles = "Maintain_KmuIcd")]//登入後可依據設定的 專案名稱 project_id 作為判斷依據
    public class KmuIcdController : Controller
    {
        private readonly KMUContext _context;

        public KmuIcdController(KMUContext context)
        {
            _context = context;
        }






        // GET: Maintenance/KmuIcd
        public async Task<IActionResult> Index()
        {
            //List<KmuIcd> KmuIcdsTemp = new List<KmuIcd>();
            //var list = new List<string>() { "A", "B", "C" };

            //foreach (var iii in list)
            //{
            //    if (!string.IsNullOrEmpty(iii))
            //    {
            //        KmuIcdsTemp.AddRange(
            //            _context.KmuIcds.Where(x => x.IcdCode.ToLower().StartsWith(iii.ToLower()))
            //            );
            //    }
            //}
            //var listD = KmuIcdsTemp.Select(x => x.IcdCode.ToLower());
            //return View(await _context.KmuIcds.Where(x => listD.Contains(x.IcdCode.ToLower())).OrderBy(x => x.IcdEnglishName).ToListAsync());


            return View(await _context.KmuIcds.Where(x => x.IcdCode.ToLower().StartsWith("A".ToLower())).OrderBy(x => x.IcdCode).ThenBy(x => x.IcdEnglishName).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Search(string IcdCode, string EnglishName)
        {

            #region 資料處理
            var splitArry = IcdCode.Split(',').ToList();
            EnglishName = EnglishName.Trim();

            List<KmuIcd> KmuIcdsTemp = new List<KmuIcd>();
            foreach (var iii in splitArry)
            {
                if (!string.IsNullOrEmpty(iii))
                {
                    KmuIcdsTemp.AddRange(
                        _context.KmuIcds.Where(x => x.IcdCode.ToLower().StartsWith(iii.ToLower()))
                        );
                }
            }
            var listD = KmuIcdsTemp.Select(x => x.IcdCode.ToLower());
            #endregion

            return View(await _context.KmuIcds.Where(x =>
                (IcdCode == null) ? x.IcdCode.ToLower().StartsWith("A") :
                (listD.Contains(x.IcdCode.ToLower()))
                &&
                (
                    (EnglishName == null) ? x.IcdEnglishName == x.IcdEnglishName :
                    x.IcdEnglishName.ToLower().Contains(EnglishName.ToLower())
                )
            ).OrderBy(x => x.IcdCode).ThenBy(x => x.IcdEnglishName).ToListAsync());

            //return View(await _context.KmuIcds.Where(x =>
            //        (IcdCode == null) ? x.IcdCode.ToLower().StartsWith("A") :
            //        (x.IcdCode.ToLower().StartsWith(IcdCode.ToLower()))
            //        &&
            //        (
            //            (EnglishName == null) ? x.IcdEnglishName == x.IcdEnglishName :
            //            x.IcdEnglishName.ToLower().Contains(EnglishName.ToLower())
            //        )
            //    ).OrderBy(x => x.IcdEnglishName).ToListAsync());

        }


        // GET: Maintenance/KmuIcd/Details/5
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

        // GET: Maintenance/KmuIcd/Create
        public IActionResult Create()
        {
            ViewData["Dhis2Data"] = _context.Dhis2Diseases.OrderBy(c => c.ShowSeq);

            return View();
        }

        // POST: Maintenance/KmuIcd/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IcdCode,IcdEnglishName,Status,IcdType,ShowMode,Versioncode,Dhis2Code,ParentCode,Status")] KmuIcd kmuIcd)
        {
            if (ModelState.IsValid)
            {
                if (!KmuIcdExists(kmuIcd.IcdCode))
                {
                    var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
                    kmuIcd.ModifyUser = login.EMPCODE;
                    kmuIcd.ModifyDate = DateTime.Now;
                    kmuIcd.IcdCodeUndot = kmuIcd.IcdCode.Replace(".", "");

                    _context.Add(kmuIcd);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ViewData["Msg"] = "IcdCode Repeat";

                    return View(kmuIcd);
                }

            }
            return View(kmuIcd);
        }

        // GET: Maintenance/KmuIcd/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.KmuIcds == null)
            {
                return NotFound();
            }

            var KmuIcds = await _context.KmuIcds.FindAsync(id);
            if (KmuIcds == null)
            {
                return NotFound();
            }

            ViewData["Dhis2Data"] = _context.Dhis2Diseases.OrderBy(c => c.ShowSeq);

            return View(KmuIcds);
        }

        // POST: Maintenance/KmuIcd/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("IcdCode,IcdEnglishName,Status,IcdType,ShowMode,Versioncode,Dhis2Code,ParentCode")] KmuIcd KmuIcd)
        {
            if (id != KmuIcd.IcdCode)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
                    KmuIcd.ModifyUser = login.EMPCODE;
                    KmuIcd.ModifyDate = DateTime.Now;
                    KmuIcd.IcdCodeUndot = KmuIcd.IcdCode.Replace(".", "");

                    _context.Update(KmuIcd);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KmuIcdExists(KmuIcd.IcdCode))
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
            return View(KmuIcd);
        }

        // GET: Maintenance/KmuIcd/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.KmuIcds == null)
            {
                return NotFound();
            }

            var KmuIcd = await _context.KmuIcds
                .FirstOrDefaultAsync(m => m.IcdCode == id);
            if (KmuIcd == null)
            {
                return NotFound();
            }

            return View(KmuIcd);
        }

        // POST: Maintenance/KmuIcd/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.KmuIcds == null)
            {
                return Problem("Entity set 'KMUContext.KmuIcd'  is null.");
            }
            var KmuIcd = await _context.KmuIcds.FindAsync(id);
            if (KmuIcd != null)
            {
                _context.KmuIcds.Remove(KmuIcd);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool KmuIcdExists(string id)
        {
            return _context.KmuIcds.Any(e => e.IcdCode == id);
        }



        public string UpdateStatus(string inIcdCode)
        {
            string strR = "";
            string strStatus = "";

            if (inIcdCode == null || _context.KmuIcds == null)
            {
                strR = "修改出現錯誤!!";

            }
            else
            {
                if (!KmuIcdExists(inIcdCode))
                {
                    strR = "修改出現錯誤!!";
                }
                else
                {
                    List<KmuIcd> icdList = new List<KmuIcd>();
                    var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

                    icdList = _context.KmuIcds.Where(e => e.IcdCode == inIcdCode).ToList();

                    if (icdList.Any())
                    {
                        try
                        {
                            KmuIcd icd = icdList.First();
                            icd = icdList.First();

                            strStatus = icd.Status;

                            if (strStatus == "1")
                            {
                                icd.Status = "2";
                            }
                            else
                            {
                                icd.Status = "1";
                            }

                            icd.ModifyDate = DateTime.Now;
                            icd.ModifyUser = login.EMPCODE;
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
