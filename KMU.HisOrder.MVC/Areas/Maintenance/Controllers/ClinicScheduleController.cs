using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Areas.Maintenance.Models;
using KMU.HisOrder.MVC.Areas.Maintenance.ViewModels;
using KMU.HisOrder.MVC.Extesion;
//using KMU.HisOrder.MVC.Migrations;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using static KMU.HisOrder.MVC.Models.EnumClass;

namespace KMU.HisOrder.MVC.Areas.Maintenance.Controllers
{
    [Area("Maintenance")]
    [Authorize(Roles = "Maintain_ClinicSchedule")]//登入後可依據設定的 專案名稱 project_id 作為判斷依據
    public class ClinicScheduleController : Controller
    {
        private readonly KMUContext _context;

        public ClinicScheduleController(KMUContext context)
        {
            _context = context;
        }

        #region View Action

        public IActionResult ClinicSchedule()
        {
            #region Login Session Check

            var checklogin = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
            if (checklogin == null || string.IsNullOrWhiteSpace(checklogin.EMPCODE))
            {
                return RedirectToAction("Login/Index");
            }

            #endregion

            return View();
        }

        public PartialViewResult ScheduleSearch(string Week, string Room, string Type)
        {
            #region Variable Setting

            ScheduleListReturn scheduleData = new ScheduleListReturn();
            #endregion

            using (ClinicScheduleService service = new ClinicScheduleService(_context))
            {
                scheduleData = service.GetScheduleDataForMaintain(Week, Room, Type, EnumClass.DisplayLanguage.English);
            }

            return PartialView("PartialViews/_ScheduleDataPartialView", scheduleData);
        }

        public PartialViewResult OpenModalContent(string Type, string Key,string Search,string Value)
        {
            #region Variable Setting

            ScheduleData modal = new ScheduleData();
            EnumClass.ActionCode Action = ActionCode.Create;
            string? Week = "";
            string? Noon = "";
            string? Room = "";
            string? Shift = "";

            List<EmployeeList> empLists = new List<EmployeeList>();
            //List<string> empStrList = new List<string>();
            List<DepartmentList> dptLists = new List<DepartmentList>();
            //List<string> dptStrList = new List<string>();
            #endregion

            foreach (EnumClass.ActionCode action in Enum.GetValues(typeof(ActionCode)))
            {
                if (action.EnumToString() == Type)
                {
                    Action = action;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(Key))
            {
                Week = Key.Split("_")[0];
                Noon = Key.Split("_")[1];
                Room = Key.Split("_")[2];
                Shift = Key.Split("_")[3];
            }

            #region Common Service

            using (CommonService CService = new CommonService(_context))
            {
                empLists = CService.GetEmpList().Where(c=>c.Category =="1").ToList();
                
                //2023.05.02 GetDptList.Category不指定 update by elain.
                dptLists = CService.GetDptList("");
            }

            //foreach (var emp in empLists)
            //{
            //    empStrList.Add(emp.UserID + "-" + emp.UserName);
            //}

            //foreach (var dpt in dptLists)
            //{
            //    dptStrList.Add(dpt.DptCode + "-" + dpt.DptName + "-" + dpt.DptCategory);
            //}

            #endregion

            #region ClinicSchedule Service

            using (ClinicScheduleService SService = new ClinicScheduleService(_context))
            {
                modal = SService.GetScheduleModal(Week, Noon, Room, Shift);
            }


            #endregion

            ViewData["Action"] = Action;
            ViewData["Type"] = Search;
            ViewData["Value"] = Value;
            ViewData["empLists"] = empLists;
            ViewData["dptLists"] = dptLists;
            return PartialView("PartialViews/_ScheduleModel", modal);
        }

        #endregion

        public string UpdateOpenFlag(string Week, string Noon, string Room, string Shift)
        {
            #region Variable Setting

            OpenFlagReturn result = new OpenFlagReturn();
            #endregion

            #region Login Session Check

            var checklogin = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

            #endregion

            using (ClinicScheduleService service = new ClinicScheduleService(_context))
            {
                result = service.UpdateOpenFlag(Week, Shift, Noon, Room, checklogin.EMPCODE, EnumClass.DisplayLanguage.English);
            }

            return JsonConvert.SerializeObject(result);
        }

        public string ModifySchedule(string Week,string Shift, string Noon, string Room, string Dpt, string DptName, string Doctor, string DoctorName, string openFlag, string remark,string action, string oldweek, string oldshift, string oldnoon, string oldroom, string olddepartment, string olddoctor)
        {
            #region Variable Setting
       
            ReturnMsg result = new ReturnMsg();

            #endregion

            #region Login Session Check

            var checklogin = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

            #endregion

            using (ClinicScheduleService service = new ClinicScheduleService(_context))
            {
                result = service.ModifyScheduleData(Week, Shift, Noon, Room, Dpt, DptName, Doctor, DoctorName, openFlag, remark, checklogin.EMPCODE, action, oldweek,oldshift,oldnoon, oldroom, olddepartment, olddoctor, EnumClass.DisplayLanguage.English);
            }

            return JsonConvert.SerializeObject(result);
        }
    }
}
