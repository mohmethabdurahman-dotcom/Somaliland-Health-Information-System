namespace KMU.HisOrder.MVC.Areas.HisOrder.Services
{
    public interface INonMedAiContextService
    {
        Task<IReadOnlyList<string>> GetOrderSummariesAsync(
            string inhospid,
            string healthId,
            CancellationToken cancellationToken = default);
    }
}
