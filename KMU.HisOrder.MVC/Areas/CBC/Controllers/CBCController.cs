using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KMU.HisOrder.MVC.Models;
using NuGet.Protocol;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using System.Net;
using KMU.HisOrder.MVC.Areas.HisOrder.ViewModels;
using Newtonsoft.Json;
using System.Numerics;
using System.Text.Json.Nodes;
using KMU.HisOrder.MVC.Areas.CBC.ViewModel;

namespace KMU.HisOrder.MVC.Areas.CBC.Controllers
{
    [Area("CBC")]
    public class CBCController : Controller
    {
        private readonly KMUContext _context;

        public CBCController(KMUContext context)
        {
            _context = context;
        }

        public  IActionResult CreateCBC()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> FindPatient(string inhospid)
        {
            try
            {
                string correctInhospid = inhospid.PadLeft(17, '0');

                var hospitalRecord = await _context.Hisorderplans.FirstOrDefaultAsync(p => p.Inhospid == correctInhospid);
                if (hospitalRecord ==null)
                {
                    var Nodata = $"In-Hospital ID not found. {correctInhospid}";
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                    return Json(new { responseText = Nodata });
                }

                var patientid = hospitalRecord.HealthId;

                var patient =await _context.KmuCharts.FirstOrDefaultAsync(p => p.ChrHealthId == patientid);
                if (patient==null)
                {
                    var Nodata = "This patient does'nt exits.";
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                    return Json(new { responseText = Nodata });
                }
                var labRecords = await _context.Hisorderplans
                .Where(lab => lab.Inhospid == correctInhospid && (lab.HplanType == "Lab" || lab.HplanType == "Path" || lab.HplanType == "Exam")).ToListAsync();

                if (!labRecords.Any())
                {
                    var Nodata = "No lab records found for this patient.";
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                    return Json(new { responseText = Nodata });
                }

                else
                {
                    var patientInfo = new KmuChart
                    {
                        
                        ChrPatientFirstname = patient.ChrPatientFirstname,
                        ChrPatientMidname = patient.ChrPatientMidname,
                        ChrPatientLastname = patient.ChrPatientLastname,
                        ChrHealthId = patient.ChrHealthId,
                        ChrSex = patient.ChrSex,
                        ChrMobilePhone = patient.ChrMobilePhone,
                        ChrEmgContact = patient.ChrBirthDate?.ToString(),
                        ChrAddress = patient.ChrAddress
                    };
                    if (patientInfo.ChrSex == "M")
                    {
                        patientInfo.ChrSex = "Male";
                    }
                    else if (patientInfo.ChrSex == "F")
                    {
                        patientInfo.ChrSex = "Female";
                    }
                    return Json(patientInfo);

                }
            }
            catch (Exception ex)
            {
                return Json(new { responseText = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult GetCBCData(string inhospid)
        {
            string correctInhospid = inhospid.PadLeft(17, '0');

            try
            {



                var cbcData = _context.testresults
                                      .Where(r => r.refbillorder == correctInhospid && r.TestName == "CBC")
                                      .Select(r => new
                                      {
                                          cbcType = r.Contents,
                                          cbcValue = r.Result
                                      })
                                      .ToList();

                if (cbcData.Any())
                {

                    return Json(cbcData);

                }
                bool cbcPlannned = _context.Hisorderplans.Any(o => o.Inhospid == correctInhospid && o.HplanType == "Lab" && o.PlanDes == "CBC");

                if (!cbcPlannned)
                {
                var Nodata = "These Patient Has No CBC Lab";
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return Json(new { responseText = Nodata });
                }
                var savedIndicator = new { CanSave = true };
                return Json(savedIndicator);
                

            }
            catch (Exception ex)
            {
                var Nodata = ex.Message;
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                return Json(new { responseText = Nodata });

            }
        }
        [HttpPost]
        public async Task<IActionResult> SaveCBC(string inhospid, string patientId, List<TestResult> cbcResults, string sheetType)
        {
            string correctInhospid = inhospid.PadLeft(17, '0');
            var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

            try
            {
                if (!cbcResults.Any())
                {
                    var Nodata = "No CBC results provided.";
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return Json(new { responseText = Nodata });

                }


                var existingCBC = await _context.testresults
                    .Where(tr => tr.refbillorder == correctInhospid)
                    .ToListAsync();


                string Reportby = login.EMPCODE;
                string Billno = existingCBC.FirstOrDefault()?.Billno;
                string OriOrder = existingCBC.FirstOrDefault()?.OriOrder;

                var predefinedValues = await _context.KmuCoderefs
                 .Where(cr => cr.RefCodetype == "Laboratory" && cr.RefCode == "LD3004")
                 .Select(cr => new
                 {
                     cr.RefName,
                     NormalValueMale = cr.RefDes.Trim(),
                     NormalValueFemale = cr.RefDes.Trim(),
                     Suffix = cr.RefDes2.Trim()
                 })
                 .ToDictionaryAsync(x => x.RefName, x => (x.NormalValueMale, x.NormalValueFemale, x.Suffix));


                foreach (var result in cbcResults)
                {
                    if (predefinedValues.TryGetValue(result.TestName, out var values))
                    {
                        if (!ValidateResult(result, values))
                        {
                            var Nodata = $"Invalid result for test: {result.TestName}.";
                            Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                            return Json(new { responseText = Nodata });

                        }

                        var testResult = new TestResult
                        {
                            TestGroup = "HEAMATOLOGY",
                            TestName = "CBC",
                            Result = result.Result,
                            Billdate = DateTime.UtcNow.ToString(),
                            Billno = Billno,
                            NvalueMale = values.NormalValueMale,
                            NvalueFemale = values.NormalValueFemale,
                            Contents = result.TestName,
                            Sufix = values.Suffix,
                            refbillorder = correctInhospid,
                            OriOrder = OriOrder,
                            Reportedby = Reportby,
                            code = "LD3004",
                            status="UnApproved",

                        };
                        _context.testresults?.Add(testResult);
                    }
                    else
                    {
                    }
                }
                await _context.SaveChangesAsync();
                return Json("CBC results saved successfully.");
            }

            catch (Exception ex)
            {
                var Nodata = "Null reference encountered: " + ex.Message;
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                return Json(new { responseText = Nodata });
            }
        }
        private bool ValidateResult(TestResult result, (string NormalValueMale, string NormalValueFemale, string Suffix) values)
        {
            if (string.IsNullOrEmpty(result.Result))
            {
                return false;
            }

            return true;
        }
        [HttpPut]
        public async Task<IActionResult> UpdateCBC(string inhospid, string patientId, List<TestResult> cbcResults, string sheetType)
        {
            string correctInhospid = inhospid.PadLeft(17, '0');

            try
            {
                if (!cbcResults.Any())
                {
                    var Nodata = "No CBC results provided.";
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return Json(new { responseText = Nodata });
                }


                var existingCBC = await _context.testresults
                    .Where(tr => tr.refbillorder == correctInhospid)
                    .ToListAsync();

                if (!existingCBC.Any())
                {
                    var Nodata = "No existing CBC results found for the specified In-Hospital ID.";
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return Json(new { responseText = Nodata });
                }

                foreach (var result in cbcResults)
                {
                    var testToUpdate = existingCBC
                        .FirstOrDefault(tr => tr.Contents == result.TestName);

                    if (testToUpdate != null)
                    {
                        testToUpdate.Result = result.Result;
                        testToUpdate.status = "UnApproved";
                    }
                    else
                    {
                    }
                }

                await _context.SaveChangesAsync();
                return Json("CBC results updated successfully.");
            }
            catch (Exception ex)
            {
                var Nodata = "Error occurred: " + ex.Message;
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Json(new { responseText = Nodata });
            }
        }

        public IActionResult ApprovedCBC(string inHospitalId, DateTime? selectedDate)
        {
            if (!string.IsNullOrEmpty(inHospitalId))
            {
                selectedDate = null;
            }

            selectedDate = selectedDate ?? DateTime.Today;
            ViewBag.SelectedDate = selectedDate.Value.ToString("yyyy-MM-dd");


            var query = _context.testresults
                .Join(_context.Hisorderplans,
                      test => test.refbillorder,
                      plan => plan.Inhospid,
                      (test, plan) => new { test, plan })
                .Join(_context.KmuCharts,
                      combined => combined.plan.HealthId,
                      chart => chart.ChrHealthId,
                      (combined, chart) => new { combined.test, chart })
                .Where(x => x.test.TestName.Contains("CBC")&&x.test.Contents!="CBC")
                .AsQueryable();

            if (!string.IsNullOrEmpty(inHospitalId))
            {
                string correctInhospid = inHospitalId.PadLeft(17, '0');
                query = query.Where(x => x.test.refbillorder == correctInhospid);
                var rawData = query
                 .Where(x => x.test.refbillorder == correctInhospid)
                 .ToList();


                query = rawData.AsQueryable();
            }
            else if (selectedDate.HasValue)
            {
                string formattedDate = selectedDate.Value.ToString("yyyy-MM-dd");

                var rawData = query
                    .Where(x => x.test.Billdate != null)
                    .ToList();

                var filteredData = rawData
                    .Where(x => DateTime.TryParse(x.test.Billdate, out var billDate) &&
                                billDate.Date == selectedDate.Value.Date)
                    .ToList();

                query = filteredData.AsQueryable();
            }

            var patientData = query
                .GroupBy(x => x.test.refbillorder)
                .Select(grouped => new TestLabsViewModel
                {
                    InHospitalId = grouped.Key,
                    TestId = grouped.OrderByDescending(x => x.test.Billdate).FirstOrDefault().test.testresultid,
                    status = grouped.OrderByDescending(x => x.test.Billdate).FirstOrDefault().test.status,
                    TestDate = grouped.OrderByDescending(x => x.test.Billdate).FirstOrDefault().test.Billdate,
                    PatientName = grouped.FirstOrDefault().chart.ChrPatientFirstname,
                    PatientId = grouped.FirstOrDefault().chart.ChrHealthId,
                    FirstName = grouped.FirstOrDefault().chart.ChrPatientFirstname,
                    MidName = grouped.FirstOrDefault().chart.ChrPatientMidname,
                    LastName = grouped.FirstOrDefault().chart.ChrPatientLastname,
                    MobilePhone = grouped.FirstOrDefault().chart.ChrMobilePhone,
                    Gender = grouped.FirstOrDefault().chart.ChrSex,
                    BirthDate = grouped.FirstOrDefault().chart.ChrBirthDate,
                    Address = grouped.FirstOrDefault().chart.ChrAddress
                })
                .ToList();


            return View(patientData);
        }
        public async Task<IActionResult> GetPatientLabsAsync(string InhospitalId)
        {
            var filteredTests = await _context.testresults
                .Where(test => test.TestName == "CBC" && test.refbillorder == InhospitalId  &&test.Contents!="CBC")
                .ToListAsync();
            if (!filteredTests.Any())
            {
                return PartialView("PartialViews/_LabDetails", Enumerable.Empty<TestLabsViewModel>());
            }

            var codes = filteredTests.Select(t => t.code).Distinct().ToList();
            var refbillorders = filteredTests.Select(t => t.refbillorder).Distinct().ToList();

            var plans = await _context.Hisorderplans
                .Where(plan => codes.Contains(plan.PlanCode) && refbillorders.Contains(plan.Inhospid))
                .ToListAsync();

            if (!plans.Any())
            {
                return PartialView("PartialViews/_LabDetails", Enumerable.Empty<TestLabsViewModel>());
            }

            var healthIds = plans.Select(p => p.HealthId).Distinct().ToList();
            var charts = await _context.KmuCharts
                .Where(chart => healthIds.Contains(chart.ChrHealthId))
                .ToListAsync();

            if (!charts.Any())
            {
                return PartialView("PartialViews/_LabDetails", Enumerable.Empty<TestLabsViewModel>());
            }
            var names = filteredTests.Select(t => t.Contents).Distinct().ToList();

            var codeRefs = await _context.KmuCoderefs
     .Where(c => codes.Contains(c.RefCode) && names.Contains(c.RefName))
     .ToListAsync();

            var codeNameSequence = codeRefs.ToDictionary(c => new { c.RefCode, c.RefName }, c => c.RefShowseq);

            var patientData = filteredTests
                .GroupBy(test => test.refbillorder)
                .Select(grouped =>
                {
                    var firstPlan = plans.FirstOrDefault(plan => plan.Inhospid == grouped.Key);
                    var chart = charts.FirstOrDefault(c => c.ChrHealthId == firstPlan?.HealthId);

                    return new TestLabsViewModel
                    {
                        InHospitalId = grouped.Key,
                        PatientName = chart?.ChrPatientFirstname,
                        PatientId = chart?.ChrHealthId,
                        FirstName = chart?.ChrPatientFirstname,
                        MidName = chart?.ChrPatientMidname,
                        LastName = chart?.ChrPatientLastname,
                        MobilePhone = chart?.ChrMobilePhone,
                        Gender = chart?.ChrSex,
                        BirthDate = chart?.ChrBirthDate,
                        Address = chart?.ChrAddress,
                        TestResults = grouped.
                        OrderBy(test =>
                            codeNameSequence.ContainsKey(new { RefCode = test.code, RefName = test.Contents })
                                ? codeNameSequence[new { RefCode = test.code, RefName = test.Contents }]
                                : int.MaxValue)
                            .Select(test => new TestResult
                            {
                                TestName = test.TestName,
                                Billdate = test.Billdate,
                                TestGroup = test.TestGroup ?? "",
                                refbillorder = test.refbillorder,
                                Result = test.Result ?? "",
                                NvalueMale = test.NvalueMale ?? "",
                                NvalueFemale = test.NvalueFemale ?? "",
                                Contents = test.Contents ?? "",
                                Sufix = test.Sufix ?? "",
                                code = test.code,
                                Reportedby = test.Reportedby ?? "",
                                status = test.status ?? "",
                            })
                            .ToList()
                    };
                })
                .FirstOrDefault();

            return PartialView("PartialViews/_LabDetails", new List<TestLabsViewModel> { patientData });
        }
        [HttpPost]
        public IActionResult ApproveLabs(string inHospitalId)
        {
            if (string.IsNullOrEmpty(inHospitalId))
            {
                return Json(new { success = false, message = "In-Hospital ID is missing." });
            }

            try
            {
                var labTests = _context.testresults.Where(x => x.refbillorder == inHospitalId).ToList();
                var login = HttpContext.Session.GetObject<LoginDTO>("LoginDTO");

                if (labTests.Any())
                {
                    foreach (var test in labTests)
                    {
                        test.status = "Approved";
                        test.Reportedby=login.EMPCODE;
                    }

                    _context.SaveChanges();
                }

                return Json(new { success = true });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }


    }

}
