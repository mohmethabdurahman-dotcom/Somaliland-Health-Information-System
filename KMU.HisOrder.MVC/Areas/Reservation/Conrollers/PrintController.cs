using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Areas.Maintenance.Models;
using KMU.HisOrder.MVC.Areas.MedicalRecord.ViewModels;
using KMU.HisOrder.MVC.Areas.Reservation.ViewModels;
using KMU.HisOrder.MVC.Extesion;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace KMU.HisOrder.MVC.Areas.Reservation.Conrollers
{
    [Area("Reservation")]
    [Authorize(Roles = "Reservation,OPDReserve,EMGReserve")]
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

        public PartialViewResult PrintReserveSheet(string InHospID, string patientID,string reserveType)
        {
            ViewModels.PrintClass ReservePrintData = new ViewModels.PrintClass();
            CommonService cService = new CommonService(_context);
            PhysicalSignService pService = new PhysicalSignService(_context);
            #region Login Session Check

            var checklogin = LoginSessionHelper.GetOrRestore(HttpContext);
            if (checklogin == null)
                checklogin = new LoginDTO { EMPCODE = "", EMPNAME = "" };

            #endregion

            ReservePrintData.HospName = _context.KmuCoderefs.Where(c => c.RefCodetype == "HospitalCode").Select(c => c.RefName).FirstOrDefault().ToString();

            var reserveDtos = _context.Registrations.Where(c => c.Inhospid == InHospID);
            var patientDtos = _context.KmuCharts.Where(c => c.ChrHealthId == patientID);
            var physicalDtos = _context.PhysicalSigns.Where(c => c.Inhospid == InHospID);


            if (patientDtos.Any())
            {
                PersonalCardClass personal = new PersonalCardClass()
                {
                    FIRSTNAME = patientDtos.FirstOrDefault().ChrPatientFirstname,
                    MIDNAME = patientDtos.FirstOrDefault().ChrPatientMidname,
                    LASTNAME = patientDtos.FirstOrDefault().ChrPatientLastname,
                    BIRTHDATE = patientDtos.FirstOrDefault().ChrBirthDate,
                    PATIENTID = patientDtos.FirstOrDefault().ChrHealthId,
                    GENDER = patientDtos.FirstOrDefault().ChrSex,
                    enumGender = patientDtos.FirstOrDefault().ChrSex == "M" ? EnumClass.EnumGender.Male : EnumClass.EnumGender.Female
                };

                ReservePrintData.patientInfo = personal;
            }
            else
            {
                ReservePrintData.patientInfo = new PersonalCardClass();
            }

            if (reserveDtos.Any())
            {
                ClinicSchedule clinic = new ClinicSchedule();
                clinic = _context.ClinicSchedules.Where(c => c.ScheWeek == reserveDtos.FirstOrDefault().RegDate.DayOfWeek.ToString("d") &&
                                                           c.ScheDptCode == reserveDtos.FirstOrDefault().RegDepartment &&
                                                           c.ScheNoon == reserveDtos.FirstOrDefault().RegNoon).FirstOrDefault();

                AppointmentDetail appointment = new AppointmentDetail()
                {
                    reserveDate = reserveDtos.FirstOrDefault().RegDate.ToDateTime(TimeOnly.MinValue),
                    reserveDpt = reserveDtos.FirstOrDefault().RegDepartment,
                    reserveDptName = cService.GetDepartmentName(reserveDtos.FirstOrDefault().RegDepartment),
                    reserveNoon = reserveDtos.FirstOrDefault().RegNoon,
                    seqNo = reserveDtos.FirstOrDefault().RegSeqNo,
                    patientID = reserveDtos.FirstOrDefault().RegHealthId,
                    inHospID = reserveDtos.FirstOrDefault().Inhospid,
                    reserveRoom = reserveDtos.FirstOrDefault().RegRoomNo,
                    reserveDoctor = reserveDtos.FirstOrDefault().RegDoctorId,
                    reserveDoctorName = cService.GetEmpName(reserveDtos.FirstOrDefault().RegDoctorId),
                    triageLevel = reserveDtos.FirstOrDefault().RegTriage,
                    clinicRemark = clinic.ScheRemark

                };

                ReservePrintData.AppointmentSheet = appointment;
            }
            else
            {
                ReservePrintData.AppointmentSheet = new AppointmentDetail();
            }

            if (physicalDtos.Any())
            {
                ReservePrintData.phySignItems = physicalDtos.ToList();
            }
            else
            {
                ReservePrintData.phySignItems = new List<PhysicalSign>();
            }
            ReservePrintData.reserveType = reserveType;
            ReservePrintData.ModifyID = checklogin.EMPCODE;
            ReservePrintData.ModifyName = checklogin.EMPNAME;
            ReservePrintData.phySignTitle = pService.GetPhysicalSignItems(reserveType).Where(c=>c.DefaultFlag).ToList();

            
            ViewData["Title"] = "Appointment Sheet";
            return PartialView("Areas/Reservation/Views/Print/_PrintTemplate.cshtml", ReservePrintData);
        }
    }
}
