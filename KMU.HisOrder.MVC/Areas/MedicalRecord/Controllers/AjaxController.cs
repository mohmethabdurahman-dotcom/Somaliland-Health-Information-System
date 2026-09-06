using KMU.HisOrder.MVC.Areas.MedicalRecord.ViewModels;
using KMU.HisOrder.MVC.Extesion;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using KMU.HisOrder.MVC.Areas.MedicalRecord.Models;
using KMU.HisOrder.MVC.Areas.Reservation.ViewModels;
using KMU.HisOrder.MVC.Areas.Reservation.Models;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using System.Security.Claims;

namespace KMU.HisOrder.MVC.Areas.MedicalRecord.Controllers
{
    [Area("MedicalRecord")]
    [Authorize(Roles = "MedicalRecord")]//登入後可依據設定的 專案名稱 project_id 作為判斷依據
    public class AjaxController : Controller
    {
        private readonly KMUContext _context;

        public AjaxController(KMUContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        #region View Action

        public PartialViewResult PatientSearch(string? PatientID, string? MobilePhone, string? PatientName)
        {
            #region Variable Setting

            PatientListReturn PatientList = new PatientListReturn();

            #endregion

            using (MedicalRecordService service = new MedicalRecordService(_context))
            {
                PatientList = service.getPatientData(PatientID, MobilePhone, PatientName, EnumClass.DisplayLanguage.English);
            }

            return PartialView("../MedRecord/PartialViews/_PatientDataPartialView", PatientList);
        }



        #endregion

        #region Function

        public string CreateNewMR(string MRJsonData)
        {
            #region Variable Setting

            MRCreateReutrnMsg Msg = new MRCreateReutrnMsg();
            MRJSONStructure? objMRCreateDto = new MRJSONStructure();

            #endregion

            #region Login Session Check

            var checklogin = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
      
            var get_user_auth_cookie =  HttpContext.User.Claims.FirstOrDefault(t=> t.Type == "User_auth_page_Name「" + "InpatientMedications" + "」")?.Value;
            #endregion


            MRJSONStructure[]? objArray = JsonConvert.DeserializeObject(MRJsonData, typeof(MRJSONStructure[])) as MRJSONStructure[];

            objMRCreateDto = objArray.Any() ? objArray.FirstOrDefault() : null;

            foreach (EnumClass.EnumGender gender in Enum.GetValues(typeof(EnumClass.EnumGender)))
            {
                if (gender.EnumToCode().Trim() == objMRCreateDto.GENDER.Trim())
                {
                    objMRCreateDto.enumGender = gender;
                    break;
                }
            }

            foreach (EnumClass.EnumAnonymous anonymous in Enum.GetValues(typeof(EnumClass.EnumAnonymous)))
            {
                if (anonymous.EnumToCode().Trim() == objMRCreateDto.ANONYMOUSTYPE.Trim())
                {
                    objMRCreateDto.enumAnonymous = anonymous;
                    break;
                }
            }

            switch (objMRCreateDto.enumAnonymous)
            {
                case EnumClass.EnumAnonymous.FemaleKid:
                case EnumClass.EnumAnonymous.FemaleAdult:
                    objMRCreateDto.GENDER = "F";
                    objMRCreateDto.enumGender = EnumClass.EnumGender.Female;
                    break;
                case EnumClass.EnumAnonymous.MaleKid:
                case EnumClass.EnumAnonymous.MaleAdult:
                    objMRCreateDto.GENDER = "M";
                    objMRCreateDto.enumGender = EnumClass.EnumGender.Male;
                    break;
                case EnumClass.EnumAnonymous.TestingUse:
                    objMRCreateDto.GENDER = "M";
                    objMRCreateDto.enumGender = EnumClass.EnumGender.Male;
                    break;

            }


            if (objMRCreateDto != null)
            {
                using (MedicalRecordService service = new MedicalRecordService(_context))
                {
                    Msg = service.GenerateMedicalRecord(objMRCreateDto, EnumClass.DisplayLanguage.English, checklogin.EMPCODE,get_user_auth_cookie);
                }

            }
            else
            {
                Msg.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.WrongParam, EnumClass.DisplayLanguage.English);
            }


            return JsonConvert.SerializeObject(Msg);
        }

        public string UpdateMRData(string MRJsonData)
        {
            #region Variable Setting

            MREditReutrnMsg Msg = new MREditReutrnMsg();
            MRJSONStructure? objMRCreateDto = new MRJSONStructure();

            #endregion

            #region Login Session Check
            var get_user_auth_cookie = HttpContext.User.Claims.FirstOrDefault(t => t.Type == "User_auth_page_Name「" + "InpatientMedications" + "」")?.Value;

            var checklogin = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

            #endregion

            MRJSONStructure[]? objArray = JsonConvert.DeserializeObject(MRJsonData, typeof(MRJSONStructure[])) as MRJSONStructure[];

            objMRCreateDto = objArray.Any() ? objArray.FirstOrDefault() : null;

            foreach (EnumClass.EnumGender gender in Enum.GetValues(typeof(EnumClass.EnumGender)))
            {
                if (gender.EnumToCode().Trim() == objMRCreateDto.GENDER.Trim())
                {
                    objMRCreateDto.enumGender = gender;
                    break;
                }
            }

            foreach (EnumClass.EnumAnonymous anonymous in Enum.GetValues(typeof(EnumClass.EnumAnonymous)))
            {
                if (anonymous.EnumToCode().Trim() == objMRCreateDto.ANONYMOUSTYPE.Trim())
                {
                    objMRCreateDto.enumAnonymous = anonymous;
                    break;
                }
            }


            if (objMRCreateDto != null)
            {
                using (MedicalRecordService service = new MedicalRecordService(_context))
                {
                    Msg = service.UpdateMedicalRecord(objMRCreateDto, EnumClass.DisplayLanguage.English, checklogin.EMPCODE, get_user_auth_cookie);
                }
            }
            else
            {
                Msg.ReturnT = MessageFunction.GetFullReplyNoMsg(EnumClass.ReplyNoCode.WrongParam, EnumClass.DisplayLanguage.English);
            }


            return JsonConvert.SerializeObject(Msg);
        }

        public string GeneratePhoneCode(string NationalName, string NationalPhoneCode)
        {
            GeneratePhoneReutrnMsg Msg = new GeneratePhoneReutrnMsg();

            #region Login Session Check

            var checklogin = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

            #endregion

            using (MedicalRecordService service = new MedicalRecordService(_context))
            {
                Msg = service.AddNationalPhoneList(NationalName, NationalPhoneCode, EnumClass.DisplayLanguage.English, checklogin.EMPCODE);
            }

            return JsonConvert.SerializeObject(Msg);
        }

        public string RedirectPage(string patientID)
        {
            string url = "";

            url = Url.Action("MREdit", "MedRecord", new { PatientID = patientID });

            return url;
        }

        #endregion
    }
}
