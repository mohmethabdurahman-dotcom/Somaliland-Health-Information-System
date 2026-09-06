using System;

namespace KMU.HisOrder.MVC.Models.BloodBank
{
    public class BloodBankDonor
    {
        public long DonorId { get; set; }
        public string FirstName { get; set; } = null!;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = null!;
        public int? Age { get; set; }
        public string? JobDescription { get; set; }
        public string Gender { get; set; } = null!;
        /// <summary>Pre-donation malaria rapid test (Pass/Fail), recorded at registration.</summary>
        public string? PreDonationMalariaResult { get; set; }
        public string Phone { get; set; } = null!;
        public string? NationalId { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? DonorNotes { get; set; }
        public DateTime? LastDonationDate { get; set; }
        public string BloodType { get; set; } = null!;
        public string DonationType { get; set; } = null!;
        public string? PatientId { get; set; }
        public string? Inhospid { get; set; }
        public long? RequestId { get; set; }
        /// <summary>Registered → Screening → Passed | Failed</summary>
        public string Status { get; set; } = DonorStatuses.Registered;
        public string ScreeningResult { get; set; } = ScreeningResults.Pending;
        public string? ScreeningNotes { get; set; }
        public string CreateUser { get; set; } = null!;
        public DateTime CreateDate { get; set; }
        public string? ModifyUser { get; set; }
        public DateTime? ModifyDate { get; set; }

        public virtual BloodBankRequest? Request { get; set; }
    }
}
