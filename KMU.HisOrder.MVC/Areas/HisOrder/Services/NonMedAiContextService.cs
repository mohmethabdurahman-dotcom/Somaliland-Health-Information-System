using KMU.HisOrder.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace KMU.HisOrder.MVC.Areas.HisOrder.Services
{
    public sealed class NonMedAiContextService : INonMedAiContextService
    {
        private readonly KMUContext _context;
        private readonly ILogger<NonMedAiContextService> _logger;

        public NonMedAiContextService(KMUContext context, ILogger<NonMedAiContextService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IReadOnlyList<string>> GetOrderSummariesAsync(
            string inhospid,
            string healthId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(inhospid) || string.IsNullOrWhiteSpace(healthId))
            {
                return Array.Empty<string>();
            }

            var normalizedInhospid = inhospid.Trim();
            var normalizedHealthId = healthId.Trim();

            var orders = await _context.Hisorderplans
                .AsNoTracking()
                .Where(c => c.Inhospid == normalizedInhospid
                            && c.HealthId == normalizedHealthId
                            && c.HplanType != "Med"
                            && c.HplanType != "ICD"
                            && c.DcDate == null)
                .OrderBy(c => c.SeqNo)
                .ToListAsync(cancellationToken);

            if (orders.Count == 0)
            {
                return Array.Empty<string>();
            }

            var locationNames = await _context.KmuCoderefs
                .AsNoTracking()
                .Where(c => c.RefCodetype == "NonMedLocation")
                .ToDictionaryAsync(c => c.RefCode, c => c.RefName, cancellationToken);

            var summaries = orders
                .Select(o => FormatOrderSummary(o, locationNames))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            _logger.LogInformation(
                "Non-med AI context loaded {Count} orders for inhospid={Inhospid}",
                summaries.Count,
                normalizedInhospid);

            return summaries;
        }

        private static string FormatOrderSummary(Hisorderplan order, IReadOnlyDictionary<string, string> locationNames)
        {
            var planCode = (order.PlanCode ?? string.Empty).Trim();
            var planDes = (order.PlanDes ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(planCode) && string.IsNullOrWhiteSpace(planDes))
            {
                return string.Empty;
            }

            var hplanType = (order.HplanType ?? string.Empty).Trim();
            var qty = order.QtyDose.HasValue ? order.QtyDose.Value.ToString("0.##") : string.Empty;
            var locationCode = (order.LocationCode ?? string.Empty).Trim();
            var location = locationNames.TryGetValue(locationCode, out var locationName)
                ? locationName
                : locationCode;
            var remark = (order.Remark ?? string.Empty).Trim();

            return string.Join(", ", new[]
            {
                hplanType,
                planCode,
                planDes,
                !string.IsNullOrWhiteSpace(qty) ? $"qty:{qty}" : string.Empty,
                !string.IsNullOrWhiteSpace(location) ? $"location:{location}" : string.Empty,
                !string.IsNullOrWhiteSpace(remark) ? $"remark:{remark}" : string.Empty
            }.Where(part => !string.IsNullOrWhiteSpace(part)));
        }
    }
}
