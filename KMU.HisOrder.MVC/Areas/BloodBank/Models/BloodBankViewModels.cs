using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using KMU.HisOrder.MVC.Models;
using KMU.HisOrder.MVC.Models.BloodBank;

namespace KMU.HisOrder.MVC.Areas.BloodBank.Models
{
    /// <summary>Lookup lists for MR-style donor registration form.</summary>
    public class DonorRegistrationLookups
    {
        public List<EnumClass.EnumGender> GenderList { get; set; } = new();
        public List<KmuCoderef> NationPhoneList { get; set; } = new();
        public List<KmuCoderef> AreaList { get; set; } = new();
    }

    public static class DonorPhoneHelper
    {
        public static void SplitPhone(string? phone, out string national, out string area, out string mobile)
        {
            national = "";
            area = "";
            mobile = "";
            if (string.IsNullOrWhiteSpace(phone)) return;

            var parts = phone.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                national = parts[0];
                area = parts[1];
                mobile = parts[2];
            }
            else if (parts.Length == 2)
            {
                area = parts[0];
                mobile = parts[1];
            }
            else
            {
                mobile = parts[0];
            }
        }

        public static string ComposePhone(string? national, string? area, string? mobile)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(national)) parts.Add(national.Trim());
            if (!string.IsNullOrWhiteSpace(area)) parts.Add(area.Trim());
            if (!string.IsNullOrWhiteSpace(mobile)) parts.Add(mobile.Trim());
            return string.Join(" ", parts);
        }

        public static int? AgeFromBirthDate(DateOnly? birthDate)
        {
            if (!birthDate.HasValue) return null;
            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - birthDate.Value.Year;
            if (birthDate.Value > today.AddYears(-age)) age--;
            return age < 1 ? 1 : age;
        }
    }
    public class BloodBankPatientPanelViewModel
    {
        public string PatientId { get; set; } = "";
        public string PatientName { get; set; } = "";
        public string Ward { get; set; } = "";
        public string BedLocation { get; set; } = "";
        public string RequestingDoctor { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public DateTime? RequestDateTime { get; set; }
        public string BloodType { get; set; } = "";
        public string ComponentType { get; set; } = "";
        public int UnitsRequested { get; set; }
        public string UrgencyLevel { get; set; } = "";
        public string PatientType { get; set; } = "";
        public long? RequestId { get; set; }
        public string Inhospid { get; set; } = "";
    }

    /// <summary>Stage 1 — donor identity only; no bag serial.</summary>
    public class DonorRegistrationViewModel
    {
        public long? DonorId { get; set; }

        [Required] public string FirstName { get; set; } = "";
        public string? MiddleName { get; set; }
        [Required] public string LastName { get; set; } = "";
        [Range(1, 120)] public int? Age { get; set; }
        public DateOnly? BirthDate { get; set; }
        /// <summary>Date | Age — mirrors medical record registration.</summary>
        public string AgeType { get; set; } = "Date";
        public string? JobDescription { get; set; }
        [Required] public string Gender { get; set; } = "";
        [Required] public string Phone { get; set; } = "";
        public string? NationalPhone { get; set; }
        public string? AreaPhone { get; set; }
        public string? MobilePhone { get; set; }
        public string? NationalId { get; set; }
        public string? AreaCode { get; set; }
        public string? Address { get; set; }
        public bool RefugeeFlag { get; set; }
        [Required] public string BloodType { get; set; } = "";
        [Required] public string DonationType { get; set; } = DonationTypes.Volunteer;
        public string? PatientId { get; set; }
        public string? Inhospid { get; set; }
        public long? RequestId { get; set; }

        public BloodBankPatientPanelViewModel? PatientPanel { get; set; }

        /* Registration vitals — blood pressure only; pulse/temp/Hb/CBC at screening */
        public string? PreDonationBloodPressure { get; set; }
        /// <summary>Malaria rapid test at registration (Pass/Fail).</summary>
        public string? PreDonationMalariaResult { get; set; }
    }

    public class PreDonationDeferralViewModel
    {
        [Required] public string FirstName { get; set; } = "";
        public string? MiddleName { get; set; }
        [Required] public string LastName { get; set; } = "";
        public int? Age { get; set; }
        public string? JobDescription { get; set; }
        [Required] public string Gender { get; set; } = "";
        [Required] public string Phone { get; set; } = "";
        public string? Hemoglobin { get; set; }
        public string? BloodPressure { get; set; }
        public string? Wbc { get; set; }
        public string? Rbc { get; set; }
        public string? Platelet { get; set; }
        public string? DeferralReason { get; set; }
        public string[]? DeferralFlags { get; set; }
    }

    public class DonorListItemViewModel
    {
        public long DonorId { get; set; }
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string BloodType { get; set; } = "";
        public string DonationType { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime RegisteredAt { get; set; }
        public string? PatientId { get; set; }
        /// <summary>Visits linked by the same phone (lifetime).</summary>
        public int LifetimeVisits { get; set; }
        public int SuccessfulDonations { get; set; }
    }

    public class DonorHistorySearchPageViewModel
    {
        public string? Query { get; set; }
        public List<DonorHistorySearchHitViewModel> Results { get; set; } = new();
        public bool HasSearched { get; set; }
    }

    public class DonorHistorySearchHitViewModel
    {
        public long PrimaryDonorId { get; set; }
        public string BloodBankDonorId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? NationalId { get; set; }
        public string Gender { get; set; } = "";
        public string BloodType { get; set; } = "";
        public int LifetimeVisits { get; set; }
        public int SuccessfulDonations { get; set; }
        public DateTime? LastVisit { get; set; }
        public string LastStatus { get; set; } = "";
    }

    public class DonorSearchResultViewModel
    {
        public bool EmptyParam { get; set; }
        public bool Truncated { get; set; }
        public int ResultCount => Results?.Count ?? 0;
        public List<DonorHistorySearchHitViewModel> Results { get; set; } = new();
    }

    public class DonorHistoryProfileViewModel
    {
        public long PrimaryDonorId { get; set; }
        public string FullName { get; set; } = "";
        public string Gender { get; set; } = "";
        public string Phone { get; set; } = "";
        public string BloodType { get; set; } = "";
        public int LifetimeVisits { get; set; }
        public int SuccessfulDonations { get; set; }
        public int FailedScreenings { get; set; }
        public int DeferredVisits { get; set; }
        public int UnitsCollected { get; set; }
        public int UnitsInInventory { get; set; }
        public DateTime? FirstVisit { get; set; }
        public DateTime? LastVisit { get; set; }
        public List<DonorHistoryVisitViewModel> Visits { get; set; } = new();
        public string? NationalId { get; set; }
        public string? Email { get; set; }
        public List<DonorDispenseHistoryItemViewModel> DispenseHistory { get; set; } = new();
    }

    public class DonorHistoryVisitViewModel
    {
        public long DonorId { get; set; }
        public DateTime RegisteredAt { get; set; }
        public string DonationType { get; set; } = "";
        public string Status { get; set; } = "";
        public string ScreeningResult { get; set; } = "";
        public string? PatientId { get; set; }
        public string? BagSerial { get; set; }
        public string? UnitStatus { get; set; }
        public long? ScreeningId { get; set; }
        public bool CanPrintCertificate { get; set; }
    }

    public class DonorDispenseHistoryItemViewModel
    {
        public long DispenseId { get; set; }
        public string SerialNumber { get; set; } = "";
        public string RecipientName { get; set; } = "";
        public string WardDestination { get; set; } = "";
        public string? DonationSource { get; set; }
        public DateTime DispenseDateTime { get; set; }
        public string BloodType { get; set; } = "";
    }

    public class ExternalDonorQuickRegisterViewModel
    {
        [Required, StringLength(255)]
        public string DonorName { get; set; } = "";

        [Required, StringLength(5)]
        public string BloodType { get; set; } = "";

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        public string? NationalId { get; set; }

        [EmailAddress, StringLength(255)]
        public string? Email { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class ExternalDonorSearchItemViewModel
    {
        public long DonorId { get; set; }
        public string BloodBankDonorId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string BloodType { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? NationalId { get; set; }
        public bool IsRepeatDonor { get; set; }
        public DateTime? LastDonationDate { get; set; }
        public int LifetimeVisits { get; set; }
    }

    public class ScreeningQueueItemViewModel
    {
        public long DonorId { get; set; }
        public string BloodBankDonorId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string BloodType { get; set; } = "";
        public string DonationType { get; set; } = "";
        public DateTime RegisteredAt { get; set; }
        public bool IsLegacyUnit { get; set; }
        public long? UnitId { get; set; }
        public string? SerialNumber { get; set; }
    }

    public class ScreeningFormViewModel
    {
        public long DonorId { get; set; }
        public string BloodBankDonorId { get; set; } = "";
        public long? UnitId { get; set; }
        public string DonorName { get; set; } = "";
        public string DonorPhone { get; set; } = "";
        public string BloodType { get; set; } = "";
        public string? SerialNumber { get; set; }
        public string StaffName { get; set; } = "";
        public DateTime ScreeningDateTime { get; set; } = DateTime.Now;

        [Required] public string HivResult { get; set; } = TestResults.Pass;
        [Required] public string HepBResult { get; set; } = TestResults.Pass;
        [Required] public string HepCResult { get; set; } = TestResults.Pass;
        [Required] public string SyphilisResult { get; set; } = TestResults.Pass;
        public string OverallResult { get; set; } = ScreeningResults.Passed;
        public string? PreDonationMalariaResult { get; set; }
        public string? RegistrationVitalsSummary { get; set; }

        public string? Hemoglobin { get; set; }
        public string? Wbc { get; set; }
        public string? Rbc { get; set; }
        public string? Platelet { get; set; }
        public string? Notes { get; set; }
    }

    public class ScreeningCertificateViewModel
    {
        public long ScreeningId { get; set; }
        public long DonorId { get; set; }
        public string BloodBankDonorId { get; set; } = "";
        public string DonorName { get; set; } = "";
        public string BloodType { get; set; } = "";
        public string DonationType { get; set; } = "";
        public string? PatientId { get; set; }
        public string? SerialNumber { get; set; }
        /// <summary>Assigned inventory unit serial (bag label), if recorded.</summary>
        public string? BagSerialNumber { get; set; }
        public DateTime ScreeningDateTime { get; set; }
        public string StaffName { get; set; } = "";
        public string OverallResult { get; set; } = "";
        public string HivResult { get; set; } = "";
        public string HepBResult { get; set; } = "";
        public string HepCResult { get; set; } = "";
        public string SyphilisResult { get; set; } = "";
        public string MalariaResult { get; set; } = "";
        public string? Notes { get; set; }
        public string FacilityName { get; set; } = "Blood Bank";
        public bool IsPassed => OverallResult == ScreeningResults.Passed;
    }

    public class BagAssignmentQueueItemViewModel
    {
        public long DonorId { get; set; }
        public string BloodBankDonorId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string BloodType { get; set; } = "";
        public string DonationType { get; set; } = "";
        public DateTime RegisteredAt { get; set; }
        public string? PatientId { get; set; }
    }

    public class BagAssignmentFormViewModel
    {
        public long DonorId { get; set; }
        public string BloodBankDonorId { get; set; } = "";
        public string DonorName { get; set; } = "";
        public string DonorPhone { get; set; } = "";
        public string BloodType { get; set; } = "";
        public string DonationType { get; set; } = "";
        public string? PatientId { get; set; }
        public long? RequestId { get; set; }

        [Required(ErrorMessage = "Bag serial number is required.")]
        [MaxLength(30)]
        public string SerialNumber { get; set; } = "";

        public bool UseGeneratedSerial { get; set; }

        [Required] public string ComponentType { get; set; } = BloodComponents.PackedRbc;

        [Range(1, 5000)]
        public int VolumeMl { get; set; } = 450;

        [Required]
        public DateTime CollectionDate { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string? StorageLocation { get; set; }
    }

    public class InventoryListViewModel
    {
        public List<BloodBankBloodUnit> Units { get; set; } = new();
        public string? FilterBloodType { get; set; }
        public string? FilterComponent { get; set; }
        public string? FilterStatus { get; set; }
        public string? SearchTerm { get; set; }
        public DateTime? ExpiryFrom { get; set; }
        public DateTime? ExpiryTo { get; set; }
    }

    public class DispensePanelViewModel
    {
        public long RequestId { get; set; }
        public BloodBankRequest Request { get; set; } = null!;
        public BloodBankPatientPanelViewModel PatientPanel { get; set; } = new();
        public List<DirectedDonorOptionViewModel> DirectedDonors { get; set; } = new();
        public List<VolunteerUnitCardViewModel> VolunteerUnits { get; set; } = new();
        public string DispensingStaffName { get; set; } = "";
        public bool IsHisLinkedRequest { get; set; } = true;

        /// <summary>Read-only HIS context — patient name from request.</summary>
        public string LockedPatientName { get; set; } = "";
        public string LockedWardBed { get; set; } = "";
        public int LockedUnitsRequested { get; set; }

        public string CollectorName { get; set; } = "";
        public string WardDestination { get; set; } = "";
        public string SourceMode { get; set; } = "Volunteer";
        public List<long> SelectedUnitIds { get; set; } = new();
        public long? SelectedDirectedDonorId { get; set; }
        public bool Confirmed { get; set; }
        public string CompatibleDonorTypesSummary { get; set; } = "";
        public int AvailableDirectedCount { get; set; }
        public int AvailableVolunteerCount { get; set; }
    }

    public class DirectedDonorOptionViewModel
    {
        public long DonorId { get; set; }
        public long? UnitId { get; set; }
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string BloodType { get; set; } = "";
        public string ComponentType { get; set; } = "";
        public string? SerialNumber { get; set; }
        public DateTime? DonationDate { get; set; }
        public string ScreeningResult { get; set; } = "";
        public DateTime? ScreeningDate { get; set; }
        public string? DonorPatientId { get; set; }
        public bool IsVerifiedPatientLinkage { get; set; }
        public bool IsExactBloodTypeMatch { get; set; }
        public bool IsAvailable { get; set; }
        public string? UnavailableReason { get; set; }
    }

    public class VolunteerUnitCardViewModel
    {
        public long UnitId { get; set; }
        public string SerialNumber { get; set; } = "";
        public string BloodType { get; set; } = "";
        public string ComponentType { get; set; } = "";
        public int? VolumeMl { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int DaysUntilExpiry { get; set; }
        public bool ExpiringSoon { get; set; }
        public string? StorageLocation { get; set; }
        public string DonorReference { get; set; } = "";
        public bool IsDirectedForRequestPatient { get; set; }
        public bool IsExactBloodTypeMatch { get; set; }
        public string CompatibilityLabel { get; set; } = "";
        public bool IsRecommended { get; set; }
    }

    public class PendingBloodRequestSearchItem
    {
        public long RequestId { get; set; }
        public string PatientId { get; set; } = "";
        public string PatientName { get; set; } = "";
        public string Inhospid { get; set; } = "";
        public string? Ward { get; set; }
        public string? BedLocation { get; set; }
        public string RequestingDoctorName { get; set; } = "";
        public string BloodType { get; set; } = "";
        public string ComponentType { get; set; } = "";
        public int UnitsRequested { get; set; }
        public string UrgencyLevel { get; set; } = "";
        public DateTime RequestDateTime { get; set; }
    }

    public class DispenseFormViewModel
    {
        public long UnitId { get; set; }
        public long? RequestId { get; set; }
        public string SerialNumber { get; set; } = "";
        public string UnitBloodType { get; set; } = "";
        public string UnitComponentType { get; set; } = "";
        public DateTime UnitExpiryDate { get; set; }
        public string UnitStatus { get; set; } = "";
        public BloodBankPatientPanelViewModel PatientPanel { get; set; } = new();

        /// <summary>request = doctor HIS order; his = lookup chart; manual = not in HisOrder.</summary>
        public string RecipientMode { get; set; } = "request";

        public string? WardFilter { get; set; }

        public string CollectorName { get; set; } = "";
        public string WardDestination { get; set; } = "";
        [Range(1, 100)] public int UnitsReleased { get; set; } = 1;
        public DateTime DispenseDateTime { get; set; } = DateTime.Now;
        public string DispensingStaffName { get; set; } = "";

        /// <summary>Donor linked to this unit (who donated the blood).</summary>
        public long? UnitDonorId { get; set; }
        public string? UnitDonorName { get; set; }
        public string? UnitDonorBloodType { get; set; }
        public string? UnitDonorPhone { get; set; }

        /// <summary>Confirmed source donor for this dispense (defaults to unit donor).</summary>
        public long? SourceDonorId { get; set; }
        public string? DonationSource { get; set; }

        /// <summary>Manual dispense destination key from ward dropdown (Emergency, Outside hospital, Other, …).</summary>
        public string? ManualDestinationKey { get; set; }
    }

    public class InventoryDispensePageViewModel
    {
        public DispenseFormViewModel Form { get; set; } = new();
        public List<PendingBloodRequestSearchItem> PendingRequests { get; set; } = new();
    }

    public class RequestQueueViewModel
    {
        public List<BloodBankRequest> Requests { get; set; } = new();
        public string? StatusFilter { get; set; }
        public string? UrgencyFilter { get; set; }
        public string? BloodTypeFilter { get; set; }
        public string? PatientIdFilter { get; set; }
        public string? PatientNameFilter { get; set; }
        public string? PhoneFilter { get; set; }
        public string? InhospidFilter { get; set; }
    }

    public class BloodRequestFormViewModel
    {
        [Required] public string PatientId { get; set; } = "";
        public string Inhospid { get; set; } = "";
        [Required] public string PatientName { get; set; } = "";
        public string? Ward { get; set; }
        public string? BedLocation { get; set; }
        [Required] public string BloodType { get; set; } = "O+";
        [Required] public string ComponentType { get; set; } = BloodComponents.PackedRbc;
        [Range(1, 20)] public int UnitsRequested { get; set; } = 1;
        [Required] public string UrgencyLevel { get; set; } = UrgencyLevels.Routine;
        [Required] public string PatientType { get; set; } = "IPD";
        public string RequestingDoctorId { get; set; } = "";
        public string RequestingDoctorName { get; set; } = "";
        public BloodBankPatientPanelViewModel? PatientPanel { get; set; }
    }

    public class HisBloodRequestApiModel
    {
        [Required] public string PatientId { get; set; } = "";
        [Required] public string Inhospid { get; set; } = "";
        [Required] public string PatientName { get; set; } = "";
        [Required] public string BloodType { get; set; } = "";
        [Required] public string ComponentType { get; set; } = "";
        [Range(1, 20)] public int UnitsRequested { get; set; } = 1;
        public string UrgencyLevel { get; set; } = UrgencyLevels.Routine;
        public string PatientType { get; set; } = "IPD";
        public string? Ward { get; set; }
        public string? BedLocation { get; set; }
        [Required] public string RequestingDoctorId { get; set; } = "";
        [Required] public string RequestingDoctorName { get; set; } = "";
        public long? Orderplanid { get; set; }
    }

    public class AuditLogViewModel
    {
        public List<BloodBankScreeningRecord> Screenings { get; set; } = new();
        public List<BloodBankDispenseRecord> Dispenses { get; set; } = new();
        public string? FilterStaff { get; set; }
        public string? FilterPatient { get; set; }
        public string? FilterBloodType { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }

    public class UnitHistoryViewModel
    {
        public BloodBankBloodUnit Unit { get; set; } = null!;
        public string BloodBankUnitId { get; set; } = "";
        public string BloodBankDonorId { get; set; } = "";
        public string? BloodBankRequestId { get; set; }
        public BloodBankDonor? Donor { get; set; }
        public int DonorLifetimeVisits { get; set; }
        public int DonorSuccessfulDonations { get; set; }
        public string? HisPatientId { get; set; }
        public string? HisInhospid { get; set; }
        public long? HisRequestId { get; set; }
        public long? HisOrderplanid { get; set; }
        public bool HisPatientMatchesDirectedDonor { get; set; }
        public BloodBankRequest? LinkedRequest { get; set; }
        public BloodBankPatientPanelViewModel? HisPatientPanel { get; set; }
        public List<BloodBankScreeningRecord> Screenings { get; set; } = new();
        public BloodBankDispenseRecord? Dispense { get; set; }
        public List<UnitHistoryTimelineEvent> Timeline { get; set; } = new();
    }

    public class UnitHistoryTimelineEvent
    {
        public string Kind { get; set; } = "";
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public DateTime When { get; set; }
        public string? ReferenceId { get; set; }
        public string? HisReference { get; set; }
        public bool IsComplete { get; set; }
        public bool IsFailed { get; set; }
    }

    public class BloodBankPatientSearchResultViewModel
    {
        public bool EmptyParam { get; set; }
        public List<BloodBankPatientSearchRowViewModel> Rows { get; set; } = new();
    }

    public class BloodBankPatientSearchRowViewModel
    {
        public KmuChart Chart { get; set; } = null!;
        public long? PendingRequestId { get; set; }
        public string? BloodType { get; set; }
        public string? ComponentType { get; set; }
        public string? UrgencyLevel { get; set; }
        public int UnitsRequested { get; set; }

        public string PatientId => Chart.ChrHealthId.Trim();
        public string FullName => BloodBankService.FormatDonorFullName(
            Chart.ChrPatientFirstname, Chart.ChrPatientMidname, Chart.ChrPatientLastname);
        public bool HasPendingRequest => PendingRequestId.HasValue;
    }

    /// <summary>Standard Blood Bank page title block (h3 + descriptive subtitle).</summary>
    public class BloodBankPageHeaderViewModel
    {
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string? Icon { get; set; }
        public bool IconBlood { get; set; }
    }

    /// <summary>Blood bank doctor request summary for Patient Visit (HisOrder Other tab).</summary>
    public class HisOrderBloodBankRequestSummaryDto
    {
        public long RequestId { get; set; }
        public long? Orderplanid { get; set; }
        public string Status { get; set; } = "";
        public string BloodType { get; set; } = "";
        public string ComponentType { get; set; } = "";
        public int UnitsRequested { get; set; }
        public string UrgencyLevel { get; set; } = "";
        public string RequestingDoctorName { get; set; } = "";
        public DateTime RequestDateTime { get; set; }
        public string? RejectReason { get; set; }
        public int DispenseCount { get; set; }
        public int UnitsReleased { get; set; }
    }

    public class HisOrderBloodBankDispenseDto
    {
        public long DispenseId { get; set; }
        public DateTime DispenseDateTime { get; set; }
        public string DispensingStaffName { get; set; } = "";
        public string CollectorName { get; set; } = "";
        public string WardDestination { get; set; } = "";
        public int UnitsReleased { get; set; }
        public string SerialNumbers { get; set; } = "";
    }

    public class HisOrderBloodBankRequestDetailsDto
    {
        public long RequestId { get; set; }
        public long? Orderplanid { get; set; }
        public string PatientId { get; set; } = "";
        public string Inhospid { get; set; } = "";
        public string PatientName { get; set; } = "";
        public string Status { get; set; } = "";
        public string BloodType { get; set; } = "";
        public string ComponentType { get; set; } = "";
        public int UnitsRequested { get; set; }
        public string UrgencyLevel { get; set; } = "";
        public string RequestingDoctorName { get; set; } = "";
        public string? Ward { get; set; }
        public string? BedLocation { get; set; }
        public DateTime RequestDateTime { get; set; }
        public DateTime? ModifyDate { get; set; }
        public string? RejectReason { get; set; }
        public List<HisOrderBloodBankDispenseDto> Dispenses { get; set; } = new();
        public string BloodBankDetailsUrl { get; set; } = "";
    }
}
