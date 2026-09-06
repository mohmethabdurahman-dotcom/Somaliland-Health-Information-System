using System.Text.RegularExpressions;
using KMU.HisOrder.MVC.Areas.Radiology.Dtos;
using KMU.HisOrder.MVC.Areas.Radiology.Models;
using KMU.HisOrder.MVC.Areas.Radiology.Services;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMU.HisOrder.MVC.Areas.Radiology.Controllers
{
    [Area("Radiology")]
    public sealed class InterpretationController : Controller
    {
        private static readonly Regex DicomUidRegex = new("^[0-2](\\.(0|[1-9][0-9]*))+$", RegexOptions.Compiled);
        private readonly KMUContext _context;
        private readonly IReportService _reportService;
        private readonly IExamWorkflowService _workflowService;
        private readonly IAIAssistService _aiAssistService;
        private readonly ILogger<InterpretationController> _logger;

        public InterpretationController(
            KMUContext context,
            IReportService reportService,
            IExamWorkflowService workflowService,
            IAIAssistService aiAssistService,
            ILogger<InterpretationController> logger)
        {
            _context = context;
            _reportService = reportService;
            _workflowService = workflowService;
            _aiAssistService = aiAssistService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            DateTime? startDate,
            DateTime? endDate,
            string status,
            string modality,
            string search,
            int page = 1,
            int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize != 10 && pageSize != 25 && pageSize != 50) pageSize = 10;

            var query = from exam in _context.radiologyexamrequests
                        join room in _context.rooms on exam.roomid equals room.roomid into roomJoin
                        from room in roomJoin.DefaultIfEmpty()
                        where
                            exam.studyinstanceuid != null &&
                            exam.status != null &&
                            exam.status != string.Empty &&
                            exam.status.ToUpper() != "CANCELLED"
                        select new
                        {
                            exam.id,
                            exam.orderid,
                            exam.patientid,
                            exam.status,
                            exam.studyinstanceuid,
                            exam.createdat,
                            modality = room != null ? room.modality : null
                        };

            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;
                query = query.Where(x => x.createdat >= start);
            }

            if (endDate.HasValue)
            {
                var endExclusive = endDate.Value.Date.AddDays(1);
                query = query.Where(x => x.createdat < endExclusive);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus = status.Trim().ToUpper();
                query = query.Where(x => x.status.ToUpper() == normalizedStatus);
            }

            if (!string.IsNullOrWhiteSpace(modality))
            {
                var normalizedModality = modality.Trim().ToUpper();
                query = query.Where(x => x.modality != null && x.modality.ToUpper() == normalizedModality);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(x =>
                    x.patientid.Contains(search) ||
                    x.orderid.Contains(search) ||
                    x.id.ToString().Contains(search));
            }

            var totalCount = await query.CountAsync();
            var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
            if (page > totalPages) page = totalPages;

            var exams = await query
                .OrderByDescending(x => x.createdat)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["StartDate"] = startDate?.ToString("yyyy-MM-dd") ?? string.Empty;
            ViewData["EndDate"] = endDate?.ToString("yyyy-MM-dd") ?? string.Empty;
            ViewData["Status"] = status ?? string.Empty;
            ViewData["Modality"] = modality ?? string.Empty;
            ViewData["Search"] = search ?? string.Empty;
            ViewData["Page"] = page;
            ViewData["PageSize"] = pageSize;
            ViewData["TotalCount"] = totalCount;
            ViewData["TotalPages"] = totalPages;

            return View("~/Areas/Radiology/Views/Interpretation/Index.cshtml", exams);
        }

        [HttpGet]
        public async Task<IActionResult> Desktop(int examRequestId)
        {
            var examWithPatient = await (from examReq in _context.radiologyexamrequests
                              join pat in _context.KmuCharts on examReq.patientid equals pat.ChrHealthId
                              where examReq.id == examRequestId
                              select new
                              {
                                  Exam = examReq,
                                  PatientName = pat.ChrPatientFirstname + pat.ChrPatientMidname + pat.ChrPatientLastname, // Adjust based on your actual column names
                                  Gender = pat.ChrSex,
                                  BirthDate = pat.ChrBirthDate,
                              }).FirstOrDefaultAsync();

        

            if (examWithPatient == null)
            {
                return NotFound();
            }
            var exam = examWithPatient.Exam;
            if (!IsValidDicomUid(exam.studyinstanceuid))
            {
                return BadRequest("Invalid StudyInstanceUID.");
            }

            var report = await _context.radiologyreports.FirstOrDefaultAsync(r => r.radrequestid == examRequestId);
            if (report == null)
            {
                report = new radiologyreport
                {
                    radrequestid = examRequestId,
                    inhospid = exam.inhospid ?? string.Empty,
                    radiologistid = User.Identity?.Name ?? string.Empty,
                    studyinstanceuid = exam.studyinstanceuid,
                    report_text = string.Empty,
                    status = radiologyreport.ReportStatuses.Draft,
                    structured_findings = "{}",
                    createdat = DateTime.UtcNow,
                    updatedat = DateTime.UtcNow
                };
                _context.radiologyreports.Add(report);
                await _context.SaveChangesAsync();
            }

            ViewData["ReportId"] = report.reportid;
            ViewData["ExamRequestId"] = examRequestId;
            ViewData["StudyInstanceUid"] = exam.studyinstanceuid;
            ViewData["PatientName"] = examWithPatient.PatientName;
            ViewData["Gender"] = examWithPatient.Gender;
            ViewData["BirthDate"] = examWithPatient.BirthDate?.ToString("dd MMM yyyy") ?? "N/A";

            // Calculate Age (Optional but professional)
            // Calculate Age using DateOnly
            if (examWithPatient.BirthDate.HasValue)
            {
                DateOnly today = DateOnly.FromDateTime(DateTime.Today);
                DateOnly dob = examWithPatient.BirthDate.Value;

                int age = today.Year - dob.Year;

                // Check if the birthday has occurred yet this year
                if (dob > today.AddYears(-age))
                {
                    age--;
                }

                ViewData["PatientAge"] = age;
            }

            ViewData["PatientSex"] = examWithPatient.Gender ?? "U";

            return View("~/Areas/Radiology/Views/Interpretation/Desktop.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetReport(int reportId)
        {
            var report = await _reportService.GetCurrentAsync(reportId);
            if (report == null)
            {
                return NotFound(new { message = "Report not found." });
            }

            return Ok(report);
        }

        [HttpPost]
        public async Task<IActionResult> AIAssist([FromBody] AIAssistRequestDto dto, CancellationToken ct)
        {
            if (dto == null || dto.ExamRequestId <= 0)
            {
                return BadRequest(new { message = "Invalid exam request." });
            }

            var examInfo = await (from exam in _context.radiologyexamrequests
                                  join room in _context.rooms on exam.roomid equals room.roomid into roomJoin
                                  from room in roomJoin.DefaultIfEmpty()
                                  where exam.id == dto.ExamRequestId
                                  select new
                                  {
                                      exam.studyinstanceuid,
                                      Modality = room != null ? room.modality : null
                                  }).FirstOrDefaultAsync(ct);

            if (examInfo == null)
            {
                return NotFound(new { message = "Exam request not found." });
            }

            if (!IsValidDicomUid(examInfo.studyinstanceuid))
            {
                return BadRequest(new { message = "Invalid or missing StudyInstanceUID." });
            }

            try
            {
                var result = await _aiAssistService.GenerateReportAsync(
                    examInfo.studyinstanceuid,
                    examInfo.Modality ?? "Unknown",
                    dto.PatientAge,
                    dto.PatientSex,
                    ct);

                return Ok(new
                {
                    impression = result.Impression,
                    narrative = result.Narrative,
                    imagesAnalyzed = result.ImagesAnalyzed,
                    disclaimer = result.Disclaimer
                });
            }
            catch (AIAssistUnavailableException ex)
            {
                _logger.LogWarning(ex, "AI Assist unavailable for exam {ExamRequestId}", dto.ExamRequestId);
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        message = ex.Message,
                        diagnostics = ex.OrthancDiagnostics != null
                            ? OrthancAccessHelper.BuildDiagnosticsDto(ex.OrthancDiagnostics)
                            : null
                    });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "AI Assist unavailable for exam {ExamRequestId}", dto.ExamRequestId);
                var status = MapAiAssistErrorStatus(ex.Message);
                return StatusCode(status, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI Assist failed for exam {ExamRequestId}", dto.ExamRequestId);
                return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveReport([FromBody] ReportSaveRequestDto dto)
        {
            var actorId = User.Identity?.Name ?? "system";
            var result = await _reportService.SaveAsync(dto.ReportId, dto, actorId);

            if (result.IsNotFound) return NotFound(new { message = result.Error });
            if (result.IsValidationError) return BadRequest(new { message = result.Error });
            if (result.Conflict != null) return Conflict(result.Conflict);

            return Ok(result.Report);
        }

        [HttpPost]
        public async Task<IActionResult> FinalizeReport(int reportId, [FromBody] FinalizeReportRequestDto dto)
        {
            var actorId = User.Identity?.Name ?? "system";
            var result = await _reportService.FinalizeAsync(reportId, dto, actorId);

            if (result.IsNotFound) return NotFound(new { message = result.Error });
            if (result.IsValidationError) return BadRequest(new { message = result.Error });
            if (result.Conflict != null) return Conflict(result.Conflict);

            var report = result.Report;
            var transition = await _workflowService.TransitionAsync(report.ExamRequestId, radiologyexamrequest.WorkflowStatuses.Finalized, actorId);
            if (!transition.Success)
            {
                return BadRequest(new { message = transition.Error, report });
            }

            return Ok(result.Report);
        }

        [HttpPost]
        public async Task<IActionResult> Transition([FromBody] TransitionRequestDto dto)
        {
            var actorId = User.Identity?.Name ?? "system";
            var result = await _workflowService.TransitionAsync(dto.ExamRequestId, dto.TargetStatus, actorId);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> ClinicalReviewLookup(string inhospid, string patientId)
        {
            var finalized = await _context.radiologyreports
                .Join(_context.radiologyexamrequests,
                    report => report.radrequestid,
                    exam => exam.id,
                    (report, exam) => new { report, exam })
                .Where(x =>
                    (x.report.is_locked || x.report.status == radiologyreport.ReportStatuses.Final) &&
                    (!string.IsNullOrWhiteSpace(inhospid) && x.report.inhospid == inhospid
                     || !string.IsNullOrWhiteSpace(patientId) && x.exam.patientid == patientId))
                .OrderByDescending(x => x.report.finalized_at ?? x.report.updatedat)
                .Select(x => new
                {
                    examRequestId = x.exam.id,
                    reportId = x.report.reportid,
                    studyInstanceUid = x.report.studyinstanceuid ?? x.exam.studyinstanceuid,
                    reportHtml = x.report.report_text ?? string.Empty,
                    impression = x.report.impression ?? string.Empty,
                    finalizedAt = x.report.finalized_at,
                    isFinalized = x.report.is_locked || x.report.status == radiologyreport.ReportStatuses.Final
                })
                .FirstOrDefaultAsync();

            if (finalized == null)
            {
                return NotFound(new { message = "No finalized radiology review available." });
            }

            return Ok(finalized);
        }

        [HttpGet]
        public async Task<IActionResult> ClinicalReviewWindow(int examRequestId)
        {
            var review = await (from report in _context.radiologyreports
                                join exam in _context.radiologyexamrequests on report.radrequestid equals exam.id
                                join pat in _context.KmuCharts on exam.patientid equals pat.ChrHealthId
                                where exam.id == examRequestId && (report.is_locked || report.status == radiologyreport.ReportStatuses.Final)
                                orderby report.finalized_at ?? report.updatedat descending
                                select new
                                {
                                    exam.id,
                                    studyuid = report.studyinstanceuid ?? exam.studyinstanceuid,
                                    reportHtml = report.report_text ?? string.Empty,
                                    impression = report.impression ?? string.Empty,
                                    finalizedAt = report.finalized_at,
                                    PatientName = pat.ChrPatientFirstname + " " + pat.ChrPatientMidname + " " + pat.ChrPatientLastname,
                                    Gender = pat.ChrSex,
                                    BirthDate = pat.ChrBirthDate
                                }).FirstOrDefaultAsync();

            if (review == null || !IsValidDicomUid(review.studyuid))
            {
                return NotFound("No finalized radiology review found.");
            }

            // View Data for IDs and Report
            ViewData["ExamRequestId"] = review.id;
            ViewData["StudyInstanceUid"] = review.studyuid;
            ViewData["ReportHtml"] = review.reportHtml;
            ViewData["Impression"] = review.impression;
            ViewData["FinalizedAt"] = review.finalizedAt;

            // New Patient View Data
            ViewData["PatientName"] = review.PatientName;
            ViewData["Gender"] = review.Gender;
            ViewData["BirthDate"] = review.BirthDate?.ToString("dd MMM yyyy") ?? "N/A";

            // Calculate Age
            if (review.BirthDate.HasValue)
            {
                DateOnly today = DateOnly.FromDateTime(DateTime.Today);
                DateOnly dob = review.BirthDate.Value;
                int age = today.Year - dob.Year;
                if (dob > today.AddYears(-age)) age--;
                ViewData["PatientAge"] = age;
            }

            return View("~/Areas/Radiology/Views/Interpretation/ClinicalReviewWindow.cshtml");
        }

        private static int MapAiAssistErrorStatus(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return StatusCodes.Status503ServiceUnavailable;
            }

            if (message.Contains("API key is not configured", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCodes.Status400BadRequest;
            }

            if (message.Contains("invalid or unauthorized", StringComparison.OrdinalIgnoreCase)
                || message.Contains("(401)", StringComparison.Ordinal))
            {
                return StatusCodes.Status401Unauthorized;
            }

            if (message.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
                || message.Contains("(429)", StringComparison.Ordinal))
            {
                return StatusCodes.Status429TooManyRequests;
            }

            return StatusCodes.Status503ServiceUnavailable;
        }

        private static bool IsValidDicomUid(string uid)
        {
            return !string.IsNullOrWhiteSpace(uid)
                && uid.Length <= 64
                && DicomUidRegex.IsMatch(uid);
        }
    }
}
