using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Areas.MedicalRecord.ViewModels;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;

namespace KMU.HisOrder.MVC.Areas.MedicalRecord.Controllers
{
    [Area("MedicalRecord")]
    [Authorize(Roles = "MedicalRecord")]//登入後可依據設定的 專案名稱 project_id 作為判斷依據
    public class PrintController : Controller
    {
        private readonly KMUContext _context;

        public PrintController(KMUContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public PartialViewResult PrintPersonalCard(string patientID)
        {
            PrintClass PersonalPrintData = new PrintClass();

            #region Login Session Check

            var checklogin = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

            #endregion

            PersonalPrintData.HospName = _context.KmuCoderefs.Where(c => c.RefCodetype == "HospitalCode").Select(c => c.RefName).FirstOrDefault().ToString();
            var Dtos = _context.KmuCharts.Where(c => c.ChrHealthId == patientID);

            if (Dtos.Any())
            {
                PersonalCardClass person = new PersonalCardClass()
                {
                    PATIENTID = Dtos.FirstOrDefault().ChrHealthId,
                    FIRSTNAME = Dtos.FirstOrDefault().ChrPatientFirstname,
                    MIDNAME = Dtos.FirstOrDefault().ChrPatientMidname,
                    LASTNAME = Dtos.FirstOrDefault().ChrPatientLastname,
                    GENDER = Dtos.FirstOrDefault().ChrSex,
                    BIRTHDATE = Dtos.FirstOrDefault().ChrBirthDate,
                    enumGender = Dtos.FirstOrDefault().ChrSex == "M"? EnumClass.EnumGender.Male: EnumClass.EnumGender.Female
                };

                PersonalPrintData.PersonalInfo = person;
                PersonalPrintData.QrCodeStr = JsonConvert.SerializeObject(person);
                PersonalPrintData.ModifyID = checklogin.EMPCODE;
                PersonalPrintData.ModifyName = checklogin.EMPNAME;
            }

            ViewData["Title"] = "Personal Card";
            return PartialView("Areas/MedicalRecord/Views/Print/_PrintTemplate.cshtml", PersonalPrintData);
        }


    }
}
