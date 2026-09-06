namespace KMU.HisOrder.MVC.Models
{
    public class MedicalAdministration
    {
        public int Id { get; set; }
        public string healthId { get; set; }
        public string inhospId { get; set; }
        public string medicalType  { get; set; }
        public string shift { get; set; }
        public string? medCode { get; set; }
        public string? medDes { get; set; }
        public string? milkAmount { get; set; }
        public string administeredBy { get; set; }
        public DateTime admisteredAt { get; set; }

    }
}
