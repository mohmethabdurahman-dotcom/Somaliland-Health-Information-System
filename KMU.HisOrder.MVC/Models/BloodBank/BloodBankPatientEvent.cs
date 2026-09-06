using System;

namespace KMU.HisOrder.MVC.Models.BloodBank
{
    public class BloodBankPatientEvent
    {
        public long EventId { get; set; }
        public string PatientId { get; set; } = null!;
        public string Inhospid { get; set; } = null!;
        public string EventType { get; set; } = null!;
        public string EventDescription { get; set; } = null!;
        public DateTime EventDateTime { get; set; }
        public string CreateUser { get; set; } = null!;
        public DateTime CreateDate { get; set; }
    }
}
