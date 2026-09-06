namespace KMU.HisOrder.MVC.Areas.Radiology.Dtos
{
    public sealed class AIAssistRequestDto
    {
        public int ExamRequestId { get; set; }
        public int? PatientAge { get; set; }
        public string PatientSex { get; set; }
    }
}
