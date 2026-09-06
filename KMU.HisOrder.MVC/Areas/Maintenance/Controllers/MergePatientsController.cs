using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KMU.HisOrder.MVC.Models;
using System.Data;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using System.Collections;

namespace KMU.HisOrder.MVC.Areas.Maintenance.Controllers
{
    [Area("Maintenance")]
    //[Authorize(Roles = "Maintain_KmuDepartments")]
    public class MergePatientsController : Controller
    {
        private readonly KMUContext _context;

        public MergePatientsController(KMUContext context)
        {
            _context = context;
        }

        public IActionResult Index(string? phone)
        {
            return View();
        }
        public IActionResult ChartMerged()
        {
            var merged_patients = _context.kmu_chart_MergeHistory.ToList();
            return View(merged_patients);
        }
        public IActionResult getpatients(string? phone, string? name)
        {

            var lphone = "+252 063 " + phone;
            var patients = _context.KmuCharts.Select(p => new {
                p.ChrHealthId,
                p.ChrPatientFirstname,
                p.ChrPatientMidname,
                p.ChrPatientLastname,
                p.ChrSex,
                p.ChrMobilePhone,
                p.ModifyTime,
            }).Where(p => p.ChrMobilePhone == lphone && p.ChrPatientFirstname.ToUpper().Contains(name.ToUpper())).OrderBy(p => p.ModifyTime).Take(2);

            return Json(patients);
        }

        public IActionResult Merge(string[] patientids, kmu_chart_MergeHistory mergedpatient)
        {
            var ID1 = patientids[0];
            var ID2 = patientids[1];
            ArrayList inhospIdList = new ArrayList();

            var regPatient = _context.Registrations.Where(p => p.RegHealthId == ID2);
            var kmu = _context.KMU_MergeHistory;
            foreach (var item in regPatient)
            {
                inhospIdList.Add(item.Inhospid);
                item.RegHealthId = ID1;

                _context.Registrations.Update(item);
            }

            var hisorderpatient = _context.Hisorderplans.Where(p => p.HealthId == ID2);
            foreach (var item in hisorderpatient)
            {
                item.HealthId = ID1;
                _context.Hisorderplans.Update(item);
            }

            var soapPatient = _context.Hisordersoas.Where(p => p.HealthId == ID2);
            foreach (var item in soapPatient)
            {
                item.HealthId = ID1;
                _context.Hisordersoas.Update(item);
            }

            var chartPatient = _context.KmuCharts.FirstOrDefault(p => p.ChrHealthId == ID2);

            if (chartPatient != null)
            {

                var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");
                mergedpatient.chr_halth_id = ID1;
                mergedpatient.mh_health_id = ID2;
                mergedpatient.merged_time = DateTime.Now;
                mergedpatient.merger_user = login.EMPCODE;

                mergedpatient.ChrPatientFirstname = chartPatient.ChrPatientFirstname;
                mergedpatient.ChrPatientMidname = chartPatient.ChrPatientMidname;
                mergedpatient.ChrPatientLastname = chartPatient.ChrPatientLastname;
                mergedpatient.ChrSex = chartPatient.ChrSex;
                mergedpatient.ChrBirthDate = chartPatient.ChrBirthDate;
                mergedpatient.ChrMobilePhone = chartPatient.ChrMobilePhone;
                mergedpatient.ChrAddress = chartPatient.ChrAddress;
                mergedpatient.ChrEmgContact = chartPatient.ChrEmgContact;
                mergedpatient.ChrContactRelation = chartPatient.ChrContactRelation;
                mergedpatient.ChrContactPhone = chartPatient.ChrContactPhone;
                mergedpatient.ChrCombineFlag = chartPatient.ChrCombineFlag;
                mergedpatient.ChrRemark = chartPatient.ChrRemark;
                mergedpatient.ModifyUser = chartPatient.ModifyUser;
                mergedpatient.ModifyTime = chartPatient.ModifyTime;
                mergedpatient.ChrAreaCode = chartPatient.ChrAreaCode;
                mergedpatient.ChrRefugeeFlag = chartPatient.ChrRefugeeFlag;
                mergedpatient.ChrNationalId = chartPatient.ChrNationalId;

                _context.kmu_chart_MergeHistory.Add(mergedpatient);
                _context.KmuCharts.Remove(chartPatient);
            }
            _context.SaveChanges();
            return Json(inhospIdList);

        }


