using KMU.HisOrder.MVC.Areas.HisOrder.Dtos;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;

namespace KMU.HisOrder.MVC.Areas.HisOrder.Services
{
    public interface IClinicNcdAiAssistService
    {
        Task<ClinicNcdAiAssistResult> GenerateAsync(
            ClinicNcdAiAssistRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
