using KMU.HisOrder.MVC.Areas.HisCalling.Models;
using KMU.HisOrder.MVC.Extesion;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;

namespace KMU.HisOrder.MVC.Areas.HisCalling.Controllers
{
    [Area("HisCalling")]
    [AllowAnonymous]
    public class CallController : Controller
    {
        private readonly KMUContext _context;

        public CallController(KMUContext context)
        {
            _context = context;
        }

        public IActionResult Index(string iarea = null, string inoon = null)
        {
            //星期, 取自enum DayOfWeek, 預設系統時間
            List<SelectListItem> _weekList = new List<SelectListItem>();
            foreach (var w in Enum.GetValues(typeof(DayOfWeek)))
            {
                _weekList.Add(new SelectListItem()
                {
                    Text = w.ToString(),
                    Value = ((int)w).ToString(),
                    Selected = (int)DateTime.Now.DayOfWeek == (int)w ? true : false
                });
            }
            ViewData["VD_week"] = _weekList;

            //區域, 預設輸入值
            CallService service = new CallService(_context);
            var _areaList = service.getCallArea(iarea);
            if (_areaList.Count() == 1)
            {
                _areaList.First().Selected = true;
            }
            ViewData["VD_area"] = _areaList;

            //午別, 取自EnumClass.EnumNoon, 預設輸入值
            List<SelectListItem> _noonList = new List<SelectListItem>();
            foreach (EnumClass.EnumNoon n in Enum.GetValues(typeof(EnumClass.EnumNoon)))
            {
                _noonList.Add(new SelectListItem()
                {
                    Text = n.EnumToString(),
                    Value = n.EnumToString(),
                    Selected = n.EnumToString() == inoon ? true : false
                });
            }
            if (_noonList.Count() == 1)
            {
                _noonList.First().Selected = true;
            }
            ViewData["VD_noon"] = _noonList;

            return View();
        }

        public IActionResult View(string iweek, string iarea, string inoon)
        {
            var list = fn_getCallGroup(iweek, iarea, inoon);
            ViewData["VD_clinic"] = list;

            var _display_clock = true;
            //if (list.Count() < 4 || (list.Count() >= 4 && list.Count() % 2 !=0) )
            //{
            //    _display_clock = true;
            //}
            ViewData["VD_clock"] = _display_clock;

            return View();
        }

        [HttpPost]
        public string getCallGroup(string iweek, string iarea, string inoon)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();
            var list = fn_getCallGroup(iweek, iarea, inoon);
            if (list.Any())
            {
                result = list.ToDictionary(r => r.ScheRoom, r => (int)r.ScheCallNo);
            }

            return JsonConvert.SerializeObject(result);
        }

        private List<ClinicSchedule> fn_getCallGroup(string iweek, string iarea, string inoon)
        {
            CallService service = new CallService(_context);
            return service.getCallGroup(iweek, iarea, inoon);
        }
    }
}
