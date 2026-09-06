using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Areas.HisOrder.ViewModels;
using KMU.HisOrder.MVC.Areas.MedicalRecord.Models;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace KMU.HisOrder.MVC.Areas.HisOrder.Controllers
{
	[Area("HisOrder")]
	public class TestResultController : Controller
	{

		public readonly KMUContext _ctx;

		public TestResultController(KMUContext context)
		{
			_ctx = context;

		}

		public IActionResult Detail(string patientId, string sex)
		{

			var departments = from rg in _ctx.Registrations
							  join plan in _ctx.Hisorderplans
							  on rg.Inhospid equals plan.Inhospid
							  join dpt in _ctx.KmuDepartments
							  on rg.RegDepartment equals dpt.DptCode
							  where plan.HealthId == patientId & (plan.HplanType == "Lab" || plan.HplanType == "Path" || plan.HplanType == "Exam")
							  select new departmentDetail
							  {
								  departmentName = dpt.DptName,
								  reg_date = rg.RegDate,
								  inhospid = plan.Inhospid,
								  sex = sex,
								  patientid = patientId
							  };

			return View(departments);
		}


		public async Task<IActionResult> ListResult(string? inhospid, string patientid, string sex)
		{
			try
			{

				var existresult = _ctx.testresults.Where(r => r.refbillorder == inhospid).ToList();
				bool onlyCbcResults = existresult.All(r => r.TestName == "CBC");
				var hisorderplans = _ctx.Hisorderplans.Where(r => r.Inhospid == inhospid && r.HplanType == "Lab").ToList();
				bool onlyCbc = hisorderplans.All(r => r.PlanDes == "CBC");
				if (!onlyCbc)
				{
					if (!existresult.Any() || onlyCbcResults)
					{
						using (var httpClient = new HttpClient())
						{
							try
							{
								var url = "http://192.168.30.202:80/";
								var response = await httpClient.GetAsync(url);
								if (response.IsSuccessStatusCode)
								{

									url = $"http://192.168.30.202:80/api/pateint/Details?pateintcode={patientid}&approved=1";
									response = await httpClient.GetAsync(url);

									if (response.IsSuccessStatusCode)
									{
										var patientDetails = await response.Content.ReadAsStringAsync();
										var patientResponse = JsonConvert.DeserializeObject<PatientResponse>(patientDetails);

										if (patientResponse.testDetails != null && patientResponse.testDetails.Count > 0)
										{
											var existResultDetial = patientResponse.testDetails.Where(t => t.refbillorder == inhospid);
											if (existResultDetial.Any())
											{
												var root = new PatientResponse();
												root.testresultSave = new List<TestResult>();
												foreach (var testresult in patientResponse.testDetails)
												{
													if (testresult.refbillorder == inhospid)
													{
														testresult.status = "Approved";

														root.testresultSave.Add(testresult);
													}
												}
												_ctx.testresults.AddRange(root.testresultSave);
												_ctx.SaveChanges();
											}
										}
									}
								}
							}
							catch (HttpRequestException ex)
							{
								TempData["error"] = ex.Message;

							}
						}
					}
				}
				var wbcSubTests = new[] { "LY", "MO", "EO", "BA", "NE" };

				var listresult = _ctx.testresults
					.Where(t => t.refbillorder == inhospid && t.status == "Approved")
					.GroupBy(t => new { t.code, t.TestName })
			.Select(g => new TestResultViewModel
			{
				sex = sex,
				Code = g.Key.code,
				TestGroup = g.Select(r => r.TestName).FirstOrDefault(),
				TestName = g.Key.TestName,
				subresults = g.Select(r => new subresult
				{
					contents = r.Contents,
					result = r.Result,
					sufix = r.Sufix,
					NvalueMale = r.NvalueMale,
					NvalueFemale = r.NvalueFemale
				}).ToList()
			})
			.AsEnumerable()
			.GroupBy(vm => vm.TestName)
			.Select(group => new TestResultViewModel
			{
				TestGroup = group.Key,
				subresults = group.SelectMany(vm => vm.subresults)
					.OrderByDescending(sr => sr.contents.Contains("WBC"))
					.ThenBy(sr => wbcSubTests.Any(sub => sr.contents.Contains(sub))
						? Array.IndexOf(wbcSubTests, wbcSubTests.First(sub => sr.contents.Contains(sub)))
						: int.MaxValue)
					.ThenBy(sr => sr.contents)
					.ToList(),
				sex = group.First().sex,
				Code = group.First().Code,
				TestName = group.First().TestName
			})
			.ToList();


				return View(listresult);




			}
			catch (Exception ex)
			{

				return View(ex.Message);
			}


		}



	}
}
