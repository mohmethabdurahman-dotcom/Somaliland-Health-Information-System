using System;

namespace KMU.HisOrder.MVC.Models.BloodBank
{
    public class BloodBankRequest
    {
        public long RequestId { get; set; }
        public long? Orderplanid { get; set; }
        public string PatientId { get; set; } = null!;
        public string Inhospid { get; set; } = null!;
        public string PatientName { get; set; } = null!;
        public string? Ward { get; set; }
        public string? BedLocation { get; set; }
        public string RequestingDoctorId { get; set; } = null!;
        public string RequestingDoctorName { get; set; } = null!;
        public string BloodType { get; set; } = null!;
        public string ComponentType { get; set; } = null!;
        public int UnitsRequested { get; set; }
        public string UrgencyLevel { get; set; } = null!;
        public string PatientType { get; set; } = null!;
        public string Status { get; set; } = RequestStatuses.Pending;
        public DateTime RequestDateTime { get; set; }
        public string? RejectReason { get; set; }
        public string CreateUser { get; set; } = null!;
        public DateTime CreateDate { get; set; }
        public string? ModifyUser { get; set; }
        public DateTime? ModifyDate { get; set; }
    }
}
