using KMU.HisOrder.MVC.Areas.HisOrder.Models;

namespace KMU.HisOrder.MVC.Areas.HisOrder.Services
{
    public interface INcdAiContextService
    {
        Task<IReadOnlyList<NcdAiContextEntry>> GetLatestContextAsync(
            string healthId,
            CancellationToken cancellationToken = default);
    }
}
