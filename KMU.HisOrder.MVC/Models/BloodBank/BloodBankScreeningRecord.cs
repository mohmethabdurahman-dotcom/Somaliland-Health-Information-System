using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KMU.HisOrder.MVC.Models.BloodBank
{
    public class BloodBankScreeningRecord
    {
        public long ScreeningId { get; set; }
        public long? DonorId { get; set; }
        public long? UnitId { get; set; }
        public string? Notes { get; set; }
        public string StaffUserId { get; set; } = null!;
        public string StaffName { get; set; } = null!;
        public DateTime ScreeningDateTime { get; set; }
        public string HivResult { get; set; } = null!;
        public string HepBResult { get; set; } = null!;
        public string HepCResult { get; set; } = null!;
        public string SyphilisResult { get; set; } = null!;
        public string MalariaResult { get; set; } = null!;
        public string OverallResult { get; set; } = null!;
        public string CreateUser { get; set; } = null!;
        public DateTime CreateDate { get; set; }

        [ForeignKey(nameof(DonorId))]
        public virtual BloodBankDonor? Donor { get; set; }
        [ForeignKey(nameof(UnitId))]
        public virtual BloodBankBloodUnit? Unit { get; set; }
    }
}
