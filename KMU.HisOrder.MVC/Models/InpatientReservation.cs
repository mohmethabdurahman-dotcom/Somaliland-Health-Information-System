namespace KMU.HisOrder.MVC.Models
{
    public class InpatientReservation
    {
        public DateTime reserveDate { get; set; }
        public string healthId { get; set; }
        public string inhospId { get; set; }
        public string department { get; set; }
        public string wardId { get; set; }
        public string bedId { get; set; }
        public string status { get; set; }
        public string? dischargeType { get; set; }
        public DateTime? dischargeDate { get; set; }
        public string? dischargeApprover { get; set; }
        public string createBy { get; set; }
        public DateTime createAt { get; set; }
        public string? modifyBy { get; set; }
        public DateTime? modifyAt { get; set; }
        public string referral_place { get; set; }
        public string transfer_place { get; set; }

        public string? doctor { get; set; }
        public string? nurse { get; set; }
    }
}
