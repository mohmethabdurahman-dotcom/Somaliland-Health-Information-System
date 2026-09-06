using KMU.HisOrder.MVC.Areas.MedicalRecord.ViewModels;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using static KMU.HisOrder.MVC.Models.EnumClass;

namespace KMU.HisOrder.MVC.Areas.MedicalRecord.Controllers
{
    [Area("MedicalRecord")]
    [Authorize(Roles = "MedicalRecord")]//登入後可依據設定的 專案名稱 project_id 作為判斷依據
    public class MedRecordController : Controller
    {
        private readonly KMUContext _context;

        public MedRecordController(KMUContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult GetAddress()
        {
            var addres = _context.KmuCoderefs.Where(a => a.RefCodetype == "Chart_Area").ToList();
            return Json(addres);
        }
        public IActionResult phonechecker(string? phone)
        {
            var lphone = "+252 063 " + phone;
            var findnumbers = _context.KmuCharts
                 
                      .Select(s => new {
                          s.ChrHealthId,
                          s.ChrPatientFirstname,
                          s.ChrPatientMidname,
                          s.ChrPatientLastname,
                          s.ChrMobilePhone,
                          s.ChrSex
                       
                      }).Where(s=> s.ChrMobilePhone == lphone);
         
            return Json(findnumbers.ToList());
        }
        public IActionResult AddMore()
        {
            return View();
        }
        [HttpPost]
        public IActionResult AddAddress(string address,KmuCoderef coderef)
        {
           
            coderef.RefName = address;
            if (coderef.RefName != null)
            {
                coderef.RefCodetype = "Chart_Area";
                coderef.RefCode = coderef.RefName;
                coderef.RefDes = "病歷主檔的地區Area_Code";
                coderef.RefId = "";
                coderef.RefCasetype = "Y";
                var refshowseq = _context.KmuCoderefs.Where(c => c.RefCodetype == "Chart_Area").Max(r => r.RefShowseq);
                coderef.RefShowseq = refshowseq + 1;

                _context.Add(coderef);
                _context.SaveChanges();
            }
            return Json(coderef);
            return RedirectToAction("MRCreate");
        }

        public IActionResult MRFind()
        {
            return View();
        }

        public IActionResult MRCreate()
        {
            #region Varibles setting

            MRCreateClass mRCreate = new MRCreateClass();
            List<EnumGender> genderList = new List<EnumGender>();
            List<KmuCoderef> areaList = new List<KmuCoderef>();
            List<KmuCoderef> relationList = new List<KmuCoderef>();
            List<KmuCoderef> NationPhoneList = new List<KmuCoderef>();

            List<EnumAnonymous> anonymousList = new List<EnumAnonymous>();
            KmuChart chartData = new KmuChart();
            #endregion


            foreach (EnumClass.EnumGender gender in Enum.GetValues(typeof(EnumGender)))
            {
                genderList.Add(gender);
            }

            foreach (EnumClass.EnumAnonymous anonymous in Enum.GetValues(typeof(EnumAnonymous)))
            {
                if (EnumClass.EnumAnonymous.Normal != anonymous)
                {
                    anonymousList.Add(anonymous);
                }
            }

            areaList = _context.KmuCoderefs.Where(c => c.RefCodetype == "Chart_Area" && c.RefCasetype == "Y").OrderBy(c => c.RefShowseq).ToList();
            relationList = _context.KmuCoderefs.Where(c => c.RefCodetype == "Chart_Relationship" && c.RefCasetype == "Y").OrderBy(c => c.RefShowseq).ToList();
            NationPhoneList = _context.KmuCoderefs.Where(c => c.RefCodetype == "NationalPhone" && c.RefCasetype == "Y").OrderBy(c => c.RefShowseq).ToList();

            mRCreate.GendetList = genderList;
            mRCreate.AreaList = areaList;
            mRCreate.RelationList = relationList;
            mRCreate.AnonymousList = anonymousList;
            mRCreate.NationPhoneList = NationPhoneList;

            ViewData["Action"] = EnumClass.ActionCode.Create;
            ViewData["mRCreate"] = mRCreate;
            ViewData["Title"] = "Medical Record → Create New Medical Record";
            return View("MRIndex", chartData);
        }

        public IActionResult MREdit(string PatientID)
        {
            #region Varibles setting

            MRCreateClass mRCreate = new MRCreateClass();
            List<EnumGender> genderList = new List<EnumGender>();
            List<KmuCoderef> areaList = new List<KmuCoderef>();
            List<KmuCoderef> relationList = new List<KmuCoderef>();
            List<KmuCoderef> NationPhoneList = new List<KmuCoderef>();

            List<EnumAnonymous> anonymousList = new List<EnumAnonymous>();

            KmuChart chartData = new KmuChart();
            #endregion


            foreach (EnumClass.EnumGender gender in Enum.GetValues(typeof(EnumGender)))
            {
                genderList.Add(gender);
            }

            foreach (EnumClass.EnumAnonymous anonymous in Enum.GetValues(typeof(EnumAnonymous)))
            {
                if (EnumClass.EnumAnonymous.Normal != anonymous)
                {
                    anonymousList.Add(anonymous);
                }
            }

            areaList = _context.KmuCoderefs.Where(c => c.RefCodetype == "Chart_Area" && c.RefCasetype == "Y").OrderBy(c => c.RefShowseq).ToList();
            relationList = _context.KmuCoderefs.Where(c => c.RefCodetype == "Chart_Relationship" && c.RefCasetype == "Y").OrderBy(c => c.RefShowseq).ToList();
            NationPhoneList = _context.KmuCoderefs.Where(c => c.RefCodetype == "NationalPhone" && c.RefCasetype == "Y").OrderBy(c => c.RefShowseq).ToList();

            mRCreate.GendetList = genderList;
            mRCreate.AreaList = areaList;
            mRCreate.RelationList = relationList;
            mRCreate.AnonymousList = anonymousList;
            mRCreate.NationPhoneList = NationPhoneList;

            var chartDtos = _context.KmuCharts.Where(c =>c.ChrHealthId == PatientID);

            if (chartDtos.Any())
            {
                chartData = chartDtos.First();
            }
            else
            {
                return RedirectToAction("MRFind");
            }

            ViewData["Action"] = EnumClass.ActionCode.Update;
            ViewData["mRCreate"] = mRCreate;
            ViewData["Title"] = "Medical Record → Edit Medical Record";

            return View("MRIndex", chartData);
        }
    }
}
