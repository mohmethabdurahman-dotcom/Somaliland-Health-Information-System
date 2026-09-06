using System;

namespace KMU.HisOrder.MVC.Models.BloodBank
{
    public class BloodBankDispenseRecord
    {
        public long DispenseId { get; set; }
        public long UnitId { get; set; }
        /// <summary>Donor who provided the unit (audit; also on unit.DonorId).</summary>
        public long? SourceDonorId { get; set; }
        public string? DonationSource { get; set; }
        public long? RequestId { get; set; }
        public string PatientId { get; set; } = null!;
        public string Inhospid { get; set; } = null!;
        public string PatientName { get; set; } = null!;
        public string CollectorName { get; set; } = null!;
        public string WardDestination { get; set; } = null!;
        public int UnitsReleased { get; set; }
        public string SerialNumbers { get; set; } = null!;
        public DateTime DispenseDateTime { get; set; }
        public string DispensingStaffId { get; set; } = null!;
        public string DispensingStaffName { get; set; } = null!;
        public string CreateUser { get; set; } = null!;
        public DateTime CreateDate { get; set; }

        public virtual BloodBankBloodUnit Unit { get; set; } = null!;
        public virtual BloodBankRequest? Request { get; set; }
    }
}
