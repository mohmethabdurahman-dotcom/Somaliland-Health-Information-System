using KMU.HisOrder.MVC.Areas.Radiology.Models;

namespace KMU.HisOrder.MVC.Areas.Radiology.Services
{
    public interface IAIAssistService
    {
        Task<AIAssistResult> GenerateReportAsync(
            string studyInstanceUid,
            string modality,
            int? patientAge,
            string patientSex,
            CancellationToken cancellationToken = default);
    }
}
