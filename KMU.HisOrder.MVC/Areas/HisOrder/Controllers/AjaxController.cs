using AutoMapper;
using KMU.HisOrder.MVC.Areas.HisCalling.Models;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Areas.Maintenance.Models;
using KMU.HisOrder.MVC.Areas.Maintenance.ViewModels;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Cryptography.Xml;

namespace KMU.HisOrder.MVC.Areas.HisOrder.Controllers
{
    [CheckSessionTimeOutAttribute]
    [Area("HisOrder")]
    public class AjaxController : Controller
    {
        private readonly KMUContext _context;
        private readonly IMapper _mapper;
        public AjaxController(KMUContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [CheckClinicSessionAttribute]
        public string ScreenSave(string transfer, string transfer_des)
        {
            using (HisOrderController col = new HisOrderController(_context))
            {
                var grv = col.getGlobalVariablDTO(HttpContext);
                var result = new ResultDTO() { isSuccess = false };
                var regData = _context.Registrations.Where(c => c.Inhospid == grv.Patient.Inhospid).FirstOrDefault();

                if (regData != null)
                {
                    regData.RegStatus = "T";
                    regData.upload_status = 'N';
                    regData.ModifyTime = DateTime.Now;
                    regData.ModifyUser = grv.Login.EMPCODE;
                    //regData.RegEndTime = DateTime.Now;
                    regData.RegFollowCode = transfer;
                    //regData.RegFollowCode = transfer == "Y" ? "*" : "";
                    regData.RegFollowDesc = transfer_des;
                    _context.Update(regData);
                    _context.SaveChanges();

                    col.modifyRegStartEndTime(regData.Inhospid, "exam_start", grv.Login.EMPCODE);

                    //取得 patient session data
                    var patientInfo = col.GetPatientInfo(grv.Patient.Inhospid, grv.Patient.RegPatientId);

                    col.IniatializeSessionPatientDto(HttpContext, patientInfo);


                    result.isSuccess = true;
                }


                return JsonConvert.SerializeObject(result);
            }
        }

        [HttpPost]
        public JsonResult SaveDischargeStatus(string healthId, string inHospitalId, string dischargeStatus, string transferWard, string transferFacility)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(healthId) || string.IsNullOrEmpty(inHospitalId))
            {
                return Json(new { isSuccess = false, Message = "Missing required fields" });
            }

            try
            {
                var loginUser = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

                var reservation = _context.InpatientReservations
                                          .FirstOrDefault(r => r.healthId == healthId && r.inhospId == inHospitalId);

                if (reservation == null)
                {
                    return Json(new { isSuccess = false, Message = "Reservation not found" });
                }

                var bed = _context.beds.FirstOrDefault(b => b.bedId == reservation.bedId);

                if (bed == null)
                {
                    return Json(new { isSuccess = false, Message = "Bed not found" });
                }

                // Discharge logic
                reservation.status = "Discharged";
                reservation.modifyAt = DateTime.UtcNow;
                reservation.modifyBy = loginUser.EMPCODE;
                reservation.dischargeApprover = loginUser.EMPCODE;
                reservation.dischargeType = dischargeStatus;
                reservation.dischargeDate = DateTime.UtcNow;

                // Assign transfer info based on discharge type
                if (dischargeStatus == "TW")
                {
                    reservation.transfer_place = transferWard;
                }
                else if (dischargeStatus == "TF")
                {
                    reservation.transfer_place = transferFacility;
                }

                // Update bed
                bed.status = "Available";
                bed.modifyAt = DateTime.UtcNow;
                bed.modifyBy = loginUser.EMPCODE;

                _context.SaveChanges();

                return Json(new { isSuccess = true });
            }
            catch (Exception ex)
            {
                return Json(new { isSuccess = false, Message = "An error occurred: " + ex.Message });
            }
        }


        [CheckClinicSessionAttribute]
        public string ObserveSave(string transfer, string transfer_des)
        {
            using (HisOrderController col = new HisOrderController(_context))
            {
                var grv = col.getGlobalVariablDTO(HttpContext);
                var result = new ResultDTO() { isSuccess = false };
                var regData = _context.Registrations.Where(c => c.Inhospid == grv.Patient.Inhospid).FirstOrDefault();

                if (regData != null)
                {
                    regData.upload_status = 'N';

                    regData.RegStatus = "O";
                    regData.ModifyTime = DateTime.Now;
                    regData.ModifyUser = grv.Login.EMPCODE;
                    //regData.RegEndTime = DateTime.Now;
                    regData.RegFollowCode = transfer;
                    //regData.RegFollowCode = transfer == "Y" ? "*" : "";
                    regData.RegFollowDesc = transfer_des;
                    _context.Update(regData);
                    _context.SaveChanges();

                    col.modifyRegStartEndTime(regData.Inhospid, "observe_start", grv.Login.EMPCODE);

                    //取得 patient session data
                    var patientInfo = col.GetPatientInfo(grv.Patient.Inhospid, grv.Patient.RegPatientId);

                    col.IniatializeSessionPatientDto(HttpContext, patientInfo);

                    result.isSuccess = true;

                }


                return JsonConvert.SerializeObject(result);
            }

        }