        public IActionResult SaveInhospid(string inhospidList, string[] chrId_mhId, KMU_MergeHistory kmuMergeHistory)
        {
            var ID1 = chrId_mhId[0];
            var ID2 = chrId_mhId[1];

            kmuMergeHistory.InhospId = inhospidList;
            kmuMergeHistory.chr_halth_id = ID1;
            kmuMergeHistory.mh_health_id = ID2;
            kmuMergeHistory.merged_time = DateTime.Now;
            _context.KMU_MergeHistory.Add(kmuMergeHistory);
            _context.SaveChanges();

            return Json(chrId_mhId);
        }

        public async Task<IActionResult> Undo(string id)
        {

            var mHistoryList = _context.KMU_MergeHistory.Where(m => m.mh_health_id == id).ToList();
            return View(mHistoryList);
        }

        public IActionResult Save(string[] inhospitalIdList, string chr_id, string mh_id, KmuChart kmuchart)
        {


            var findmergedPatient = _context.kmu_chart_MergeHistory.SingleOrDefault(p => p.mh_health_id == mh_id);
            var removePatient = _context.kmu_chart_MergeHistory.SingleOrDefault(p => p.mh_health_id == mh_id);
            if (findmergedPatient != null && removePatient != null)
            {
                kmuchart.ChrHealthId = mh_id;
                kmuchart.ChrPatientFirstname = findmergedPatient.ChrPatientFirstname;
                kmuchart.ChrPatientMidname = findmergedPatient.ChrPatientMidname;
                kmuchart.ChrPatientLastname = findmergedPatient.ChrPatientLastname;
                kmuchart.ChrSex = findmergedPatient.ChrSex;
                kmuchart.ChrBirthDate = findmergedPatient.ChrBirthDate;
                kmuchart.ChrMobilePhone = findmergedPatient.ChrMobilePhone;
                kmuchart.ChrAddress = findmergedPatient.ChrAddress;
                kmuchart.ChrEmgContact = findmergedPatient.ChrEmgContact;
                kmuchart.ChrContactRelation = findmergedPatient.ChrContactRelation;
                kmuchart.ChrContactPhone = findmergedPatient.ChrContactPhone;
                kmuchart.ChrCombineFlag = findmergedPatient.ChrCombineFlag;
                kmuchart.ChrRemark = findmergedPatient.ChrRemark;
                kmuchart.ModifyUser = findmergedPatient.ModifyUser;
                kmuchart.ModifyTime = findmergedPatient.ModifyTime;
                kmuchart.ChrAreaCode = findmergedPatient.ChrAreaCode;
                kmuchart.ChrRefugeeFlag = findmergedPatient.ChrRefugeeFlag;
                kmuchart.ChrNationalId = findmergedPatient.ChrNationalId;
                foreach (var item in inhospitalIdList)
                {
                    var finregistration = _context.Registrations.SingleOrDefault(r => r.Inhospid == item);
                    finregistration.RegHealthId = mh_id;
                    _context.Registrations.Update(finregistration);

                    var findsoap = _context.Hisordersoas.Where(r => r.Inhospid == item);
                    foreach (var item2 in findsoap)
                    {
                        item2.HealthId = mh_id;
                        _context.Hisordersoas.Update(item2);
                    }

                    var hisplan = _context.Hisorderplans.Where(p => p.Inhospid == item);
                    foreach (var item2 in hisplan)
                    {
                        item2.HealthId = mh_id;
                        _context.Hisorderplans.Update(item2);

                    }
                }


                _context.KmuCharts.Add(kmuchart);
                _context.kmu_chart_MergeHistory.Remove(removePatient);
                var removePatientKMUMerge = _context.KMU_MergeHistory.Where(p => p.mh_health_id == mh_id).ToList();
                foreach (var item in removePatientKMUMerge)
                {
                    _context.KMU_MergeHistory.Remove(item);
                }
            }
            _context.SaveChanges();
            return Json(inhospitalIdList + "" + chr_id + "" + mh_id);
        }


    }
}
