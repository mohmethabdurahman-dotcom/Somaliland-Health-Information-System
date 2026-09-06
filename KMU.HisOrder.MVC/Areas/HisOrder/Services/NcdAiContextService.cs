using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace KMU.HisOrder.MVC.Areas.HisOrder.Services
{
    public sealed class NcdAiContextService : INcdAiContextService
    {
        private readonly KMUContext _context;
        private readonly ILogger<NcdAiContextService> _logger;

        public NcdAiContextService(KMUContext context, ILogger<NcdAiContextService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IReadOnlyList<NcdAiContextEntry>> GetLatestContextAsync(
            string healthId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(healthId))
            {
                return Array.Empty<NcdAiContextEntry>();
            }

            var normalizedHealthId = healthId.Trim();

            var rows = await (
                from c in _context.kmu_ncd
                join a in _context.KmuNonMedicines on c.plancode equals a.ItemId
                join b in _context.KmuCoderefs on a.GroupCode equals b.RefCode
                where c.healthid == normalizedHealthId
                      && a.Status == '1'
                      && b.RefCodetype == "group_code"
                orderby c.createdate descending
                select new
                {
                    a.ItemType,
                    a.ItemName,
                    c.patient_answer,
                    c.createdate,
                    c.plancode
                }).ToListAsync(cancellationToken);

            var latestPerPlan = rows
                .GroupBy(r => r.plancode)
                .Select(g => g.First())
                .OrderBy(r => MapCategory(r.ItemType))
                .ThenBy(r => r.ItemName)
                .ToList();

            var entries = new List<NcdAiContextEntry>();
            foreach (var row in latestPerPlan)
            {
                var answer = ResolveAnswer(row.patient_answer, row.ItemName);
                if (string.IsNullOrWhiteSpace(answer))
                {
                    continue;
                }

                entries.Add(new NcdAiContextEntry
                {
                    Category = MapCategory(row.ItemType),
                    ItemName = row.ItemName ?? string.Empty,
                    Answer = answer,
                    RecordedDate = row.createdate
                });
            }

            _logger.LogInformation(
                "NCD AI context loaded {Count} entries for healthId={HealthId}",
                entries.Count,
                normalizedHealthId);

            return entries;
        }

        private static string ResolveAnswer(string patientAnswer, string itemName)
        {
            if (!string.IsNullOrWhiteSpace(patientAnswer))
            {
                return patientAnswer.Trim();
            }

            return string.IsNullOrWhiteSpace(itemName) ? string.Empty : itemName.Trim();
        }

        private static string MapCategory(string itemType)
        {
            return itemType switch
            {
                "10" => "Intake diagnosis",
                "11" => "Comorbidities",
                "12" => "Lifestyle",
                "17" => "Social background",
                "13" => "Hypertension",
                "14" => "Asthma/COPD",
                "15" => "Heart failure",
                "16" => "Diabetes",
                "0" => "NCD vitals",
                _ => "Other NCD"
            };
        }
    }
}
