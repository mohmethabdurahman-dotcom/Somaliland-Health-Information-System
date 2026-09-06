namespace KMU.HisOrder.MVC.Areas.Radiology.Dtos
{
    public sealed class TransitionRequestDto
    {
        public int ExamRequestId { get; init; }
        public string TargetStatus { get; init; } = string.Empty;
    }
}
