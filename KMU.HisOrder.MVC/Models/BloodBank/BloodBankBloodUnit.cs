using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KMU.HisOrder.MVC.Models.BloodBank
{
    public class BloodBankBloodUnit
    {
        public long UnitId { get; set; }
        public string SerialNumber { get; set; } = null!;
        public long DonorId { get; set; }
        public string BloodType { get; set; } = null!;
        public string ComponentType { get; set; } = null!;
        public string Status { get; set; } = UnitStatuses.PendingScreening;
        public int Quantity { get; set; } = 1;
        public int? VolumeMl { get; set; }
        public string? StorageLocation { get; set; }
        public DateTime DonationDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string ScreeningResult { get; set; } = ScreeningResults.Pending;
        public string? PatientId { get; set; }
        public long? RequestId { get; set; }
        public string CreateUser { get; set; } = null!;
        public DateTime CreateDate { get; set; }
        public string? ModifyUser { get; set; }
        public DateTime? ModifyDate { get; set; }

        [ForeignKey(nameof(DonorId))]
        public virtual BloodBankDonor Donor { get; set; } = null!;
        public virtual BloodBankRequest? Request { get; set; }
    }
}
