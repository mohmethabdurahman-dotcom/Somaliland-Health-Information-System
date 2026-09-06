using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Areas.Maintenance.Models;
using KMU.HisOrder.MVC.Areas.Maintenance.ViewModels;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Security.AccessControl;
using System.Security.Policy;
using System.Web;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.Hosting;
using System.Globalization;
using Microsoft.AspNetCore.Cors;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace KMU.HisOrder.MVC.Areas.HisOrder.Controllers
{
    public class CheckClinicSessionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var context = filterContext.HttpContext;
            if (context.Session.GetObject<ClinicDTO>("ClinicDTO") == null)
            {
                var request = context.Request;
                filterContext.Result = new RedirectResult("~/Login/Index");
                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }



    public class CheckSessionTimeOutAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var context = filterContext.HttpContext;
            var login = LoginSessionHelper.GetOrRestore(context);
            if (login == null)
            {
                if (LoginSessionHelper.IsAjax(context.Request))
                {
                    filterContext.Result = new StatusCodeResult(StatusCodes.Status401Unauthorized);
                    return;
                }
                filterContext.Result = new RedirectResult("~/Login/NotLogin");
                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }
    [CheckSessionTimeOutAttribute]
    [Area("HisOrder")]
    public class HisOrderController : Controller
    {
        private readonly KMUContext _context;

        public HisOrderController(KMUContext context)
        {
            _context = context;
        }

        #region View Function



        public IActionResult Index(string sourceType ,string? wardId)
        {
            if (sourceType == "OPD")
            {
                return Enter_opd_clinic();
            }
            else if(sourceType == "EMG")
            {
                return Enter_emg_clinc();
            }
            else
            {
                return Enter_ward_clinc(wardId);
            }

        }


        public IActionResult Enter_opd_clinic()
        {

            //var request = HttpContext.Request;
            //var checklogin = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
            //if (checklogin == null || string.IsNullOrWhiteSpace(checklogin.EMPCODE))
            //{
            //    return Redirect(request.Host + "/Login/Index");
            //}

            var gv = new GlobalVariableDTO()
            {
                Clinic = HttpContext.Session.GetObject<ClinicDTO>("ClinicDTO"),
                Login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO")
            };

            if (gv.Clinic != null && gv.Clinic.InhospType != "OPD")
            {
                gv.Clinic = null;
                HttpContext.Session.SetObject("ClinicDTO", null);
            }


            if (gv.Clinic == null)
            {
                var getDefaultSchDept = _context.ClinicSchedules
                    .Where(c => c.ScheDoctor == gv.Login.EMPCODE
                    && c.ScheWeek == DateTime.Today.DayOfWeek.ToString()
                    && c.ScheNoon == "AM"
                    && c.ScheDptCode.Substring(0, 2) != "16").FirstOrDefault();
                if (getDefaultSchDept != null)
                {

                    IniatializeSessionClinicDto(
                            DateTime.Today,
                           getDefaultSchDept.ScheDptCode,
                            getDefaultSchDept.ScheNoon,
                            gv.Login.EMPCODE,
                            gv.Login.EMPCODE
                            );
                    gv.Clinic = HttpContext.Session.GetObject<ClinicDTO>("ClinicDTO");
                }
            }
            if (gv.Clinic == null)
            {
                ViewBag.DisPlaySwithClinicModal = true;
                ViewBag.DisPlayDefaultRegDate = DateTime.Today;
            }
            else
            {
                ViewBag.DisPlaySwithClinicModal = false;
                ViewBag.DisPlayDefaultClinic = gv.Clinic;
                ViewBag.DisPlayDefaultRegDate = gv.Clinic.RegDate;
            }


            var patientData = InitializePatientList();
            ViewBag.SourceType = "OPD";
            ViewBag.clinicScheList = GetClinicScheList(
               gv.Login.EMPCODE,
               gv.Clinic == null ? DateTime.Today : gv.Clinic.RegDate,
               "OPD"
               );
            ViewBag.loginDTO = gv.Login;
            return View("Index", patientData);
        }


        public IActionResult Enter_emg_clinc()
        {
            var checklogin = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
            if (checklogin == null || string.IsNullOrWhiteSpace(checklogin.EMPCODE))
            {
                return RedirectToAction("Login/Index");
            }

            var gv = new GlobalVariableDTO()
            {
                Clinic = HttpContext.Session.GetObject<ClinicDTO>("ClinicDTO"),
                Login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO")
            };


            if (gv.Clinic != null && gv.Clinic.InhospType != "EMG")
            {
                gv.Clinic = null;
                HttpContext.Session.SetObject("ClinicDTO", null);
            }

            if (gv.Clinic == null)
            {
                IniatializeSessionClinicDto(
                        gv.Clinic == null ? null : gv.Clinic.RegDate,
                        gv.Clinic == null ? null : gv.Clinic.DeptCode,
                        "A",
                        gv.Login.EMPCODE,
                        gv.Login.EMPCODE
                        );
                gv.Clinic = HttpContext.Session.GetObject<ClinicDTO>("ClinicDTO");
            }
            if (gv.Clinic == null)
            {
                ViewBag.DisPlaySwithClinicModal = true;
                ViewBag.DisPlayDefaultRegDate = DateTime.Today;
            }
            else
            {
                ViewBag.DisPlaySwithClinicModal = false;
                ViewBag.DisPlayDefaultClinic = gv.Clinic;
                ViewBag.DisPlayDefaultRegDate = gv.Clinic.RegDate;
            }

            var patientData = InitializePatientList();
            ViewBag.SourceType = "EMG";
            ViewBag.clinicScheList = GetClinicScheList(gv.Login.EMPCODE, gv.Clinic == null ? DateTime.Today : gv.Clinic.RegDate, "EMG");
            ViewBag.loginDTO = gv.Login;
            return View("Index", patientData);

        }

        public IActionResult Enter_ward_clinc(string id)
        {
            var ward = _context.Wards.Where(w => w.wardid == id).FirstOrDefault();
            var checklogin = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
            if (checklogin == null || string.IsNullOrWhiteSpace(checklogin.EMPCODE))
            {
                return RedirectToAction("Login/Index");
            }

            // get all the patient ids from inpatient reservation
            var reservations = _context.InpatientReservations.Where(w => w.wardId == id && w.status == "admitted").Select(p => p.healthId).ToList();

            //get All Patients in the Ward Count()
            var patients = _context.KmuCharts.Where(r => reservations.Contains(r.ChrHealthId)).Count();

            //get All Beds in the Ward Count()
            var beds = _context.beds.Where(b => b.wardId == id).Count();

            //get all the Available Beds in the Ward Count()
            var availableBeds = _context.beds.Where(b => b.wardId == id && b.status == "Available").Count();

            //get all the Occupied Beds in the Ward Count()
            var occupiedBeds = _context.beds.Where(b => b.wardId == id && b.status == "Occupied").Count();
            var gv = new GlobalVariableDTO()
            {
                Clinic = HttpContext.Session.GetObject<ClinicDTO>("ClinicDTO"),
                Login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO")
            };

            if (gv.Clinic != null && gv.Clinic.InhospType != "WARD")
            {
                gv.Clinic = null;
                HttpContext.Session.SetObject("ClinicDTO", null);
            }

            HttpContext.Session.SetObject("ClinicDTO", new ClinicDTO()
            {
                WardId = ward.wardid,
                DeptCode = ward.department,
                DeptName = ward.wardName,
                RegDate = DateTime.Now,
                InhospType = "WARD"
            });
            var ptData = from d in _context.Set<InpatientReservation>()
                         join chart in _context.Set<KmuChart>()
                         on d.healthId equals chart.ChrHealthId
                         join bed in _context.Set<Bed>() on d.bedId equals bed.bedId
                         where d.wardId == id && d.status == "admitted"
                         select new PatientDTO
                         {
                             Inhospid = d.inhospId,
                             RegPatientId = d.healthId,
                             RegStatus = d.status,
                             NationalId = chart.ChrNationalId,
                             FirstName = chart.ChrPatientFirstname,
                             MidName = chart.ChrPatientMidname,
                             LastName = chart.ChrPatientLastname,
                             Sex = chart.ChrSex,
                             MobilePhone = chart.ChrMobilePhone,
                             BirthDate = chart.ChrBirthDate == null ? null : chart.ChrBirthDate.Value.ToDateTime(TimeOnly.Parse("00:00 AM")),
                             Age = chart.ChrBirthDate == null ? -1 : DateTime.Now.Year - chart.ChrBirthDate.Value.Year,
                             RegDate = d.reserveDate,
                             RegDept = d.department,
                             remark = chart.ChrRemark,
                             bed = bed.bedName,
                             canVisit = true
                         };

            var AllBeds = _context.beds.Where(b=> b.wardId == id).Count();
            var OccupiedBeds = _context.beds.Where(b=> b.wardId == id && b.status == "Occupied").Count();
            var AvailableBeds = _context.beds.Where(b=> b.wardId == id && b.status == "Available").Count();


            ViewBag.AllBeds = AllBeds;
            ViewBag.AvailableBeds = AvailableBeds;
            ViewBag.OccupiedBeds = OccupiedBeds;
            var list = ptData.ToList();
            ViewBag.AllPatients = patients;
         

            return View("Index", list);
        }



        [HttpPost]
        public ActionResult SwitchClinic(string clinicDate, string clinicDeptCode, string clinicRoomNo, string clinicDoctorId, string loginId, string sourceType)
        {

            //    DateTime.Parse(clinicInfo["clinic-date"].Trim()),
            //    clinicInfo["clinic-dept-code"].Trim(),
            //    clinicInfo["clinic-noon-no"].Trim(),
            //    clinicInfo["clinic-doctor-id"].Trim(),
            //    clinicInfo["login-id"].Trim(),
            //    clinicInfo["clinic-doctor-id"].Trim()

            var checklogin = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
            if (checklogin == null || string.IsNullOrWhiteSpace(checklogin.EMPCODE))
            {
                return RedirectToAction("Login/Index");
            }

            IniatializeSessionClinicDto(
                    DateTime.ParseExact(clinicDate, "dd/MM/yyyy", CultureInfo.InvariantCulture),
                    clinicDeptCode,
                    "AM",
                    checklogin.EMPCODE,
                    checklogin.EMPCODE
                    );

            //var _clinicDTO = HttpContext.Session.GetObject<ClinicDTO>("ClinicDTO");

            //if (_clinicDTO == null)
            //{
            //    ViewBag.DisPlaySwithClinicModal = true;
            //    ViewBag.DisPlayDefaultRegDate = DateTime.Today;
            //}
            //else
            //{
            //    ViewBag.DisPlaySwithClinicModal = false;
            //    ViewBag.DisPlayDefaultClinic = _clinicDTO;
            //    ViewBag.DisPlayDefaultRegDate = _clinicDTO.RegDate;
            //}

            //var patientData = GetPatientList(DateTime.ParseExact(clinicDate, "dd/MM/yyyy", CultureInfo.InvariantCulture), clinicDeptCode);
            //ViewBag.SourceType = sourceType;
            //ViewBag.clinicScheList = GetClinicScheList(checklogin.EMPCODE, _clinicDTO == null ? DateTime.Today : _clinicDTO.RegDate, sourceType);
            //ViewBag.loginDTO = checklogin;
            //return View("Index", patientData);

            //2023.08.22 Html.BeginForm ERR_CACHE_MISS  update by 1050325 
            if (sourceType == "OPD")
            {
                return RedirectToAction("Enter_opd_clinic");
            }
            else
            {
                return RedirectToAction("Enter_emg_clinc");
            }
        }


        /// <summary>
        /// ClinicOrder畫面(SOAP、藥品、檢驗、檢查...)
        /// </summary>
        /// <returns></returns>
        /// 
        [CheckClinicSessionAttribute]
        public ActionResult ClinicOrder()
        {
            //viewbag assign
            ViewBag.htmlbody = HttpContext.Session.GetString("htmlBody");
            //ViewBag.htmlbodyActive =  HttpContext.Session.GetString("htmlbodyActive");
            return View();
        }



        private bool CheckPatientVisitBool(DateOnly inRegDate)
        {
            try
            {
                // pass visit
                var passData = _context.KmuCoderefs.Where(c => c.RefCodetype == "PassClinicVisit").FirstOrDefault();
                if (passData != null && passData.RefCasetype == "Y")
                {
                    return true;
                }

                if (inRegDate <= DateOnly.FromDateTime(DateTime.Today))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        [HttpPost]
        public string CheckPatientVisit(string patientInhospid, string patientPatientid)
        {
            try
            {
                var today = DateTime.Today;
                var result = new ResultDTO() { isSuccess = false };
                if (patientInhospid.Contains("IN"))
                {
                    result.isSuccess = true;
                    return JsonConvert.SerializeObject(result);
                }

                var regData = _context.Registrations.Where(c => c.Inhospid == patientInhospid && c.RegHealthId == patientPatientid).FirstOrDefault();
                var clinicCategory = _context.KmuDepartments.Where(d => d.DptCode == regData.RegDepartment).FirstOrDefault().DptCategory;

                //checking the Physical Sign
                var vital_sign = _context.PhysicalSigns.Where(p => p.Inhospid == patientInhospid).Any();

                if (regData != null)
                {
                    if (vital_sign == false && regData.RegDepartment == "1604")
                    {
                        result.isSuccess = false;
                        result.Message = "This patient has not vital sign";
                        return JsonConvert.SerializeObject(result);
                    }

                    if (CheckPatientVisitBool(regData.RegDate) || clinicCategory == "EMG")
                    {
                        result.isSuccess = true;
                        return JsonConvert.SerializeObject(result);
                    }
                    else
                    {
                        result.isSuccess = false;
                        result.Message = "The patient of this visit is locked";
                        return JsonConvert.SerializeObject(result);
                    }
                }
                else
                {
                    result.isSuccess = false;
                    result.Message = "No such patient id found";
                    return JsonConvert.SerializeObject(result);
                }
            }
            catch
            {
                return JsonConvert.SerializeObject(new ResultDTO() { isSuccess = false, Message = "An exception occurred ( CheckPatientVisit )" });
            }
        }


        [HttpPost]
        public ActionResult PatientVisit(string patientInhospid, string patientPatientid, string patientVisitStatus, string htmlBody, string htmlBodyActive)
        {

            if (string.IsNullOrWhiteSpace(patientInhospid) || string.IsNullOrWhiteSpace(patientPatientid))
            {
                return RedirectToAction("Index");
            }
            else
            {
                //取得 patient session data
                var patientInfo = GetPatientInfo(patientInhospid, patientPatientid);

                IniatializeSessionPatientDto(patientInfo);
               

                //取得 hisorderplan
                var hplanList = GetHisOrderPlanList(patientInhospid, patientPatientid);

                if (hplanList != null)
                {
                    ViewBag.MedList = hplanList.Where(c => c.HplanType == "Med" && c.DcDate == null).OrderBy(c => c.SeqNo).ToList();
                    ViewBag.NonMedList = hplanList.Where(c => (c.HplanType != "Med" && c.HplanType != "ICD") && c.DcDate == null).OrderBy(c => c.SeqNo).ToList();
                }

                //取得 soap
                List<hisordersoa_version> soapVersion = null;
                using (SoapController _soapController = new SoapController(_context, null))
                {

                    //var regdata = _context.Registrations.Where(c => c.Inhospid == patientInhospid).FirstOrDefault();
                    //var souretype = (regdata != null && regdata.RegDepartment.Substring(0, 2) != "16") ? "OPD" : "EMG";

                    soapVersion = _soapController.querySoapVer(patientInhospid);
                    ViewBag.SoapVersion = soapVersion;
                    //2023.02.17 add by 1050325 MG_INFO
                    ViewBag.MG_INFO = _soapController.getMgInfo();
                }

                //取得藥品相關設定
                ViewBag.MedFreq = _context.KmuMedfrequencies.ToList();
                ViewBag.MedIndication = _context.KmuMedfrequencyInds.ToList();
                ViewBag.MedPathWay = _context.KmuMedpathways.ToList();
                ViewBag.MedItem = JsonConvert.SerializeObject(_context.KmuMedicines.ToList());
                ViewBag.Discharge = _context.KmuCoderefs.Where(c=> c.RefCodetype == "discharge_type").OrderBy(o=>o.RefShowseq).ToList();
                ViewBag.Departments = _context.KmuDepartments.Where(g => g.DptParent == "").ToList();
                //取得非藥相關
                if (_context.KmuCoderefs.Where(c => c.RefCodetype == "NonMedLocation").Count() > 0)
                {
                    ViewBag.NonMedLocation = _context.KmuCoderefs.Where(c => c.RefCodetype == "NonMedLocation").OrderBy(d => d.RefShowseq).ToList();
                }
                else
                {
                    ViewBag.NonMedLocation = new List<KmuCoderef>();
                }

                //取得 diagnosis 
                //to do...
                //取得 allergy
                //to do...
                //畫面左側bar設定
                HttpContext.Session.SetString("htmlBody", htmlBody);
                //取得歷史病歷頭檔
                ViewBag.HistoryRecordMaster = getHistoryRecordMaster(patientInfo.RegDate.Value.AddMonths(-96), patientInfo.RegDate, patientInfo.RegPatientId);
                //寫入看診時間
                modifyRegStartEndTime(patientInfo.Inhospid, "reg_start", "");
                //開啟看診畫面
                //return RedirectToAction("ClinicOrder");
                return View("ClinicOrder", patientInfo);
            }

        }

        #endregion

        #region init Function

        private void IniatializeSessionClinicDto(DateTime? regDate, string deptCode, string noonNo, string doctorCode, string loginCode, string authdoctorCode = null)
        {

            DateTime _regDate = regDate == null ? DateTime.Now : (DateTime)regDate;

            if (string.IsNullOrWhiteSpace(deptCode))
            {
                HttpContext.Session.SetObject("ClinicDTO", null);
                return;
            }
            else
            {
                var clinicInfo = GetDefaultClinicInfo(_regDate, deptCode, "AM", doctorCode, loginCode, null);
                var deptInfo = _context.KmuDepartments.Where(c => c.DptCode == deptCode).FirstOrDefault();


                if (clinicInfo != null)
                {
                    HttpContext.Session.SetObject("ClinicDTO", new ClinicDTO()
                    {
                        DeptCode = clinicInfo.ScheDptCode,
                        DeptName = clinicInfo.ScheDptName,
                        RegDate = _regDate,
                        DoctorCode = clinicInfo.ScheDoctor,
                        DoctorName = clinicInfo.ScheDoctorName,
                        NoonNO = noonNo,
                        RoomNumber = clinicInfo.ScheRoom,
                        InhospType = deptInfo.DptCategory
                    }); ;
                }
            }

        }


        public void IniatializeSessionPatientDto(PatientDTO inPatientInfo)
        {
            HttpContext.Session.SetObject("PatientDTO", null);
            HttpContext.Session.SetObject("PatientDTO", inPatientInfo);
        }

        public void IniatializeSessionPatientDto(HttpContext _context, PatientDTO inPatientInfo)
        {
            _context.Session.SetObject("PatientDTO", null);
            _context.Session.SetObject("PatientDTO", inPatientInfo);
        }

        private List<PatientDTO> InitializePatientList()
        {
            var gv = new GlobalVariableDTO()
            {
                Clinic = HttpContext.Session.GetObject<ClinicDTO>("ClinicDTO"),
                Patient = HttpContext.Session.GetObject<PatientDTO>("PatientDTO"),
                Login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO")
            };


            if (gv.Login == null)
            {
                throw new Exception("看診資訊或登入者資訊有錯! 可能發呆太久 Sesssion Timeout...");
            }

            if (gv.Clinic == null)
            {
                return new List<PatientDTO>();
            }
            else
            {
                return GetPatientList(gv.Clinic.RegDate, gv.Clinic.DeptCode);
            }
        }



        #endregion

        #region get data Function
        /// <summary>
        /// 取得病人清單
        /// </summary>
        /// <param name="inRegDate"></param>
        /// <param name="inDeptCode"></param>
        /// <returns></returns>
        public List<PatientDTO> GetPatientList(DateTime inRegDate, string inDeptCode)
        {
            //Console.WriteLine(inDeptCode);

            var shift = "";

            var ShiftA = _context.KmuCoderefs.SingleOrDefault(sh => sh.RefCodetype == "Shift" && sh.RefCode == "Shift A").RefDes;
            var ShiftB = _context.KmuCoderefs.SingleOrDefault(sh => sh.RefCodetype == "Shift" && sh.RefCode == "Shift B").RefDes;
            var ShiftC = _context.KmuCoderefs.SingleOrDefault(sh => sh.RefCodetype == "Shift" && sh.RefCode == "Shift C").RefDes;

            //string ShiftA = "07:30:00";
            //string ShiftB = "13:30:00";
            //string ShiftC = "19:30:00";

            TimeSpan duration = DateTime.Now.TimeOfDay;


            if (duration > TimeSpan.Parse(ShiftA) && duration < TimeSpan.Parse(ShiftB))
            {
                shift = "Shift A";
            }
            else if (duration > TimeSpan.Parse(ShiftB) && duration < TimeSpan.Parse(ShiftC))
            {
                shift = "Shift B";
            }
            else
            {
                shift = "Shift C";
            }

            var ptData = from d in _context.Set<Registration>()
                         join chart in _context.Set<KmuChart>()
                         on d.RegHealthId equals chart.ChrHealthId
                         where d.RegDate == DateOnly.FromDateTime(inRegDate) && (d.shift == shift || d.shift == "All Time") && d.RegDepartment == inDeptCode && d.RegStatus != "C"
                         select new PatientDTO
                         {
                             Inhospid = d.Inhospid,
                             RegPatientId = d.RegHealthId,
                             RegSeqNo = d.RegSeqNo,
                             RegStatus = d.RegStatus,
                             NationalId = chart.ChrNationalId,
                             FirstName = chart.ChrPatientFirstname,
                             MidName = chart.ChrPatientMidname,
                             LastName = chart.ChrPatientLastname,
                             Sex = chart.ChrSex,
                             MobilePhone = chart.ChrMobilePhone,
                             BirthDate = chart.ChrBirthDate == null ? null : chart.ChrBirthDate.Value.ToDateTime(TimeOnly.Parse("00:00 AM")),
                             Age = chart.ChrBirthDate == null ? -1 : DateTime.Now.Year - chart.ChrBirthDate.Value.Year,
                             RegDate = d.RegDate.ToDateTime(TimeOnly.Parse("00:00 AM")),
                             RegDept = d.RegDepartment,
                             RegNoon = d.RegNoon,
                             transfer_code = d.RegFollowCode,
                             transfer_des = d.RegFollowDesc,
                             remark = chart.ChrRemark


                         };

            if (ptData.Any())
            {
                var list = ptData.ToList();
                var clinicCategory = _context.KmuDepartments.Where(c => c.DptCode == inDeptCode).FirstOrDefault().DptCategory;

                list.ForEach(c =>
                {
                    if (clinicCategory == "EMG")
                    {
                        c.canVisit = true;
                    }
                    else if (c.RegDate != null)
                    {
                        c.canVisit = CheckPatientVisitBool(DateOnly.FromDateTime(Convert.ToDateTime(c.RegDate)));
                    }
                    else
                    {
                        c.canVisit = false;
                    }
                });


                return list;
            }
            else
            {
                return new List<PatientDTO>();
            }
        }
        /// <summary>
        /// 取得診間單一病人資料
        /// </summary>
        /// <param name="inhospid"></param>
        /// <param name="inPatientid"></param>
        /// <returns></returns>
        public PatientDTO GetPatientInfo(string inhospid, string inPatientid)
        {
            if (string.IsNullOrWhiteSpace(inhospid) || string.IsNullOrWhiteSpace(inPatientid))
            {
                return null;
            }
            else
            {
                IQueryable<PatientDTO> ptData;

                if (!inhospid.StartsWith("IN"))
                {
                    ptData = from d in _context.Set<Registration>()
                             join chart in _context.Set<KmuChart>() on d.RegHealthId equals chart.ChrHealthId
                             where d.Inhospid == inhospid && d.RegHealthId == inPatientid
                             select new PatientDTO
                             {
                                 Inhospid = d.Inhospid,
                                 RegPatientId = d.RegHealthId,
                                 RegSeqNo = d.RegSeqNo,
                                 RegStatus = d.RegStatus,
                                 NationalId = chart.ChrNationalId,
                                 FirstName = chart.ChrPatientFirstname,
                                 MidName = chart.ChrPatientMidname,
                                 LastName = chart.ChrPatientLastname,
                                 Sex = chart.ChrSex,
                                 MobilePhone = chart.ChrMobilePhone,
                                 BirthDate = chart.ChrBirthDate == null ? null : chart.ChrBirthDate.Value.ToDateTime(TimeOnly.Parse("00:00 AM")),
                                 Age = chart.ChrBirthDate == null ? -1 : DateTime.Now.Year - chart.ChrBirthDate.Value.Year,
                                 Address = chart.ChrAddress,
                                 RegDate = d.RegDate.ToDateTime(TimeOnly.Parse("00:00 AM")),
                                 RegDept = d.RegDepartment,
                                 RegNoon = d.RegNoon,
                                 transfer_code = d.RegFollowCode,
                                 transfer_des = d.RegFollowDesc,
                                 remark = chart.ChrRemark
                             };
                }
                else
                {
                    ptData = from d in _context.Set<InpatientReservation>()
                             join chart in _context.Set<KmuChart>() on d.healthId equals chart.ChrHealthId
                             join bed in _context.Set<Bed>() on d.bedId equals bed.bedId
                             where d.inhospId == inhospid && d.healthId == inPatientid
                             select new PatientDTO
                             {
                                 Inhospid = d.inhospId,
                                 RegPatientId = d.healthId,
                                 RegStatus = d.status,
                                 NationalId = chart.ChrNationalId,
                                 FirstName = chart.ChrPatientFirstname,
                                 MidName = chart.ChrPatientMidname,
                                 LastName = chart.ChrPatientLastname,
                                 Sex = chart.ChrSex,
                                 MobilePhone = chart.ChrMobilePhone,
                                 BirthDate = chart.ChrBirthDate == null ? null : chart.ChrBirthDate.Value.ToDateTime(TimeOnly.Parse("00:00 AM")),
                                 Age = chart.ChrBirthDate == null ? -1 : DateTime.Now.Year - chart.ChrBirthDate.Value.Year,
                                 RegDate = d.reserveDate,
                                 RegDept = d.department,
                                 remark = chart.ChrRemark,
                                 bed = bed.bedName,
                                 canVisit = true
                             };
                }





                if (ptData.Any())
                {
                    var list = ptData.ToList();

                    list.ForEach(c =>
                    {
                        var clinicCategory = _context.KmuDepartments.Where(d => d.DptCode == c.RegDept).FirstOrDefault().DptCategory;
                        if (clinicCategory == "EMG")
                        {
                            c.canVisit = true;
                        }
                        else
                        if (c.RegDate != null)
                        {
                            c.canVisit = CheckPatientVisitBool(DateOnly.FromDateTime(Convert.ToDateTime(c.RegDate)));
                        } else if(inhospid.StartsWith("IN"))
                        {
                            c.canVisit = true;
                        }
                        else
                        {
                            c.canVisit = false;
                        }
                    });

                    return list.FirstOrDefault();
                }
                else
                {
                    return null;
                }
            }
        }

        public PatientDTO GetPatientInfo(string inPatientid)
        {
            if (string.IsNullOrWhiteSpace(inPatientid))
            {
                return null;
            }
            else
            {
                var ptData = from d in _context.Set<Registration>()
                             join chart in _context.Set<KmuChart>()
                             on d.RegHealthId equals chart.ChrHealthId
                             where d.RegHealthId == inPatientid

                             select new PatientDTO
                             {
                                 Inhospid = d.Inhospid,
                                 RegPatientId = d.RegHealthId,
                                 RegSeqNo = d.RegSeqNo,
                                 RegStatus = d.RegStatus,
                                 NationalId = chart.ChrNationalId,
                                 FirstName = chart.ChrPatientFirstname,
                                 MidName = chart.ChrPatientMidname,
                                 LastName = chart.ChrPatientLastname,
                                 Sex = chart.ChrSex,
                                 MobilePhone = chart.ChrMobilePhone,
                                 BirthDate = chart.ChrBirthDate == null ? null : chart.ChrBirthDate.Value.ToDateTime(TimeOnly.Parse("00:00 AM")),
                                 Age = chart.ChrBirthDate == null ? -1 : DateTime.Now.Year - chart.ChrBirthDate.Value.Year,
                                 Address = chart.ChrAddress,
                                 RegDate = d.RegDate.ToDateTime(TimeOnly.Parse("00:00 AM")),
                                 RegDept = d.RegDepartment,
                                 RegNoon = d.RegNoon,
                                 transfer_code = d.RegFollowCode,
                                 transfer_des = d.RegFollowDesc,
                                 remark = chart.ChrRemark

                             };

                if (ptData.Any())
                {
                    var list = ptData.ToList();

                    list.ForEach(c =>
                    {

                        if (c.RegDate != null)
                        {
                            c.canVisit = CheckPatientVisitBool(DateOnly.FromDateTime(Convert.ToDateTime(c.RegDate)));
                        }
                        else
                        {
                            c.canVisit = false;
                        }
                    });

                    return list.OrderBy(c => c.RegDate).LastOrDefault();
                }
                else
                {
                    return null;
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckPatientLabs(string patientId)
        {
            var hasLabOrders = _context.Hisorderplans
          .Any(plan => plan.HealthId == patientId &&
                       (plan.HplanType == "Lab" || plan.HplanType == "Path" || plan.HplanType == "Exam"));

            return Json(new { hasLabOrders });
        }
        private ClinicSchedule GetDefaultClinicInfo(System.DateTime inREGISTER_DATE, string inDeptCode, string inNOON_NO, string inDoctorCode, string inEmpcode, string inAuthDoctorCode)
        {
            var scheWeek = (int)inREGISTER_DATE.DayOfWeek;
            var clinicData = _context.ClinicSchedules.Where(c => c.ScheWeek == scheWeek.ToString() && c.ScheDptCode == inDeptCode).FirstOrDefault();

            return clinicData;

        }


        private List<ClinicScheduleItem> GetClinicScheList(string inDoctorCode, DateTime inRegDate, string inSourceType)
        {
            if (string.IsNullOrWhiteSpace(inDoctorCode))
            {
                throw new Exception();
            }
            else
            {
                //var scheData = new List<ClinicSchedule>();
                var scheWeek = (int)inRegDate.DayOfWeek;

                //var data_opd = _context.ClinicSchedules.Where(c => c.ScheDoctor == inDoctorCode && c.ScheWeek == scheWeek.ToString() && c.ScheNoon == "AM");
                //var data_er = _context.ClinicSchedules.Where(c => c.ScheWeek == scheWeek.ToString() && c.ScheDptCode.Substring(0, 2) == "16");

                using (ClinicScheduleService service = new ClinicScheduleService(_context))
                {
                    var data = service.GetScheduleDataForHisOrder(new string[] { inSourceType }, DateOnly.FromDateTime(inRegDate), inDoctorCode);


                    if (data.Any())
                    {
                        return data;
                    }
                    else
                    {
                        return new List<ClinicScheduleItem>();
                    }
                }

            }
        }



        private List<ClinicScheduleItem> GetClinicScheList(string inDoctorCode, DateTime inRegDate)
        {
            if (string.IsNullOrWhiteSpace(inDoctorCode))
            {
                throw new Exception();
            }
            else
            {
                //var scheData = new List<ClinicSchedule>();
                var scheWeek = (int)inRegDate.DayOfWeek;

                var data_opd = _context.ClinicSchedules.Where(c => c.ScheDoctor == inDoctorCode && c.ScheWeek == scheWeek.ToString() && c.ScheNoon == "AM");
                var data_er = _context.ClinicSchedules.Where(c => c.ScheWeek == scheWeek.ToString() && c.ScheDptCode.Substring(0, 2) == "16");

                using (ClinicScheduleService service = new ClinicScheduleService(_context))
                {
                    var data = service.GetScheduleDataForHisOrder(new string[] { "OPD", "EMG" }, DateOnly.FromDateTime(inRegDate), inDoctorCode);

                    if (data.Any())
                    {
                        return data;
                    }
                    else
                    {
                        return new List<ClinicScheduleItem>();
                    }
                }

            }
        }


        private List<Hisorderplan> GetHisOrderPlanList(string inhospid, string inPatientid)
        {
            if (string.IsNullOrWhiteSpace(inhospid) || string.IsNullOrWhiteSpace(inPatientid))
            {
                return null;
            }
            else
            {
                var hplan = _context.Hisorderplans.Where(c => c.Inhospid == inhospid && c.HealthId == inPatientid && c.DcStatus != '2').OrderBy(d => d.SeqNo);
                if (hplan.Any())
                {
                    return hplan.ToList();
                }
                else
                {
                    return null;
                }
            }

        }


        #endregion

        #region getGlobalVariableDTO
        public GlobalVariableDTO getGlobalVariablDTO(HttpContext httpContext)
        {
            var grv = new GlobalVariableDTO();

            if (httpContext.Session.GetObject<ClinicDTO>("ClinicDTO") != null)
            {
                grv.Clinic = httpContext.Session.GetObject<ClinicDTO>("ClinicDTO");
            }

            if (httpContext.Session.GetObject<LoginDTO>("LoginDTO") != null)
            {
                grv.Login = httpContext.Session.GetObject<LoginDTO>("LoginDTO");
            }


            if (httpContext.Session.GetObject<PatientDTO>("PatientDTO") != null)
            {
                grv.Patient = httpContext.Session.GetObject<PatientDTO>("PatientDTO");
            }

            return grv;

        }
        #endregion

        [HttpGet]
        public IActionResult GetDepartmentsWithWards()
        {
            try
            {
                var departmentsWithWards = _context.Wards
                    .Select(w => w.department)
                    .Distinct()
                    .Join(
                        _context.KmuDepartments,
                        wardDept => wardDept,
                        dept => dept.DptCode,
                        (wardDept, dept) => new
                        {
                            id = dept.DptCode,
                            name = dept.DptName
                        })
                    .ToList();
                return Json(departmentsWithWards);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error loading departments");
            }
        }
        [HttpGet]
        public IActionResult GetWardsByDepartment(string departmentCode)
        {
            try
            {
                var wards = _context.Wards
                    .Where(w => w.department == departmentCode)
                    .Select(w => new
                    {
                        id = w.wardid,
                        name = w.wardName
                    })
                    .ToList();

                return Json(wards);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error loading wards");
            }
        }

        [HttpPost]
        public IActionResult SaveDepartmentWard(string departmentCode, string wardId, string patientPatientid, string inhospitalId)
        {
            try
            {
                var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

                

                bool isAdmitted = _context.InpatientReservations
                .Any(p => p.healthId == patientPatientid && p.status == "Admitted");

                if (isAdmitted)
                    return Json(new
                    {
                        success = false,
                        message = "Patient may have already been admitted",
                    });


                var reservation = new InpatientReservation
                {
                    createBy = login.EMPCODE,
                    createAt = DateTime.Now,
                    reserveDate = DateTime.Now,
                    status = "transfer",
                    inhospId = "",
                    healthId = patientPatientid,
                    department = departmentCode,
                    wardId = wardId
                };
                _context.InpatientReservations.Add(reservation);
                _context.SaveChanges();

                var departmentName = _context.KmuDepartments
                    .Where(d => d.DptCode == departmentCode)
                    .Select(d => d.DptName)
                    .FirstOrDefault();

                var wardName = _context.Wards
                    .Where(w => w.wardid == wardId)
                    .Select(w => w.wardName)
                    .FirstOrDefault();

                return Json(new
                {
                    success = true,
                    message = "Saved successfully",
                    departmentName,
                    wardName
                });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #region History Record 相關

        public List<HistoryRecordDto> getHistoryRecordMaster(DateTime? inBeginDate, DateTime? inEndDate, string inHealthId)
        {
            try
            {
                DateOnly _inBeginDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-96));
                DateOnly _inEndDate = DateOnly.FromDateTime(DateTime.Today);

         

                if (inBeginDate is not null)
                {
                    _inBeginDate = DateOnly.FromDateTime(Convert.ToDateTime(inBeginDate));
                }

                if (inEndDate is not null)
                {
                    _inEndDate = DateOnly.FromDateTime(Convert.ToDateTime(inEndDate));
                }
                //Console.WriteLine(_inBeginDate);
                //Console.WriteLine(_inEndDate);
                //var historyData = _context.Registrations.Where(c => c.RegPatientId == inChartNo && (c.RegDate >= _inBeginDate && c.RegDate <= _inEndDate));

                //2022.04.11 update by 1050325 排除取消掛號的病人
                var historyData = from reg in _context.Set<Registration>()
                                  join dept in _context.Set<KmuDepartment>()
                                  on reg.RegDepartment equals dept.DptCode into DptCodeGroup
                                  from D in DptCodeGroup.DefaultIfEmpty()
                                  join user in _context.Set<KmuUser>()
                                  on reg.RegDoctorId equals user.UserIdno into UserIndoGroup
                                  from U in UserIndoGroup.DefaultIfEmpty()
                                  where reg.RegHealthId == inHealthId && (reg.RegDate >= _inBeginDate && reg.RegDate <= _inEndDate) && reg.RegStatus != "C"
                                  select new HistoryRecordDto
                                  {
                                      inhospid = reg.Inhospid,
                                      regDate = reg.RegDate.ToDateTime(TimeOnly.Parse("00:00 AM")),
                                      clinicCode = D.DptCode,
                                      clinicName = D.DptName,
                                      doctorCode = reg.RegDoctorId,
                                      doctorName = String.Format("{0} {1} {2}", U.UserNameFirstname, U.UserNameMidname, U.UserNameLastname),
                                      sourceType = D.DptCategory,
                                      regFollowCode = reg.RegFollowCode

                                  };

                //測試用記得ban
                //getHistoryRecordDetail(historyData.Select(c => c.inhospid).ToList());

                if (historyData.Any())
                {
                    return historyData.OrderByDescending(c => c.regDate).ToList();
                }
                else
                {
                    return new List<HistoryRecordDto>();
                }
            }
            catch
            {
                return new List<HistoryRecordDto>();
            }
        }
        public List<HistoryRecordDto> getHistoryRecordMasterWard(DateTime? inBeginDate, DateTime? inEndDate, string inHealthId)
        {
            try
            {
                DateTime _inBeginDate = DateTime.Today.AddMonths(-96);
                DateTime _inEndDate = DateTime.Today;

         

                if (inBeginDate is not null)
                {
                    _inBeginDate = inBeginDate.Value;
                }

                if (inEndDate is not null)
                {
                    _inEndDate = inEndDate.Value;
                }
                Console.WriteLine(_inBeginDate);
                Console.WriteLine(_inEndDate);
                //var historyData = _context.Registrations.Where(c => c.RegPatientId == inChartNo && (c.RegDate >= _inBeginDate && c.RegDate <= _inEndDate));

                //2022.04.11 update by 1050325 排除取消掛號的病人
                var historyData = from reg in _context.Set<InpatientReservation>()
                                  join dept in _context.Set<KmuDepartment>()
                                  on reg.department equals dept.DptCode into DptCodeGroup
                                  from D in DptCodeGroup.DefaultIfEmpty()
                                  join ward in _context.Set<Ward>()
                                  on reg.wardId equals ward.wardid into ward
                                  from w in ward.DefaultIfEmpty()
                                  join user in _context.Set<KmuUser>()
                                  on reg.createBy equals user.UserIdno into UserIndoGroup
                                  from U in UserIndoGroup.DefaultIfEmpty()
                                  where reg.healthId == inHealthId
                                                                 && reg.reserveDate >= _inBeginDate
                                                                 && reg.reserveDate <= _inEndDate
                                                                 && reg.status != ""
                                  select new HistoryRecordDto
                                  {
                                      inhospid = reg.inhospId,
                                      regDate = reg.reserveDate,
                                      clinicCode = D.DptCode,
                                      clinicName = D.DptName,
                                      wardid = w.wardName,
                                      doctorCode = reg.createBy,
                                      doctorName = String.Format("{0} {1} {2}", U.UserNameFirstname, U.UserNameMidname, U.UserNameLastname),
                                      sourceType = D.DptCategory,
                                      regFollowCode = reg.status

                                  };

                //測試用記得ban
                //getHistoryRecordDetail(historyData.Select(c => c.inhospid).ToList());

                if (historyData.Any())
                {
                    return historyData.OrderByDescending(c => c.regDate).ToList();
                }
                else
                {
                    return new List<HistoryRecordDto>();
                }
            }
            catch
            {
                return new List<HistoryRecordDto>();
            }
        }


        public List<HistoryRecordDetail> getHistoryRecordDetail(List<string> inhospidList)
        {
            try
            {
                if (inhospidList == null || inhospidList.Count == 0)
                {
                    return new List<HistoryRecordDetail>();
                }
                else
                {
                    using (SoapService service = new SoapService(_context))
                    {
                        List<HistoryRecordDetail> detailList = new List<HistoryRecordDetail>();
                        using (SoapController _soapController = new SoapController(_context, null))
                        {
                            //2023.04.28 add by 1050325 MG_INFO
                            var mg_info_list = _soapController.getMgInfo();

                            var idOrder = inhospidList.Distinct().ToList();

                            var regsByInhosp = _context.Registrations
                                .Where(c => idOrder.Contains(c.Inhospid))
                                .ToList()
                                .GroupBy(c => c.Inhospid)
                                .ToDictionary(g => g.Key, g => g.First());

                            var soapByInhosp = _context.Hisordersoas
                                .Where(c => idOrder.Contains(c.Inhospid) && c.Status == 'V')
                                .ToList()
                                .GroupBy(c => c.Inhospid)
                                .ToDictionary(g => g.Key, g => g.ToList());

                            var plansByInhosp = _context.Hisorderplans
                                .Where(c => idOrder.Contains(c.Inhospid))
                                .ToList()
                                .GroupBy(c => c.Inhospid)
                                .ToDictionary(g => g.Key, g => g.ToList());

                            foreach (var inhospid in inhospidList)
                            {
                                regsByInhosp.TryGetValue(inhospid, out var regRow);
                                var regFollowCode = regRow?.RegFollowCode ?? "";

                                List<ExtendHisrdersoa> soapData_ex = new List<ExtendHisrdersoa>();
                                soapByInhosp.TryGetValue(inhospid, out var soapData);
                                if (soapData != null)
                                {
                                    foreach (var obj in soapData)
                                    {
                                        soapData_ex.Add(new ExtendHisrdersoa()
                                        {
                                            Inhospid = obj.Inhospid,
                                            Soaid = obj.Soaid,
                                            HealthId = obj.HealthId,
                                            Context = obj.Context,
                                            Kind = obj.Kind,
                                            SourceType = obj.SourceType,
                                            Status = obj.Status,
                                            CreateUser = obj.CreateUser,
                                            CreateDate = obj.CreateDate,
                                            ModifyDate = obj.ModifyDate,
                                            ModifyUser = obj.ModifyUser,
                                            VersionCode = obj.VersionCode,
                                            pure_context = service.StripHTML(obj.Context)
                                        });

                                    }
                                }

                                plansByInhosp.TryGetValue(inhospid, out var allPlansForInhosp);
                                if (allPlansForInhosp == null)
                                {
                                    allPlansForInhosp = new List<Hisorderplan>();
                                }

                                // Prefer active (non–DC) rows; for closed visits, fall back so history still shows what was ordered.
                                var diagnosisData = allPlansForInhosp
                                    .Where(c => c.HplanType == "ICD" && c.DcDate == null)
                                    .OrderBy(c => c.SeqNo)
                                    .ToList();
                                if (diagnosisData.Count == 0)
                                {
                                    diagnosisData = allPlansForInhosp
                                        .Where(c => c.HplanType == "ICD")
                                        .OrderBy(c => c.SeqNo)
                                        .ToList();
                                }

                                var medData = allPlansForInhosp.Where(c => c.HplanType == "Med" && c.DcDate == null).ToList();

                                var nonmedData = allPlansForInhosp
                                    .Where(c => c.HplanType != "Med" && c.HplanType != "ICD" && c.DcDate == null)
                                    .OrderBy(c => c.SeqNo)
                                    .ToList();
                                if (nonmedData.Count == 0)
                                {
                                    nonmedData = allPlansForInhosp
                                        .Where(c => c.HplanType != "Med" && c.HplanType != "ICD")
                                        .OrderBy(c => c.SeqNo)
                                        .ToList();
                                }

                                detailList.Add(new HistoryRecordDetail()
                                {
                                    inhospid = inhospid,
                                    soapContext = soapData_ex,
                                    DiagnosisContext = diagnosisData,
                                    MedicineContext = medData,
                                    NonMedContext = nonmedData,
                                    MG_INFO = mg_info_list,
                                    RegFollowCode = regFollowCode,
                                }); ;

                            }

                            return detailList;
                        }
                    }
                }
            }
            catch
            {
                return new List<HistoryRecordDetail>();
            }
        }


        public IActionResult HistoryRecordMastrView(string patientInhospid, string patientPatientid,string clinicSourceType, bool showClinic = true, bool showPrint = false)
        {
            PatientDTO patientInfo = null;
            if (string.IsNullOrWhiteSpace(patientInhospid))
            {
                patientInfo = GetPatientInfo(patientPatientid);
            }
            else
            {
                patientInfo = GetPatientInfo(patientInhospid, patientPatientid);
            }
            List<HistoryRecordDto> HistoryRecordMaster = null;

            if (patientInfo != null)
            {
                if (clinicSourceType == "WARD")
                {
                    HistoryRecordMaster = getHistoryRecordMasterWard(
                        patientInfo.RegDate?.AddMonths(-96),
                        patientInfo.RegDate,
                        patientInfo.RegPatientId);
                }
                else
                {
                    HistoryRecordMaster = getHistoryRecordMaster(
                        patientInfo.RegDate?.AddMonths(-96),
                        patientInfo.RegDate,
                        patientInfo.RegPatientId);
                }
                //取得歷史病歷頭檔，預設6個月

                List<HistoryRecordDetail> HistoryRecordDetail = null;
                if (HistoryRecordMaster != null && HistoryRecordMaster.Count() > 0)
                {
                    HistoryRecordDetail = getHistoryRecordDetail(HistoryRecordMaster.Select(c => c.inhospid).ToList());

                }

                ViewData["patientInfo"] = patientInfo;
                ViewData["HistoryRecordMaster"] = HistoryRecordMaster;
                ViewData["HistoryRecordDetail"] = HistoryRecordDetail;
                ViewData["ShowClinic"] = showClinic;
                ViewData["ShowPrint"] = showPrint;


                return PartialView("~/Areas/HisOrder/Views/HisOrder/PartialViews/_HistoryRecordMasterPartialView.cshtml", new ViewDataDictionary(ViewData));
            }
            else
            {
                return Content("null", "text/html");
            }
        }

        /// <summary>
        /// 紀錄看診相關時間
        /// </summary>
        /// <param name="inhospid"></param>
        /// <param name="inType"></param>
        public void modifyRegStartEndTime(string inhospid, string inType, string inEmpCode)
        {
            if (string.IsNullOrWhiteSpace(inhospid) || string.IsNullOrWhiteSpace(inType))
            {
                return;
            }
            else
            {
                var regData = _context.Registrations.Where(c => c.Inhospid == inhospid).FirstOrDefault();
                if (regData != null)
                {
                    if (inType == "reg_start")
                    {
                        if (regData.RegStatus == "N")
                        {
                            regData.RegStartTime = DateTime.Now;

                        }

                        if (regData.RegStatus == "O")
                        {
                            regData.reg_observe_end_time = DateTime.Now;
                        }

                        if (regData.RegStatus == "T")
                        {
                            regData.RegExamEndTime = DateTime.Now;
                        }

                    }
                    if (inType == "reg_end")
                    {
                        if (regData.RegEndTime == null && regData.RegStatus == "*")
                        {
                            regData.RegEndTime = DateTime.Now;

                        }

                    }

                    if (inType == "exam_start")
                    {
                        if (regData.RegExamStartTime == null && regData.RegStatus == "T")
                        {
                            regData.RegExamStartTime = DateTime.Now;
                        }
                        if (regData.reg_observe_end_time == null && regData.RegStatus == "O")
                        {
                            regData.reg_observe_end_time = DateTime.Now;
                        }
                    }
                    if (inType == "observe_start")
                    {
                        if (regData.reg_observe_start_time == null && regData.RegStatus == "O")
                        {
                            regData.reg_observe_start_time = DateTime.Now;

                        }
                        if (regData.RegExamEndTime == null && regData.RegStatus == "T")
                        {
                            regData.RegExamEndTime = DateTime.Now;

                        }
                    }

                    //2023.06.26 add by 1050325 每次存檔都要記錄看診醫師
                    if (!string.IsNullOrWhiteSpace(inEmpCode))
                    {
                        regData.RegDoctorId = inEmpCode;
                    }


                    _context.Update(regData);
                    _context.SaveChanges();
                }

                return;
            }
        }


        #endregion
    }

}