        [CheckClinicSessionAttribute]
        public string EndVisitSave(string transfer, string transfer_des)
        {
            using (HisOrderController col = new HisOrderController(_context))
            {
                var grv = col.getGlobalVariablDTO(HttpContext);
                var result = new ResultDTO { isSuccess = false };

                if (string.IsNullOrEmpty(grv.Patient?.Inhospid))
                {
                    result.Message = "Invalid Inhospid";
                    return JsonConvert.SerializeObject(result);
                }

                // Inpatient
                if (grv.Patient.Inhospid.StartsWith("IN"))
                {
                    var regDataInp = _context.InpatientReservations
                                            .FirstOrDefault(c => c.inhospId == grv.Patient.Inhospid);

                    if (regDataInp != null)
                    {
                        regDataInp.modifyAt = DateTime.Now;
                        regDataInp.modifyBy = grv.Login.EMPCODE;

                        _context.Update(regDataInp);
                        _context.SaveChanges();

                        result.isSuccess = true;
                    }
                    else
                    {
                        result.Message = "Inpatient reservation not found";
                    }

                    return JsonConvert.SerializeObject(result);
                }

                // Outpatient
                var regData = _context.Registrations
                                      .FirstOrDefault(c => c.Inhospid == grv.Patient.Inhospid);

                if (regData != null)
                {
                    regData.upload_status = 'N';  
                    regData.RegStatus = "*";
                    regData.ModifyTime = DateTime.Now;
                    regData.ModifyUser = grv.Login.EMPCODE;
                    regData.RegFollowCode = transfer;
                    regData.RegFollowDesc = transfer_des;

                    _context.Update(regData);
                    _context.SaveChanges();

                    col.modifyRegStartEndTime(regData.Inhospid, "reg_end", grv.Login.EMPCODE);

                    var patientInfo = col.GetPatientInfo(grv.Patient.Inhospid, grv.Patient.RegPatientId);
                    col.IniatializeSessionPatientDto(HttpContext, patientInfo);

                    result.isSuccess = true;
                }
                else
                {
                    result.Message = "Registration not found";
                }

                return JsonConvert.SerializeObject(result);
            }
        }



        [HttpPost]
        public string GetPatientList(DateTime inRegDate, string inDeptCode, string inRoomNo)
        {
            var result = new ResultDTO() { isSuccess = false };

            using (var controller = new HisOrderController(_context))
            {
                var data = controller.GetPatientList(inRegDate, inDeptCode);

                if (data.Any())
                {
                    result.isSuccess = true;
                    result.returnValue = JsonConvert.SerializeObject(data);
                }


            }
            return JsonConvert.SerializeObject(result);
        }


        [HttpPost]
        public string GetClinicScheList(string inRegDate, string inDoctorCode, string inSourceType)
        {

            var result = new ResultDTO() { isSuccess = false };

            if (string.IsNullOrWhiteSpace(inDoctorCode) || string.IsNullOrWhiteSpace(inRegDate))
            {
                throw new Exception();
            }
            else
            {
                //var scheWeek = (int)DateTime.Parse(inRegDate).DayOfWeek;
                //var data = new List<ClinicSchedule>();
                //var data_opd = _context.ClinicSchedules.Where(c => c.ScheDoctor == inDoctorCode && c.ScheWeek == scheWeek.ToString() && c.ScheNoon == "AM");
                //var data_er = _context.ClinicSchedules.Where(c => c.ScheWeek == scheWeek.ToString() && c.ScheDptCode.Substring(0, 2) == "16");

                //if (data_opd.Any())
                //{
                //    data.AddRange(data_opd);
                //}

                //if (data_er.Any())
                //{
                //    data.AddRange(data_er);
                //}

                using (ClinicScheduleService service = new ClinicScheduleService(_context))
                {
                    var data = service.GetScheduleDataForHisOrder(new string[] { inSourceType }, DateOnly.Parse(inRegDate), inDoctorCode);

                    if (data.Any())
                    {
                        result.isSuccess = true;
                        result.returnValue = JsonConvert.SerializeObject(data.ToList());

                        return JsonConvert.SerializeObject(result);
                    }
                    else
                    {
                        result.isSuccess = false;
                        return JsonConvert.SerializeObject(result);
                    }
                }


            }
        }


        [HttpPost]
        public string callLight(string inhospid)
        {
            var result = new ResultDTO() { isSuccess = false };

            using (var service = new CallService(_context))
            {
                var regdata = _context.Registrations.Where(c => c.Inhospid == inhospid).FirstOrDefault();
                if (regdata != null)
                {
                    if (regdata.RegCallTime == null)
                    {
                        regdata.RegCallTime = DateTime.Now;

                        _context.Update(regdata);
                    }
                    var inWeek = ((int)regdata.RegDate.DayOfWeek).ToString();
                    var inNoon = regdata.RegNoon;
                    var inRoom = regdata.RegRoomNo;
                    var inNo = regdata.RegSeqNo;


                    //var schedata = _context.ClinicSchedules.Where(c => c.ScheWeek == inWeek && c.ScheNoon == inNoon && c.ScheRoom == inRoom).FirstOrDefault();
                    //if(schedata.ScheCallNo == null)
                    //{ 
                    //    schedata.ScheCallNo = regdata.RegSeqNo;
                    //    _context.Update(schedata);
                    //}

                    _context.SaveChanges();

                    var callResult = service.setScheCallNo(inWeek, inNoon, inRoom, inNo);

                    if (callResult.isSuccess == true)
                    {
                        result.isSuccess = true;
                        result.Message = callResult.Message;
                        result.returnValue = JsonConvert.SerializeObject(regdata);
                    }

                }

                return JsonConvert.SerializeObject(result);
            }


        }

    }
}
