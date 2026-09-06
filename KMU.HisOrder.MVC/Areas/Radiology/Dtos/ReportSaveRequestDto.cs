using System.Text.Json;

namespace KMU.HisOrder.MVC.Areas.Radiology.Dtos
{
    public sealed class ReportSaveRequestDto
    {
        public int ReportId { get; init; }
        public string Status { get; init; } = "Draft";
        public string Impression { get; init; }
        public string ReportBodyHtml { get; init; } = string.Empty;
        public JsonDocument StructuredFindings { get; init; } = JsonDocument.Parse("{}");
        public string ConcurrencyToken { get; init; } = string.Empty;
    }

    public sealed class FinalizeReportRequestDto
    {
        public string Impression { get; init; } = string.Empty;
        public string RadiologistSignature { get; init; } = string.Empty;
        public string ConcurrencyToken { get; init; } = string.Empty;
    }
}
