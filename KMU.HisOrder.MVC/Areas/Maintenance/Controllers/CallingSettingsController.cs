using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Areas.HisOrder.ViewModels;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMU.HisOrder.MVC.Areas.Maintenance.Controllers
{
    [Area("Maintenance")]
    [Authorize(Roles = "Maintain_CallingSetting")]//登入後可依據設定的 專案名稱 Calling_Settings 作為判斷依據
    public class CallingSettingsController : Controller
    {
        private readonly KMUContext _context;

        public CallingSettingsController(KMUContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index() 
        {
            return View(await _context.KmuCoderefs.Where(x => x.RefCodetype == "call_area" | x.RefCodetype == "clinic_room").OrderBy(x => x.RefCode).ToListAsync());
        }

        public IActionResult Close(string id)
        {
            var result = _context.KmuCoderefs.Find(id);
            if (result.RefCasetype == "Y")
            {
                result.RefCasetype = "N";
            }
            else
            {
                result.RefCasetype = "Y";
            }
            var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
            result.ModifyId = login.EMPCODE;
            _context.Update(result);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Search(string Code, string Name)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                var temp = _context.KmuCoderefs.OrderBy(x => x.RefCodetype).ThenBy(x => x.RefCode).ToListAsync();

                return View(await temp);
            }
            else
            {
                Name = Name.Trim().ToLower();

                var temp = _context.KmuCoderefs.Where(x =>
                (
                    (Code == null) ? x.RefName == x.RefName :
                        ((Code == "-") ? x.RefName == x.RefName : x.RefName == Code)
                )
                && (
                        (Name == null) ? x.RefCode == x.RefCode :
                        (x.RefCode.ToLower().Contains(Code) || x.RefName.ToLower().Contains(Name))
                    )
                ).OrderBy(x => x.RefCode).ThenBy(x => x.RefName).ToListAsync();

                return View(await temp);
            }
        }

        public IActionResult CreateRoom()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRoom([Bind("RefCode,RefCodetype,RefName,RefDes,RefId,RefCasetype,RefShowseq,RefDes2,ModifyId,ModifyTime,RefDefaultFlag")] KmuCoderef kmuCoderef)
        {
            if (ModelState.IsValid)
            {
                
                var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
                kmuCoderef.ModifyId = login.EMPCODE;
                kmuCoderef.ModifyTime = DateTime.Now;
                kmuCoderef.RefId = "";

                _context.Add(kmuCoderef);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(kmuCoderef);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create( KmuCoderef kmuCoderef)
        {
            if (ModelState.IsValid)
            {
                

                var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
                kmuCoderef.ModifyId = login.EMPCODE;
                kmuCoderef.ModifyTime= DateTime.Now;
                kmuCoderef.RefCasetype = "Y";

                kmuCoderef.RefId = "";
                _context.Add(kmuCoderef);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(kmuCoderef);
        }
       

        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.KmuCoderefs == null)
            {
                return NotFound();
            }

            var kmuCoderef = await _context.KmuCoderefs.FindAsync(id);
            if (kmuCoderef == null)
            {
                return NotFound();
            }
            return View(kmuCoderef);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("RefCode,RefCodetype,RefName,RefDes,RefId,RefCasetype,RefShowseq,RefDes2,ModifyId,ModifyTime,RefDefaultFlag")] KmuCoderef kmuCoderef)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
                    kmuCoderef.ModifyId= login.EMPCODE;
                    kmuCoderef.ModifyTime = DateTime.Now;
                    kmuCoderef.RefId = id;

                    _context.Update(kmuCoderef);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KmuCodeRefExists(kmuCoderef.RefId))
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
            return View(kmuCoderef);
        }


        // GET: Maintenance/KmuCodref/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.KmuCoderefs == null)
            {
                return NotFound();
            }

            var kmuCoderef = await _context.KmuCoderefs
                .FirstOrDefaultAsync(m => m.RefId == id);
            if (kmuCoderef == null)
            {
                return NotFound();
            }

            return View(kmuCoderef);
        }

        // POST: Maintenance/KmuCoderefs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.KmuCoderefs == null)
            {
                return Problem("Entity set 'KMUContext.KmuCodeRefs'  is null.");
            }
            var KmuCoderefs = await _context.KmuCoderefs.FindAsync(id);
            if (KmuCoderefs != null)
            {
                _context.KmuCoderefs.Remove(KmuCoderefs);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        private bool KmuCodeRefExists(string id)
        {
            return _context.KmuCoderefs.Any(e => e.RefId == id);
        }

    }
}
