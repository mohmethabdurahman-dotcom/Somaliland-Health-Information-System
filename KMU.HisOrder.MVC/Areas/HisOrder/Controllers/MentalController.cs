using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Areas.HisOrder.ViewModels;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace KMU.HisOrder.MVC.Areas.HisOrder.Controllers
{
    [Area("HisOrder")]

    public class MentalController : Controller
    {
        private readonly KMUContext _context;

        public MentalController(KMUContext context)
        {
            _context = context;
        }

        public IActionResult Index(string inhospitalId, string HealthId, string NmType, string date)
        {
            var dt = Convert.ToDateTime(date).ToString("MM/dd/yyyy");

            TempData["InhospitalId"] = inhospitalId;
            TempData["HealthId"] = HealthId;
            TempData["NmType"] = NmType;
            TempData["Date"] = dt;


            IEnumerable<ExtendKmuNonMedicine> data = (from a in _context.KmuNonMedicines
                                                      join b in _context.KmuCoderefs on a.GroupCode equals b.RefCode
                                                      join c in _context.kmu_mental on a.ItemId equals c.plancode
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
                                                          enabled = a.enabled,
                                                          PlanDes = c.plandes
                                                      });

            switch (NmType)
            {
                case "Biography":
                    data = data.Where(c => c.ItemType == "17").ToList();
                    break;
                case "Hist":
                    data = data.Where(c => c.ItemType == "18").ToList();
                    break;
                case "DC":
                    data = data.Where(c => c.ItemType == "19").ToList();
                    break;
                case "PSHX":
                    data = data.Where(c => c.ItemType == "20").ToList();
                    break;
                case "AH":
                    data = data.Where(c => c.ItemType == "21").ToList();
                    break;
                case "MSE":
                    data = data.Where(c => c.ItemType == "22").ToList();
                    break;
                case "GPE":
                    data = data.Where(c => c.ItemType == "23").ToList();
                    break;
                case "PI":
                    data = data.Where(c => c.ItemType == "24").ToList();
                    break;
                case "DEV":
                    data = data.Where(c => c.ItemType == "25").ToList();
                    break;
                default:
                    break;
            }

            List<VKmuNonMedicine> vdata = new List<VKmuNonMedicine>();
            foreach (ExtendKmuNonMedicine p in data)
            {
                p.ItemType = p.ItemType == "17" ? "Biography" : p.ItemType == "18" ? "Hist" : p.ItemType == "19" ? "DC" : p.ItemType == "20" ? "PSHX" : p.ItemType == "21" ? "AH" : p.ItemType == "22" ? "MSE" : p.ItemType == "23" ? "GPE" : p.ItemType == "24" ? "PI" : p.ItemType == "25" ? "DEV" : "";

                vdata.Add(new VKmuNonMedicine(p));
            }

            if (vdata.Count == 0)
            {
                return RedirectToAction("Update", new { inhospitalId = inhospitalId, HealthId = HealthId, NmType = NmType, date = date, Prevdata = vdata });
            }

            return View(vdata);
        }


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
                case "Biography":
                    data = data.Where(c => c.ItemType == "17").ToList();
                    break;
                case "Hist":
                    data = data.Where(c => c.ItemType == "18").ToList();
                    break;
                case "DC":
                    data = data.Where(c => c.ItemType == "19").ToList();
                    break;
                case "PSHX":
                    data = data.Where(c => c.ItemType == "20").ToList();
                    break;
                case "AH":
                    data = data.Where(c => c.ItemType == "21").ToList();
                    break;
                case "MSE":
                    data = data.Where(c => c.ItemType == "22").ToList();
                    break;
                case "GPE":
                    data = data.Where(c => c.ItemType == "23").ToList();
                    break;
                case "PI":
                    data = data.Where(c => c.ItemType == "24").ToList();
                    break;
                case "DEV":
                    data = data.Where(c => c.ItemType == "25").ToList();
                    break;
                default:
                    break;
            }

            List<VKmuNonMedicine> vdata = new List<VKmuNonMedicine>();
            foreach (ExtendKmuNonMedicine p in data)
            {
                p.ItemType = p.ItemType == "17" ? "Biography" : p.ItemType == "18" ? "Hist" : p.ItemType == "19" ? "DC" : p.ItemType == "20" ? "PSHX" : p.ItemType == "21" ? "AH" : p.ItemType == "22" ? "MSE" : p.ItemType == "23" ? "GPE" : p.ItemType == "24" ? "PI" : p.ItemType == "25" ? "DEV" : "";

                vdata.Add(new VKmuNonMedicine(p));
            }

            return View(vdata);
        }


        [HttpPost]
        public async Task<IActionResult> SaveMnt(string inhospitalId, string healthId, string[] items, string[] inps, string NmType, string date, kmu_mental _Mental)
        {
            var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
            foreach (var item in items)
            {
                var exits = _context.kmu_mental.Where(e => e.plancode == item && e.healthid == healthId).ToList();
                var itemType = _context.KmuNonMedicines.SingleOrDefault(e => e.ItemId == item).ItemType;

                if (itemType == "17" || itemType == "18")
                {
                    if (exits.Count > 0)
                    {
                        return RedirectToAction("Update", new { inhospitalId = inhospitalId, HealthId = healthId, NmType = NmType });
                    }
                    else
                    {
                        _Mental.inhospid = inhospitalId;
                        _Mental.healthid = healthId;
                        _Mental.healthid = healthId;
                        _Mental.plancode = item;
                        foreach (var inp in inps)
                        {
                            _Mental.plandes = inp;
                        }
                        _Mental.createdate = Convert.ToDateTime(date);
                        _Mental.createuser = login.EMPCODE;
                        await _context.kmu_mental.AddRangeAsync(_Mental);

                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    var exitsitem = _context.kmu_mental.SingleOrDefault(e => e.plancode == item && e.inhospid == inhospitalId);

                    if (exitsitem != null)
                    {
                        exitsitem.healthid = healthId;
                        //exits.HealthId = healthId;
                        exitsitem.plancode = item;
                        foreach (var inp in inps)
                        {
                            exitsitem.plandes = inp;
                        }
                        //exits.CreateDate = Convert.ToDateTime(date);
                        exitsitem.createuser = login.EMPCODE;
                        _context.kmu_mental.UpdateRange(exitsitem);

                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        _Mental.inhospid = inhospitalId;
                        _Mental.healthid = healthId;
                        _Mental.plancode = item;
                        foreach (var inp in inps)
                        {
                            _Mental.plandes = inp;
                        }
                        _Mental.createdate = Convert.ToDateTime(date);
                        _Mental.createuser = login.EMPCODE;
                        await _context.kmu_mental.AddRangeAsync(_Mental);

                        await _context.SaveChangesAsync();
                    }
                }
            }

            return Json(_Mental.healthid);
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
                                                      join c in _context.kmu_mental on a.ItemId equals c.plancode

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
                                                          PlanDes = c.plandes
                                                      });

            data = data.ToList();

            List<VKmuNonMedicine> vdata = new List<VKmuNonMedicine>();
            foreach (ExtendKmuNonMedicine p in data)
            {
                p.ItemType = p.ItemType == "17" ? "Biography" : p.ItemType == "18" ? "Hist" : p.ItemType == "19" ? "DC" : p.ItemType == "20" ? "PSHX" : p.ItemType == "21" ? "AH" : p.ItemType == "22" ? "MSE" : p.ItemType == "23" ? "GPE" : p.ItemType == "24" ? "PI" : p.ItemType == "25" ? "DEV" : "";
                vdata.Add(new VKmuNonMedicine(p));
            }

            return View(vdata);
        }

        [HttpGet]
        public IActionResult Detail(string healthId, string NmType, string inhosp, bool showClinic = true, bool showPrint = false)
        {
            var pateint = _context.Registrations.FirstOrDefault(e => e.Inhospid == inhosp);
            var getDepartment = _context.KmuDepartments.SingleOrDefault(e => e.DptCode == pateint.RegDepartment).DptName;

            var dpt = _context.KmuDepartments.Where(e => e.DptParent == "4000").ToList();
            List<String> dptCodes = new List<string>();
            foreach (var a in dpt)
            {
                dptCodes.Add(a.DptCode);
            }
            var patientInfo = _context.Registrations.Where(e => e.RegHealthId == healthId && dptCodes.Contains(e.RegDepartment)).ToList();
           
            TempData["NcdDept"] = getDepartment;
            TempData["inhospital"] = inhosp;
            TempData["healthId"] = healthId;
            TempData["regDate"] = pateint.RegDate;
            TempData["RegDept"] = pateint.RegDate;

            return View(patientInfo);
        }

    }
}
