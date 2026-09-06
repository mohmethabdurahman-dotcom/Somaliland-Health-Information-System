using KMU.HisOrder.MVC.Areas.BloodBank.Filters;
using KMU.HisOrder.MVC.Areas.BloodBank.Models;
using KMU.HisOrder.MVC.Models;
using Microsoft.AspNetCore.Mvc;

namespace KMU.HisOrder.MVC.Areas.BloodBank.Controllers
{
    /// <summary>HIS integration: POST blood requests into the Blood Bank queue.</summary>
    [Area("BloodBank")]
    [Route("BloodBank/api/[controller]")]
    [ApiController]
    [BloodBankAccess]
    public class BloodBankApiController : ControllerBase
    {
        private readonly BloodBankService _service;

        public BloodBankApiController(BloodBankService service)
        {
            _service = service;
        }

        [HttpPost("requests")]
        public async Task<IActionResult> CreateRequest([FromBody] HisBloodRequestApiModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.Identity?.Name
                ?? "HIS";

            var result = await _service.CreateRequestFromApiAsync(model, userId);
            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message, requestId = result.RequestId });
        }
    }
}
