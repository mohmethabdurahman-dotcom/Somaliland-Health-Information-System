using System.Text.Json;

namespace KMU.HisOrder.MVC.Areas.Radiology.Dtos
{
    public sealed class ReportResponseDto
    {
        public int ReportId { get; init; }
        public int ExamRequestId { get; init; }
        public string Status { get; init; } = string.Empty;
        public string Impression { get; init; }
        public string ReportBodyHtml { get; init; } = string.Empty;
        public JsonDocument StructuredFindings { get; init; } = JsonDocument.Parse("{}");
        public DateTime UpdatedAt { get; init; }
        public string UpdatedBy { get; init; } = string.Empty;
        public string ConcurrencyToken { get; init; } = string.Empty;
    }

    public sealed class ConflictResponseDto
    {
        public string ErrorCode { get; init; } = "ConcurrencyConflict";
        public string Message { get; init; } = string.Empty;
        public string ServerConcurrencyToken { get; init; } = string.Empty;
        public ReportResponseDto ServerVersion { get; init; }
    }
}
