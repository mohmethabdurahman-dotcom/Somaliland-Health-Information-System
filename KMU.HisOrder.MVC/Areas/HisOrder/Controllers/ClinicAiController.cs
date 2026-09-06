using KMU.HisOrder.MVC.Areas.HisOrder.Dtos;
using KMU.HisOrder.MVC.Areas.HisOrder.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KMU.HisOrder.MVC.Areas.HisOrder.Controllers
{
    [CheckClinicSessionAttribute]
    [CheckSessionTimeOutAttribute]
    [Area("HisOrder")]
    [Authorize(Roles = "HisOrder")]
    public sealed class ClinicAiController : Controller
    {
        private static readonly HashSet<string> NcdDeptCodes = new(StringComparer.Ordinal)
        {
            "3000", "3001", "3002", "6000", "6001", "6002", "6003"
        };

        private readonly IClinicNcdAiAssistService _ncdAiAssistService;
        private readonly ILogger<ClinicAiController> _logger;

        public ClinicAiController(
            IClinicNcdAiAssistService ncdAiAssistService,
            ILogger<ClinicAiController> logger)
        {
            _ncdAiAssistService = ncdAiAssistService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> NcdAssist(
            [FromBody] ClinicNcdAiAssistRequestDto dto,
            CancellationToken ct)
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Invalid request." });
            }

            var deptCode = (dto.DeptCode ?? string.Empty).Trim();
            if (!NcdDeptCodes.Contains(deptCode))
            {
                return BadRequest(new { message = "NCD AI Assist is only available for NCD departments." });
            }

            if (string.IsNullOrWhiteSpace(dto.HealthId))
            {
                return BadRequest(new { message = "Health ID is required." });
            }

            try
            {
                var result = await _ncdAiAssistService.GenerateAsync(dto, ct);
                return Ok(new { content = result.Content });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "NCD AI Assist unavailable for healthId={HealthId}", dto.HealthId);
                var status = ex.Message.Contains("OpenAI API key", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("Cannot reach OpenAI", StringComparison.OrdinalIgnoreCase)
                    ? StatusCodes.Status503ServiceUnavailable
                    : StatusCodes.Status502BadGateway;
                return StatusCode(status, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NCD AI Assist failed for healthId={HealthId}", dto.HealthId);
                return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
            }
        }
    }
}
