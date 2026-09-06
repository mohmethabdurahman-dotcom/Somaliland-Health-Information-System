using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMU.HisOrder.MVC.Areas.InPatient.Controllers
{
    [Area("InPatient")]
    public class InPatientDoctorController : Controller
    {
        private readonly KMUContext _context;
        public InPatientDoctorController(KMUContext context)
        {
            _context = context;
        }
        public IActionResult Index(string inDeptCode)
        {
            var ptData = from d in _context.Set<InpatientReservation>()
                         join chart in _context.Set<KmuChart>()
                         on d.healthId equals chart.ChrHealthId
                         where  d.wardId == inDeptCode
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
                             canVisit = true
                         };
            var p = ptData.ToList();

            return View(p);
        }
        public IActionResult WardList()
        {
            var wards = _context.Wards.ToList();
            return View(wards);
        }
        public IActionResult Comming(string url)
        {
            return View();
        }
        public IActionResult Visit(string patientInhospid, string patientPatientid, string patientVisitStatus, string htmlBody, string htmlBodyActive)
        {
            return RedirectToAction(
               actionName: "PatientVisit",
               controllerName: "HisOrder",
               routeValues: new { patientInhospid,patientPatientid, patientVisitStatus,htmlBody,htmlBodyActive });
        }
        public IActionResult WardSoap(string sourceType = "OPD")
        {
            ViewBag.SourceType = "EMG";
            return View();
        }
    }
}
