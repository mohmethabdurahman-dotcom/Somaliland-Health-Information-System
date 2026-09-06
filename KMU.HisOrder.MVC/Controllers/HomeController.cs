using KMU.HisOrder.MVC.Areas.Maintenance.Models;
using KMU.HisOrder.MVC.Areas.Maintenance.ViewModels;
using KMU.HisOrder.MVC.Models;
using MessagePack;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.Localization;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

namespace KMU.HisOrder.MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        private readonly KMUContext _context;

        public HomeController(ILogger<HomeController> logger, IStringLocalizer<SharedResource> localizer, KMUContext context)
        {
            _logger = logger;
            _localizer = localizer;
            _context = context;
        }

        public IActionResult Help()
        {
            return View();
        }

        public IActionResult Details(string? id, string? cancelList, string? hideList, DateTime ChosenDate)
        {
            TempData["DepName"] = _context.KmuDepartments.SingleOrDefault(e => e.DptCode == id).DptName;
            TempData["DeptId"] = _context.KmuDepartments.SingleOrDefault(e => e.DptCode == id).DptCode;

            if (id == null || id == "")
            {
                return NotFound();
            }

            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            DateOnly chosenDate = DateOnly.FromDateTime(ChosenDate);

            if (chosenDate == DateOnly.MinValue)
            {
                var res = _context.Registrations.Where(R => R.RegDepartment == id && (R.RegDate == today)).ToList();
                return View(res);
            }

            var result = _context.Registrations.Where(R => R.RegDepartment == id && (R.RegDate == chosenDate)).ToList();

            
            return View(result);
        }

        //加入下方會自動檢查是否有認證
        //[Authorize]//改成設定在Program.cs全專案都適用登入驗證
        public IActionResult Index()
        {
            ViewBag.Account = _localizer["Account"];
            ViewBag.Password = _localizer["Password"];

            List<PatientCountIng> YearOfPatientList = GetThreeMonsPatientList(); //整年的病人

            #region Organize effective year-round patients

            List<string> YearData = new List<string>();
            List<string> YearCat = new List<string>();
            YearData.Add("Count"); //the first to give the name
            var yL = YearOfPatientList.OrderBy(c => c.YearMon).GroupBy(c => c.YearMon);
            foreach (var item in yL)
            {
                string cat = item.Select(c => c.YearMon).First();
                YearData.Add(item.Count().ToString());
                YearCat.Add(cat);
            }
            #endregion Organize effective year-round patients

            ViewData["YearData"] = YearData;
            ViewData["YearCat"] = YearCat;

            return View();
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Organize pie chart data
        /// </summary>
        public ReloadChart ChartDataCollation(DateTime? ChosenDate)
        {
            ReloadChart ChartData = new ReloadChart();

            DateTime _regDate = ChosenDate == null ? DateTime.Today : (DateTime)ChosenDate;

            List<PieChart> PieChartData = new List<PieChart>();
            List<PatientCountIng> PatientList = GetPatientList(_regDate);
            List<DeptForChart> ClinicSchedules = GetClinicSchedules(_regDate).OrderBy(c => c.ScheDptCode).ToList();

            ChartData.pList = PatientList.ToList();

            foreach (DeptForChart items in ClinicSchedules)
            {
                PieChart datas = new PieChart();
                List<string> Completed = new List<string>();
                List<string> UnCompleted = new List<string>();
                datas.PieId = items.ScheDptCode;
                datas.PieName = items.ScheDptName;
                datas.dpt_category = items.dpt_category;
                int count = PatientList.Count(c => c.DepCode == items.ScheDptCode );
                int Examin = PatientList.Count(c => c.DepCode == items.ScheDptCode && c.RegStatus == "T");
                int Observ = PatientList.Count(c => c.DepCode == items.ScheDptCode && c.RegStatus == "O");
                int Finish = PatientList.Count(c => c.DepCode == items.ScheDptCode && c.RegStatus == "*");
                int Completedcount = (Examin + Observ + Finish);
                // Completed.Add("Completed");
                // UnCompleted.Add("UnCompleted");
                Completed.Add("Finished");
                UnCompleted.Add("Examining");

                Completed.Add(Completedcount.ToString());
                UnCompleted.Add((count-Completedcount).ToString());
                datas.Cloumns = new List<List<string>> {
                    Completed,
                    UnCompleted
                };
                PieChartData.Add(datas);
            }

            int TotalPatitents = PatientList.Count();
            int Female = PatientList.Count(c=>c.Sex == "F");
            int Male = TotalPatitents - Female;

            int Obser = PatientList.Count(c => c.RegStatus == "O");
            int Exam = PatientList.Count(c => c.RegStatus == "T");
            int Fin = PatientList.Count(c => c.RegStatus == "*");
            int Comp = (Exam + Fin + Obser);

            //int Comp = PatientList.Count(c=>c.RegStatus == "*");

            ChartData.PieChartList = PieChartData;
            ChartData.Total = TotalPatitents;
            ChartData.FemaleCount = Female;
            ChartData.MaleCount = Male;
            ChartData.CompletedCount = Comp;

            return ChartData;
        }

        /// <summary>
        /// Capture the number of people in each clinic
        /// </summary>
        /// <param name="inRegDate"></param>
        /// <returns></returns>
        public List<PatientCountIng> GetPatientList(DateTime inRegDate)
        {
            
            //List<PatientCountIng> pList = new List<PatientCountIng>();
            var ptData = from  d in _context.Set<Registration>()
                         join dept in _context.Set<KmuDepartment>()
                         on d.RegDepartment equals dept.DptCode
                         join chart in _context.Set<KmuChart>()
                         on d.RegHealthId equals chart.ChrHealthId
                         where d.RegDate == DateOnly.FromDateTime(inRegDate)
                         where d.RegStatus != "C" 
                         select new PatientCountIng
                         {
                             DepCode = dept.DptCode,
                             Department = dept.DptName,
                             RegNoon = d.RegNoon,
                             RegSeqNo = d.RegSeqNo,
                             Sex = chart.ChrSex,
                             RegStatus = d.RegStatus,
                             Inhospid = d.Inhospid,
                             RegPatientId = d.  RegHealthId,
                             dpt_category = dept.DptCategory
                         };


            if (ptData.Any())
            {
                return ptData.ToList();
            }
            else
            {
                return new List<PatientCountIng>();
            }
        }

        /// <summary>
        /// Find out which clinics are open that day
        /// </summary>
        /// <returns></returns>
        public List<DeptForChart> GetClinicSchedules(DateTime ChosenDate)
        {
            var today = ChosenDate.DayOfWeek.ToString("d");

            var csData = from c in _context.Set<ClinicSchedule>()
                         join b in _context.Set<KmuDepartment>()
                         on c.ScheDptCode equals b.DptCode
                         where c.ScheWeek == today
                         where c.ScheOpenFlag == "Y"
                         select new DeptForChart
                         {
                             ScheWeek = c.ScheWeek,
                             ScheNoon = c.ScheNoon,
                             ScheDptCode = c.ScheDptCode,
                             ScheDptName = c.ScheDptName,
                             ScheRoom = c.ScheRoom,
                             ScheOpenFlag = c.ScheOpenFlag,
                             dpt_category = b.DptCategory
                                                        };

            if (csData.Any())
            {
                return csData.ToList();
            }
            else
            {
                return new List<DeptForChart>();
            }

        }

        /// <summary>
        /// Capture the patients who have been registered and completed throughout the year
        /// </summary>
        /// <returns></returns>
        public List<PatientCountIng> GetThreeMonsPatientList()
        {
            DateTime today = DateTime.Today;
            DateTime TwoMonsAgo = today.AddMonths(-2);
            TwoMonsAgo = TwoMonsAgo.AddDays(-TwoMonsAgo.Day + 1);  //take the first day

            DateTime monthEnd = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

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
                         where b.CreateDate >= TwoMonsAgo && b.CreateDate <= monthEnd
                         where (d.RegStatus == "*")
                         where d.RegDate >= DateOnly.FromDateTime(TwoMonsAgo) && d.RegDate <= DateOnly.FromDateTime(monthEnd)
                         select new PatientCountIng
                         {
                             Department = dept.DptName,
                             RegSeqNo = d.RegSeqNo,
                             Sex = chart.ChrSex,
                             RegStatus = d.RegStatus,
                             Inhospid = d.Inhospid,
                             RegPatientId = d.RegHealthId,
                             YearMon = d.RegDate.ToString("yyyyMM")
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

        /// <summary>
        /// Capture the patients who have been diagnosed on the day of the consultation and have not been deleted
        /// </summary>
        /// <param name="inRegDate"></param>
        /// <returns></returns>
        public List<Hisorderplan> GetHisPlans(DateTime inRegDate)
        {

            var hisp = from b in _context.Set<Hisorderplan>()
                       where b.HplanType == "ICD"
                       where b.Status == '2'
                       where b.DcDate != null
                       where b.CreateDate == inRegDate
                       select new Hisorderplan
                       {
                           Inhospid = b.Inhospid
                       };

            if (hisp.Any())
            {
                return hisp.ToList();
            }
            else
            {
                return new List<Hisorderplan>();
            }
        }
    }
}