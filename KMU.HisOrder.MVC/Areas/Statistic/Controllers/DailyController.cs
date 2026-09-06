using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using System.Data;

namespace KMU.HisOrder.MVC.Areas.Statistic.Controllers
{
    [Area("Statistic")]
    [Authorize(Roles = "Statistic")]
    public class DailyController : Controller
    {
        private readonly KMUContext _context;

        public DailyController(KMUContext context)
        {
            _context = context;
        }

        
        public List<KmuDepartment> GetKmuDepartments()
        {
            var deptData = from d in _context.Set<KmuDepartment>()
                           where d.DptStatus == "Y"
                           //where d.DptParent != null
                           //where d.DptParent != ""
                           orderby d.DptCode
                           select new KmuDepartment
                           {
                               DptParent = d.DptParent,
                               DptCode = d.DptCode,
                               DptName = d.DptName
                           };


            if (deptData.Any())
            {
                return deptData.ToList();
            }
            else
            {
                return new List<KmuDepartment>();
            }
        }

        public IActionResult Index()
        {
            #region Login Session Check
            var checklogin = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
            if (checklogin == null || string.IsNullOrWhiteSpace(checklogin.EMPCODE))
            {
                return RedirectToAction("Login/Index");
            }
            #endregion

            List<KmuDepartment> deptData = GetKmuDepartments();
            List<KmuDepartment> deptParents = new List<KmuDepartment>();
            deptParents = deptData.Where(c => string.IsNullOrEmpty(c.DptParent)).ToList();

            JArray Datas = new JArray();
            foreach (KmuDepartment deptItems in deptParents)
            {
                List<KmuDepartment> ChildDept = new List<KmuDepartment>();
                ChildDept = deptData.Where(c => c.DptParent == deptItems.DptCode).ToList();
                JObject datass = new JObject(
                     new JProperty("type", "optgroup"),
                     new JProperty("label", deptItems.DptName),
                     new JProperty("children",
                         new JArray(from it in ChildDept
                                    select new JObject(
                                    new JProperty("text", it.DptName),
                                    new JProperty("value", it.DptCode)
                                    )
                         )));
                Datas.Add(datass);
            }

            return View(Datas);
        }



        public ChartRelated MonthsPatientsDataCollation(string parent, int Month, List<string> deptList)
        {
            List<PatientCountIng> ptData = DeptPatients(deptList, Month).OrderBy(c => c.YearMon).ToList();

            ChartRelated result = new ChartRelated();
            result.HaveData = true;

            List<string> YearCat = new List<string>();
            List<string> DepKind = new List<string>();
            List<List<string>> mcloums = new List<List<string>>();

            if (ptData.Any())
            {
                //Deal with subjects first
                DepKind = ptData.Select(c => c.Department).ToList();
                DepKind = DepKind.Distinct().ToList();   //['0100','0102','0111']

                //First process the date classification
                YearCat = ptData.Select(c => c.YearMon).ToList();
                YearCat = YearCat.Distinct().ToList();

                for (int j = 0; j < DepKind.Count(); j++)
                {
                    List<string> YearData = new List<string>();
                    for (int i = 0; i < YearCat.Count(); i++)
                    {
                        string num = "0";
                        try
                        {
                            var aaa = ptData.Where(c => c.YearMon.Trim() == YearCat[i] && c.Department.Trim() == DepKind[j]);

                            if (aaa.Any())
                            {
                                num = aaa.Count().ToString();
                            }
                        }
                        catch (Exception e)
                        {
                            string a = e.ToString();
                            //result.HaveData = false;
                        }
                        YearData.Add(num);
                    }
                    YearData.Insert(0, DepKind[j]);
                    mcloums.Add(YearData);
                }
            }
            else
            {
                result.HaveData = false;
            }
            YearCat.Insert(0, "x");
            result.mcloumns = mcloums;
            result.categories = YearCat;
            result.cloumns = DepKind;

            return result;
        }

        public List<PatientCountIng> DeptPatients(List<string> deptList, int Month)
        {
            var cy = DateTime.Now.Year;
            var days = DateTime.DaysInMonth(cy,Month);


            var ptData = from d in _context.Set<Registration>()
                         join dept in _context.Set<KmuDepartment>()
                         on d.RegDepartment equals dept.DptCode
                         join chart in _context.Set<KmuChart>()
                         on d.RegHealthId equals chart.ChrHealthId
                         join b in _context.Set<Hisorderplan>()
                         on d.Inhospid equals b.Inhospid
                         where b.HplanType == "ICD"
                         where b.Status == '2'
                         where b.DcDate == null
                         where b.CreateDate >= new DateTime(cy, Month, 1) && b.CreateDate <= new DateTime(cy, Month, days)
                         //where d.RegDepartment == deptCode
                         where deptList.Contains(d.RegDepartment)
                         where (d.RegStatus == "*")
                         where d.RegDate >= DateOnly.FromDateTime(new DateTime(cy, Month, 1)) && d.RegDate <= DateOnly.FromDateTime(new DateTime(cy, Month, days))
                         orderby d.RegDate, d.RegDepartment
                         select new PatientCountIng
                         {
                             Department = dept.DptName,
                             RegSeqNo = d.RegSeqNo,
                             Sex = chart.ChrSex,
                             RegStatus = d.RegStatus,
                             Inhospid = d.Inhospid,
                             RegPatientId = d.RegHealthId,
                             YearMon = d.RegDate.ToString("MM/dd")
                         };

            ptData = ptData.Distinct();
        

            if (ptData.Any())
            {
                return ptData.ToList();
            }
            else
            {
                return new List<PatientCountIng>();
            }
        }
    }
}
