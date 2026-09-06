using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KMU.HisOrder.MVC.Models;
using KMU.HisOrder.MVC.Areas.HisOrder.ViewModels;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authorization;
using KMU.HisOrder.MVC.Extesion;
using static KMU.HisOrder.MVC.Models.EnumClass;
using KMU.HisOrder.MVC.Areas.Reservation.ViewModels;

namespace KMU.HisOrder.MVC.Areas.HisOrder.Controllers
{
    [CheckClinicSessionAttribute]
    [CheckSessionTimeOutAttribute]
    [Area("HisOrder")]
    [Authorize(Roles = "HisOrder")]

    public class NcdController : Controller
    {
        private readonly KMUContext _context;

        public NcdController(KMUContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index(string inhospitalId, string HealthId, string NmType, string date)
        {
            var dt = Convert.ToDateTime(date).ToString("MM/dd/yyyy");

            TempData["InhospitalId"] = inhospitalId;
            TempData["HealthId"] = HealthId;
            TempData["NmType"] = NmType;
            TempData["Date"] = dt;


            IEnumerable<ExtendKmuNonMedicine> data = (from a in _context.KmuNonMedicines
                                                      join b in _context.KmuCoderefs on a.GroupCode equals b.RefCode
                                                      join c in _context.kmu_ncd on a.ItemId equals c.plancode
                                                      where a.Status == '1' && b.RefCodetype == "group_code" && c.healthid == HealthId
                                                      orderby a.ShowSeq, a.ItemId
                                                      select new ExtendKmuNonMedicine()
                                                      {
                                                          ItemId = a.ItemId,
                                                          ItemName = a.ItemName,
                                                          ItemType = a.ItemType,
                                                          ItemSpec = a.ItemSpec,
                                                          StartDate = a.StartDate,
                                                          EndDate = a.EndDate,
                                                          Status = a.Status,
                                                          CreateUser = a.CreateUser,
                                                          CreateDate = a.CreateDate,
                                                          ModifyUser = a.ModifyUser,
                                                          ModifyDate = a.ModifyDate,
                                                          Remark = a.Remark,
                                                          ShowSeq = a.ShowSeq,
                                                          GroupCode = a.GroupCode,
                                                          RefName = b.RefName,
                                                          RefShowseq = b.RefShowseq,
                                                      });

            switch (NmType)
            {
                case "intake":
                    data = data.Where(c => c.ItemType == "10").ToList();
                    break;
                case "Back":
                    data = data.Where(c => c.ItemType == "17" && c.GroupCode == "MS" || c.GroupCode == "OC").ToList();
                    break;
                case "Comor":
                    data = data.Where(c => c.ItemType == "11").ToList();
                    break;
                case "Hist":
                    data = data.Where(c => c.ItemType == "12").ToList();
                    break;
                case "Vital":
                    data = data.Where(c => c.ItemType == "0").ToList();
                    break;
                case "Hyper":
                    data = data.Where(c => c.ItemType == "13").ToList();
                    break;
                case "Asthma":
                    data = data.Where(c => c.ItemType == "14").ToList();
                    break;
                case "Heart":
                    data = data.Where(c => c.ItemType == "15").ToList();
                    break;
                case "Diabetes":
                    data = data.Where(c => c.ItemType == "16").ToList();
                    break;
                default:
                    break;
            }

            List<VKmuNonMedicine> vdata = new List<VKmuNonMedicine>();
            foreach (ExtendKmuNonMedicine p in data)
            {
                p.ItemType = p.ItemType == "10" ? "intake" : p.ItemType == "17" ? "Back" : p.ItemType == "11" ? "Comor" : p.ItemType == "12" ? "Hist" : p.ItemType == "0" ? "Vital" : p.ItemType == "13" ? "Hyper" : p.ItemType == "14" ? "Asthma" : p.ItemType == "15" ? "Heart" : p.ItemType == "16" ? "Diabetes" : "";
                vdata.Add(new VKmuNonMedicine(p));
            }

            if (vdata.Count == 0)
            {
                return RedirectToAction("Update", new { inhospitalId = inhospitalId, HealthId = HealthId, NmType = NmType, date = date, Prevdata = vdata });
            }

            return View(vdata);
        }
        [HttpGet]
        public IActionResult Update(string inhospitalId, string HealthId, string NmType, string date)
        {
            var dt = Convert.ToDateTime(date).ToString("MM/dd/yyyy");

            TempData["InhospitalId"] = inhospitalId;
            TempData["HealthId"] = HealthId;
            TempData["NmType"] = NmType;
            TempData["Date"] = dt;
            IEnumerable<ExtendKmuNonMedicine> data = (from a in _context.KmuNonMedicines
                                                      join b in _context.KmuCoderefs on a.GroupCode equals b.RefCode
                                                      where a.Status == '1' && b.RefCodetype == "group_code"
                                                      orderby a.ShowSeq, a.ItemId
                                                      select new ExtendKmuNonMedicine()
                                                      {
                                                          ItemId = a.ItemId,
                                                          ItemName = a.ItemName,
                                                          ItemType = a.ItemType,
                                                          ItemSpec = a.ItemSpec,
                                                          StartDate = a.StartDate,
                                                          EndDate = a.EndDate,
                                                          Status = a.Status,
                                                          CreateUser = a.CreateUser,
                                                          CreateDate = a.CreateDate,
                                                          ModifyUser = a.ModifyUser,
                                                          ModifyDate = a.ModifyDate,
                                                          Remark = a.Remark,
                                                          ShowSeq = a.ShowSeq,
                                                          GroupCode = a.GroupCode,
                                                          RefName = b.RefName,
                                                          RefShowseq = b.RefShowseq,
                                                          enabled = a.enabled
                                                      });

            switch (NmType)
            {
                case "Back":
                    data = data.Where(c => c.ItemType == "17" && c.GroupCode == "MS" || c.GroupCode == "OC").ToList();
                    break;
                case "intake":
                    data = data.Where(c => c.ItemType == "10").ToList();
                    break;
                case "Comor":
                    data = data.Where(c => c.ItemType == "11").ToList();
                    break;
                case "Hist":
                    data = data.Where(c => c.ItemType == "12").ToList();
                    break;
                case "Vital":
                    data = data.Where(c => c.ItemType == "0").ToList();
                    break;
                case "Hyper":
                    data = data.Where(c => c.ItemType == "13").ToList();
                    break;
                case "Asthma":
                    data = data.Where(c => c.ItemType == "14").ToList();
                    break;
                case "Heart":
                    data = data.Where(c => c.ItemType == "15").ToList();
                    break;
                case "Diabetes":
                    data = data.Where(c => c.ItemType == "16").ToList();
                    break;
                default:
                    break;
            }
            List<VKmuNonMedicine> vdata = new List<VKmuNonMedicine>();
            foreach (ExtendKmuNonMedicine p in data)
            {
                p.ItemType = p.ItemType == "10" ? "intake" : p.ItemType == "17" ? "Back" : p.ItemType == "11" ? "Comor" : p.ItemType == "12" ? "Hist" : p.ItemType == "0" ? "Vital" : p.ItemType == "13" ? "Hyper" : p.ItemType == "14" ? "Asthma" : p.ItemType == "15" ? "Heart" : p.ItemType == "16" ? "Diabetes" : "";
                vdata.Add(new VKmuNonMedicine(p));
            }
            return View(vdata);
        }

        [HttpPost]
        public async Task<IActionResult> SaveNcd(string inhospitalId, string healthId, string[] items, string NmType, string[] inps, string date, kmu_ncd Ncd)
        {

            var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
            foreach (var item in items)
            {
                var exititem = _context.kmu_ncd.Where(e => e.plancode == item && e.healthid == healthId).ToList();
                var itemType = _context.KmuNonMedicines.SingleOrDefault(e => e.ItemId == item).ItemType;
                //Console.WriteLine(Patientdto.RegDate);

                if (itemType == "10" || itemType == "11" || itemType == "12" || itemType == "17")
                {
                    if (exititem.Count > 0)
                    {
                        return RedirectToAction("Update", new { inhospitalId = inhospitalId, HealthId = healthId, NmType = NmType });
                    }
                    else
                    {
                        Ncd.inhospid = inhospitalId;
                        Ncd.healthid = healthId;
                        Ncd.healthid = healthId;
                        Ncd.plancode = item;
                        Ncd.createdate = Convert.ToDateTime(date);
                        Ncd.createuser = login.EMPCODE;
                        await _context.kmu_ncd.AddRangeAsync(Ncd);

                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    var exits = _context.kmu_ncd.SingleOrDefault(e => e.plancode == item && e.healthid == healthId);

                    if (exits != null)
                    {
                        exits.healthid = healthId;
                        //exits.HealthId = healthId;
                        exits.plancode = item;
                        foreach (var inp in inps)
                        {
                            exits.patient_answer = inp;
                        }
                        //exits.CreateDate = Convert.ToDateTime(date);
                        exits.createuser = login.EMPCODE;
                        _context.kmu_ncd.UpdateRange(exits);

                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        Ncd.inhospid = inhospitalId;
                        Ncd.healthid = healthId;
                        Ncd.plancode = item;
                        foreach (var inp in inps)
                        {
                            Ncd.patient_answer = inp;
                        }
                        Ncd.createdate = Convert.ToDateTime(date);
                        Ncd.createuser = login.EMPCODE;
                        await _context.kmu_ncd.AddRangeAsync(Ncd);

                        await _context.SaveChangesAsync();
                    }
                }
            }

            return Json(Ncd.healthid);
        }

        [HttpGet]
        public IActionResult history(string id, string inhospitalId, DateTime date)
        {
            var findDpt = _context.Registrations.SingleOrDefault(e => e.Inhospid == inhospitalId).RegDepartment;
            var dptParent = _context.KmuDepartments.SingleOrDefault(e => e.DptCode == findDpt).DptParent;

            TempData["Date"] = date;
            TempData["InhospitalId"] = inhospitalId;
            TempData["HealthId"] = id;
            TempData["DeptParent"] = dptParent;



            IEnumerable<ExtendKmuNonMedicine> data = (from a in _context.KmuNonMedicines
                                                      join b in _context.KmuCoderefs on a.GroupCode equals b.RefCode
                                                      join c in _context.kmu_ncd on a.ItemId equals c.plancode

                                                      where a.Status == '1' && b.RefCodetype == "group_code" && c.healthid == id && c.createdate == date
                                                      orderby a.ShowSeq, a.ItemId
                                                      select new ExtendKmuNonMedicine()
                                                      {
                                                          ItemId = a.ItemId,
                                                          ItemName = a.ItemName,
                                                          ItemType = a.ItemType,
                                                          ItemSpec = a.ItemSpec,
                                                          StartDate = a.StartDate,
                                                          EndDate = a.EndDate,
                                                          Status = a.Status,
                                                          CreateUser = a.CreateUser,
                                                          CreateDate = a.CreateDate,
                                                          ModifyUser = a.ModifyUser,
                                                          ModifyDate = a.ModifyDate,
                                                          Remark = a.Remark,
                                                          ShowSeq = a.ShowSeq,
                                                          GroupCode = a.GroupCode,
                                                          RefName = b.RefName,
                                                          RefShowseq = b.RefShowseq,
                                                          enabled = a.enabled,
                                                          PlanDes = c.patient_answer
                                                      });

            data = data.ToList();

            List<VKmuNonMedicine> vdata = new List<VKmuNonMedicine>();
            foreach (ExtendKmuNonMedicine p in data)
            {
                p.ItemType = p.ItemType == "10" ? "intake" : p.ItemType == "11" ? "Comor" : p.ItemType == "12" ? "Hist" : p.ItemType == "0" ? "Vital" : p.ItemType == "13" ? "Hyper" : p.ItemType == "14" ? "Asthma" : p.ItemType == "15" ? "Heart" : p.ItemType == "16" ? "Diabetes" : "";
                vdata.Add(new VKmuNonMedicine(p));
            }

            return View(vdata);
        }

        [HttpDelete]
        public IActionResult deleteNcd(string inhospitalId, string healthId, string[] items, string NmType, kmu_ncd Ncd)
        {
            foreach (var item in items)
            {
                var delete = _context.kmu_ncd.Where(e => e.healthid == healthId && e.plancode == item).ToList();

                foreach (var item2 in delete)
                {
                    var deleteItem = _context.kmu_ncd.Find(item2.ncdid);
                    _context.kmu_ncd.Remove(deleteItem);
                    _context.SaveChanges();
                    return RedirectToAction("Update", new { inhospitalId = inhospitalId, HealthId = healthId, NmType = NmType });
                }
            }
            return RedirectToAction("Update", new { inhospitalId = inhospitalId, HealthId = healthId, NmType = NmType });
        }

        [HttpGet]
        public IActionResult Detail(string healthId, string NmType, string inhosp, bool showClinic = true, bool showPrint = false)
        {
            var pateint = _context.Registrations.FirstOrDefault(e => e.Inhospid == inhosp);
            var getDepartment = _context.KmuDepartments.SingleOrDefault(e => e.DptCode == pateint.RegDepartment).DptName;

            TempData["NcdDept"] = getDepartment;
            TempData["inhospital"] = inhosp;
            TempData["healthId"] = healthId;
            TempData["regDate"] = pateint.RegDate;
            TempData["NmType"] = NmType;

            //var ncditems = _context.kmu_ncd.Where(n => n.healthid == healthId).ToList();
            //if (healthId == null)
            //{
            //    return NotFound();
            //}

            var patientInfo = _context.Registrations.Where(e => e.RegHealthId == healthId).ToList();

            return View(patientInfo);
        }

        [HttpGet]
        public IActionResult statistic(string inhosp, string HealthId)
        {
            TempData["InhospitalId"] = inhosp;
            TempData["HealthId"] = HealthId;

            return View();
        }

        [HttpGet]
        public IActionResult VitalSign(string inhospitalId, string healthId, DateTime date)
        {

            var dt = date.ToString("MM/dd/yyyy");

            TempData["Date"] = dt;
            TempData["InhospitalId"] = inhospitalId;
            TempData["HealthId"] = healthId;

            var data = _context.PhysicalSigns.Where(e => e.Inhospid == inhospitalId && e.ModifyTime == date).ToList();
            return View(data);
        }
        ReserveHistoryList result = new ReserveHistoryList();

        //[HttpPost]
        //public async Task<IActionResult> vitalSign(string healthId, string[] type, string[] value, PhysicalSign physicalSign)
        //{
        //    Console.WriteLine("active");
        //    var date = DateOnly.FromDateTime(DateTime.Now);

        //    var inhospId = await _context.Registrations.SingleOrDefaultAsync(e => e.RegHealthId == healthId && e.RegDate == date);
        //    if(inhospId.Inhospid == null)
        //    {
        //        Console.WriteLine("InhospitalID is null...");
        //    }

        //    var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
        //    var exitVital = _context.PhysicalSigns.Where(e => e.Inhospid == inhospId.Inhospid && e.ModifyTime == Convert.ToDateTime(date)).ToList();


        //    if (exitVital.Any())
        //    {
        //        return View();
        //    }
        //    else
        //    {
        //        foreach (var item in type)
        //        {
        //            physicalSign.PhyId = "";
        //            physicalSign.PhyType = item;
        //            physicalSign.Inhospid = inhospId.Inhospid;
        //            physicalSign.PhyValue = "";
        //            physicalSign.ModifyTime = Convert.ToDateTime(date);
        //            physicalSign.ModifyUser = login.EMPCODE;
        //            //physicalSign.PhyDept = "Ncd_Physical";
        //            await _context.PhysicalSigns.AddRangeAsync(physicalSign);

        //            await _context.SaveChangesAsync();
        //        }
        //        var dat = Convert.ToDateTime(date);
        //        var findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Height" && DateOnly.FromDateTime((DateTime)p.ModifyTime) == DateOnly.FromDateTime(Convert.ToDateTime(dat)));
        //        foreach (var item in findinhospid)
        //        {
        //            item.PhyValue = value[0];
        //            _context.PhysicalSigns.Update(item);
        //        }

        //        findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Weight" && DateOnly.FromDateTime((DateTime)p.ModifyTime) == DateOnly.FromDateTime(Convert.ToDateTime(dat)));
        //        foreach (var item in findinhospid)
        //        {
        //            item.PhyValue = value[1];
        //            _context.PhysicalSigns.Update(item);
        //        }

        //        findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Temp" && DateOnly.FromDateTime((DateTime)p.ModifyTime) == DateOnly.FromDateTime(Convert.ToDateTime(dat)));
        //        foreach (var item in findinhospid)
        //        {
        //            item.PhyValue = value[2];
        //            _context.PhysicalSigns.Update(item);
        //        }

        //        findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == Convert.ToString(type[3]) && DateOnly.FromDateTime((DateTime)p.ModifyTime) == DateOnly.FromDateTime(Convert.ToDateTime(dat)));
        //        foreach (var item in findinhospid)
        //        {
        //            item.PhyValue = value[3];
        //            _context.PhysicalSigns.Update(item);
        //        }

        //        findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "SpO2" && DateOnly.FromDateTime((DateTime)p.ModifyTime) == DateOnly.FromDateTime(Convert.ToDateTime(dat)));
        //        foreach (var item in findinhospid)
        //        {
        //            item.PhyValue = value[4];
        //            _context.PhysicalSigns.Update(item);
        //        }

        //        findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "BP" && DateOnly.FromDateTime((DateTime)p.ModifyTime) == DateOnly.FromDateTime(Convert.ToDateTime(dat)));
        //        foreach (var item in findinhospid)
        //        {
        //            item.PhyValue = value[5];
        //            _context.PhysicalSigns.Update(item);
        //        }

        //        findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Systolic (Sys)" && DateOnly.FromDateTime((DateTime)p.ModifyTime) == DateOnly.FromDateTime(Convert.ToDateTime(dat)));
        //        foreach (var item in findinhospid)
        //        {
        //            item.PhyValue = value[6];
        //            _context.PhysicalSigns.Update(item);
        //        }

        //        findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Diastolic (Dias)" && DateOnly.FromDateTime((DateTime)p.ModifyTime) == DateOnly.FromDateTime(Convert.ToDateTime(dat)));
        //        foreach (var item in findinhospid)
        //        {
        //            item.PhyValue = value[7];
        //            _context.PhysicalSigns.Update(item);
        //        }
        //        findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "HR" && DateOnly.FromDateTime((DateTime)p.ModifyTime) == DateOnly.FromDateTime(Convert.ToDateTime(dat)));
        //        foreach (var item in findinhospid)
        //        {
        //            item.PhyValue = value[8];
        //            _context.PhysicalSigns.Update(item);
        //        }
        //        findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "RBS" && DateOnly.FromDateTime((DateTime)p.ModifyTime) == DateOnly.FromDateTime(Convert.ToDateTime(dat)));
        //        foreach (var item in findinhospid)
        //        {
        //            item.PhyValue = value[9];
        //            _context.PhysicalSigns.Update(item);
        //        }
        //        findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "FBS" && DateOnly.FromDateTime((DateTime)p.ModifyTime) == DateOnly.FromDateTime(Convert.ToDateTime(dat)));
        //        foreach (var item in findinhospid)
        //        {
        //            item.PhyValue = value[10];
        //            _context.PhysicalSigns.Update(item);
        //        }

        //        findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "Pulse" && DateOnly.FromDateTime((DateTime)p.ModifyTime) == DateOnly.FromDateTime(Convert.ToDateTime(date)));
        //        foreach (var item in findinhospid)
        //        {
        //            item.PhyValue = value[11];
        //            _context.PhysicalSigns.Update(item);
        //        }

        //        findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "MUAC" && DateOnly.FromDateTime((DateTime)p.ModifyTime) == DateOnly.FromDateTime(Convert.ToDateTime(dat)));
        //        foreach (var item in findinhospid)
        //        {
        //            item.PhyValue = value[12];
        //            _context.PhysicalSigns.Update(item);
        //        }

        //        findinhospid = _context.PhysicalSigns.Where(p => p.Inhospid == inhospId.Inhospid && p.PhyType == "HBA1C" && DateOnly.FromDateTime((DateTime)p.ModifyTime) == DateOnly.FromDateTime(Convert.ToDateTime(dat)));
        //        foreach (var item in findinhospid)
        //        {
        //            item.PhyValue = value[13];
        //            _context.PhysicalSigns.Update(item);
        //        }

        //        await _context.SaveChangesAsync();
        //        return Json(type);
        //    }

        //    return null;
        //}
    }
}