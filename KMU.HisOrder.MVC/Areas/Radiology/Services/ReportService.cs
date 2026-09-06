using System.Text.Json;
using KMU.HisOrder.MVC.Areas.Radiology.Dtos;
using KMU.HisOrder.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace KMU.HisOrder.MVC.Areas.Radiology.Services
{
    public interface IReportService
    {
        Task<ReportSaveResult> SaveAsync(int reportId, ReportSaveRequestDto dto, string actorId, CancellationToken ct = default);
        Task<ReportFinalizeResult> FinalizeAsync(int reportId, FinalizeReportRequestDto dto, string actorId, CancellationToken ct = default);
        Task<ReportResponseDto> GetCurrentAsync(int reportId, CancellationToken ct = default);
    }

    public sealed class ReportService : IReportService
    {
        private readonly KMUContext _context;

        public ReportService(KMUContext context)
        {
            _context = context;
        }

        public async Task<ReportSaveResult> SaveAsync(int reportId, ReportSaveRequestDto dto, string actorId, CancellationToken ct = default)
        {
            var report = await _context.radiologyreports.FirstOrDefaultAsync(x => x.reportid == reportId, ct);
            if (report == null)
            {
                return ReportSaveResult.NotFound("Report not found.");
            }

            if (report.is_locked)
            {
                return ReportSaveResult.ValidationFailed("Finalized report is read-only.");
            }

            var clientToken = ParseToken(dto.ConcurrencyToken);
            if (clientToken == 0 || clientToken != report.xmin)
            {
                return ReportSaveResult.FromConflict(await BuildConflictAsync(report, "Report has been modified by another user.", ct));
            }

            report.report_text = dto.ReportBodyHtml;
            report.impression = dto.Impression;
            report.status = dto.Status;
            report.structured_findings = dto.StructuredFindings.RootElement.GetRawText();
            report.updatedat = DateTime.UtcNow;
            report.radiologistid = string.IsNullOrWhiteSpace(actorId) ? report.radiologistid : actorId;

            await _context.SaveChangesAsync(ct);
            await PersistVersionAndAuditAsync(report, actorId, "SAVE", ct);

            return ReportSaveResult.Saved(await MapReportAsync(report.reportid, ct));
        }

        public async Task<ReportFinalizeResult> FinalizeAsync(int reportId, FinalizeReportRequestDto dto, string actorId, CancellationToken ct = default)
        {
            var report = await _context.radiologyreports.FirstOrDefaultAsync(x => x.reportid == reportId, ct);
            if (report == null)
            {
                return ReportFinalizeResult.NotFound("Report not found.");
            }

            var clientToken = ParseToken(dto.ConcurrencyToken);
            if (clientToken == 0 || clientToken != report.xmin)
            {
                return ReportFinalizeResult.FromConflict(await BuildConflictAsync(report, "Report has been modified by another user.", ct));
            }

            if (string.IsNullOrWhiteSpace(dto.Impression))
            {
                return ReportFinalizeResult.ValidationFailed("Impression is required before finalization.");
            }

            if (string.IsNullOrWhiteSpace(dto.RadiologistSignature))
            {
                return ReportFinalizeResult.ValidationFailed("Radiologist signature is required.");
            }

            report.status = radiologyreport.ReportStatuses.Final;
            report.impression = dto.Impression;
            report.radiologistid = dto.RadiologistSignature;
            report.is_locked = true;
            report.finalized_at = DateTime.UtcNow;
            report.signedat = DateTime.UtcNow;
            report.updatedat = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            await PersistVersionAndAuditAsync(report, actorId, "FINALIZE", ct);
            return ReportFinalizeResult.Saved(await MapReportAsync(report.reportid, ct));
        }

        public async Task<ReportResponseDto> GetCurrentAsync(int reportId, CancellationToken ct = default)
        {
            return await MapReportAsync(reportId, ct);
        }

        private async Task PersistVersionAndAuditAsync(radiologyreport report, string actorId, string eventType, CancellationToken ct)
        {
            var currentVersion = await _context.radiologyreportversions
                .Where(v => v.report_id == report.reportid)
                .MaxAsync(v => (int?)v.version_no, ct) ?? 0;

            var payload = JsonSerializer.Serialize(new
            {
                report.reportid,
                report.radrequestid,
                report.status,
                report.impression,
                report.report_text,
                structured_findings = report.structured_findings,
                report.is_locked,
                report.finalized_at,
                report.updatedat
            });

            _context.radiologyreportversions.Add(new radiologyreportversion
            {
                report_id = report.reportid,
                version_no = currentVersion + 1,
                snapshot = payload,
                created_by = actorId ?? "system",
                created_at = DateTime.UtcNow
            });

            _context.radiologyreportaudittrails.Add(new radiologyreportaudittrail
            {
                report_id = report.reportid,
                event_type = eventType,
                payload = payload,
                actor_id = actorId ?? "system",
                created_at = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(ct);
        }

        private async Task<ConflictResponseDto> BuildConflictAsync(radiologyreport report, string message, CancellationToken ct)
        {
            var mapped = await MapReportAsync(report.reportid, ct);
            return new ConflictResponseDto
            {
                Message = message,
                ServerConcurrencyToken = report.xmin.ToString(),
                ServerVersion = mapped
            };
        }

        private async Task<ReportResponseDto> MapReportAsync(int reportId, CancellationToken ct)
        {
            var report = await _context.radiologyreports.FirstOrDefaultAsync(x => x.reportid == reportId, ct);
            if (report == null)
            {
                return null;
            }

            return new ReportResponseDto
            {
                ReportId = report.reportid,
                ExamRequestId = report.radrequestid,
                Status = report.status ?? radiologyreport.ReportStatuses.Draft,
                Impression = report.impression,
                ReportBodyHtml = report.report_text ?? string.Empty,
                StructuredFindings = JsonDocument.Parse(report.structured_findings ?? "{}"),
                UpdatedAt = report.updatedat,
                UpdatedBy = report.radiologistid ?? string.Empty,
                ConcurrencyToken = report.xmin.ToString()
            };
        }

        private static uint ParseToken(string token)
        {
            return uint.TryParse(token, out var parsed) ? parsed : 0;
        }
    }

    public sealed class ReportSaveResult
    {
        public bool Success { get; private set; }
        public bool IsNotFound { get; private set; }
        public bool IsValidationError { get; private set; }
        public ConflictResponseDto Conflict { get; private set; }
        public ReportResponseDto Report { get; private set; }
        public string Error { get; private set; }

        public static ReportSaveResult Saved(ReportResponseDto report) => new() { Success = true, Report = report };
        public static ReportSaveResult NotFound(string error) => new() { IsNotFound = true, Error = error };
        public static ReportSaveResult ValidationFailed(string error) => new() { IsValidationError = true, Error = error };
        public static ReportSaveResult FromConflict(ConflictResponseDto conflict) => new() { Conflict = conflict, Error = conflict.Message };
    }

    public sealed class ReportFinalizeResult
    {
        public bool Success { get; private set; }
        public bool IsNotFound { get; private set; }
        public bool IsValidationError { get; private set; }
        public ConflictResponseDto Conflict { get; private set; }
        public ReportResponseDto Report { get; private set; }
        public string Error { get; private set; }

        public static ReportFinalizeResult Saved(ReportResponseDto report) => new() { Success = true, Report = report };
        public static ReportFinalizeResult NotFound(string error) => new() { IsNotFound = true, Error = error };
        public static ReportFinalizeResult ValidationFailed(string error) => new() { IsValidationError = true, Error = error };
        public static ReportFinalizeResult FromConflict(ConflictResponseDto conflict) => new() { Conflict = conflict, Error = conflict.Message };
    }
}
