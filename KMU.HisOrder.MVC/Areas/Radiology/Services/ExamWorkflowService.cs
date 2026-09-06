using KMU.HisOrder.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace KMU.HisOrder.MVC.Areas.Radiology.Services
{
    public interface IExamWorkflowService
    {
        Task<TransitionResult> TransitionAsync(int examRequestId, string targetStatus, string actorId, CancellationToken ct = default);
        bool CanTransition(string currentStatus, string targetStatus);
    }

    public sealed class ExamWorkflowService : IExamWorkflowService
    {
        private static readonly Dictionary<string, string> AllowedTransitions = new(StringComparer.OrdinalIgnoreCase)
        {
            [radiologyexamrequest.WorkflowStatuses.Ordered] = radiologyexamrequest.WorkflowStatuses.Scheduled,
            [radiologyexamrequest.WorkflowStatuses.Scheduled] = radiologyexamrequest.WorkflowStatuses.InProgress,
            [radiologyexamrequest.WorkflowStatuses.InProgress] = radiologyexamrequest.WorkflowStatuses.Interpreted,
            [radiologyexamrequest.WorkflowStatuses.Interpreted] = radiologyexamrequest.WorkflowStatuses.Finalized
        };

        private readonly KMUContext _context;

        public ExamWorkflowService(KMUContext context)
        {
            _context = context;
        }

        public bool CanTransition(string currentStatus, string targetStatus)
        {
            if (string.IsNullOrWhiteSpace(currentStatus) || string.IsNullOrWhiteSpace(targetStatus))
            {
                return false;
            }

            return AllowedTransitions.TryGetValue(currentStatus, out var allowedTarget)
                && string.Equals(allowedTarget, targetStatus, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<TransitionResult> TransitionAsync(int examRequestId, string targetStatus, string actorId, CancellationToken ct = default)
        {
            var exam = await _context.radiologyexamrequests.FirstOrDefaultAsync(x => x.id == examRequestId, ct);
            if (exam == null)
            {
                return TransitionResult.Failed("Exam request not found.");
            }

            var current = NormalizeStatus(exam.status);
            var desired = NormalizeStatus(targetStatus);
            if (!CanTransition(current, desired))
            {
                return TransitionResult.Failed($"Transition {current} -> {desired} is not allowed.");
            }

            var preconditionError = await ValidatePreconditionsAsync(exam, desired, actorId, ct);
            if (!string.IsNullOrEmpty(preconditionError))
            {
                return TransitionResult.Failed(preconditionError);
            }

            exam.status = desired;
            await _context.SaveChangesAsync(ct);
            return TransitionResult.Succeeded(exam.id, current, desired);
        }

        private async Task<string> ValidatePreconditionsAsync(radiologyexamrequest exam, string targetStatus, string actorId, CancellationToken ct)
        {
            if (string.Equals(targetStatus, radiologyexamrequest.WorkflowStatuses.Interpreted, StringComparison.OrdinalIgnoreCase))
            {
                var hasDraft = await _context.radiologyreports.AnyAsync(
                    r => r.radrequestid == exam.id &&
                         !string.IsNullOrEmpty(r.report_text),
                    ct);

                if (!hasDraft)
                {
                    return "Cannot mark as Interpreted without a saved report draft.";
                }
            }

            if (string.Equals(targetStatus, radiologyexamrequest.WorkflowStatuses.Finalized, StringComparison.OrdinalIgnoreCase))
            {
                var report = await _context.radiologyreports
                    .Where(r => r.radrequestid == exam.id)
                    .OrderByDescending(r => r.updatedat)
                    .FirstOrDefaultAsync(ct);

                if (report == null || string.IsNullOrWhiteSpace(report.impression))
                {
                    return "Cannot finalize exam without an impression.";
                }

                if (string.IsNullOrWhiteSpace(report.radiologistid) && string.IsNullOrWhiteSpace(actorId))
                {
                    return "Cannot finalize exam without radiologist signature metadata.";
                }
            }

            return string.Empty;
        }

        private static string NormalizeStatus(string status)
        {
            return status switch
            {
                "PENDING" => radiologyexamrequest.WorkflowStatuses.Ordered,
                "SCHEDULED" => radiologyexamrequest.WorkflowStatuses.Scheduled,
                "COMPLETED" => radiologyexamrequest.WorkflowStatuses.Interpreted,
                "FINALIZED" => radiologyexamrequest.WorkflowStatuses.Finalized,
                _ => status ?? string.Empty
            };
        }
    }

    public sealed class TransitionResult
    {
        public bool Success { get; private set; }
        public string Error { get; private set; }
        public int ExamRequestId { get; private set; }
        public string FromStatus { get; private set; }
        public string ToStatus { get; private set; }

        public static TransitionResult Succeeded(int examRequestId, string fromStatus, string toStatus)
        {
            return new TransitionResult
            {
                Success = true,
                ExamRequestId = examRequestId,
                FromStatus = fromStatus,
                ToStatus = toStatus
            };
        }

        public static TransitionResult Failed(string error)
        {
            return new TransitionResult
            {
                Success = false,
                Error = error
            };
        }
    }
}
