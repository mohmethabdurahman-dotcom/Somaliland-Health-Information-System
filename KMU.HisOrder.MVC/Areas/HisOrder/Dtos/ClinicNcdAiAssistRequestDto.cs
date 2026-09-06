namespace KMU.HisOrder.MVC.Areas.HisOrder.Dtos
{
    public sealed class ClinicNcdAiAssistRequestDto
    {
        public string Inhospid { get; set; } = string.Empty;

        public string HealthId { get; set; } = string.Empty;

        public string DeptCode { get; set; } = string.Empty;

        public int? PatientAge { get; set; }

        public string PatientSex { get; set; } = string.Empty;

        public string ClinicRemarkHtml { get; set; } = string.Empty;

        public string ManagementHtml { get; set; } = string.Empty;

        public IReadOnlyList<string> IcdCodes { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> Medications { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> NonMedOrders { get; set; } = Array.Empty<string>();

        public object PhysicalSigns { get; set; }
    }
}
