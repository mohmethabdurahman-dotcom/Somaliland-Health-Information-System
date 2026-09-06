namespace KMU.HisOrder.MVC.Models.BloodBank
{
    public static class BloodBankRoles
    {
        public const string Admin = "BloodBank_Admin";
        public const string Staff = "BloodBank_Staff";
        public const string Any = "BloodBank_Admin,BloodBank_Staff";

        /// <summary>Legacy ids — migrated by seeder to Admin / Staff.</summary>
        public const string LegacyReception = "BloodBank_Reception";
        public const string LegacyLab = "BloodBank_Lab";
    }

    public static class DonationTypes
    {
        public const string Volunteer = "Volunteer";
        public const string Directed = "Directed";
        /// <summary>Walk-in / external registration (Not in HisOrder workflow).</summary>
        public const string WalkIn = "Walk-in";
    }

    /// <summary>Donor pipeline before a blood unit exists.</summary>
    public static class DonorStatuses
    {
        public const string Registered = "Registered";
        public const string Screening = "Screening";
        public const string Passed = "Passed";
        public const string Failed = "Failed";
        /// <summary>Deferred at pre-donation screening before unit collection.</summary>
        public const string Deferred = "Deferred";
    }

    public static class UnitStatuses
    {
        public const string Available = "Available";
        public const string Reserved = "Reserved";
        public const string Dispensed = "Dispensed";
        public const string Expired = "Expired";
        public const string Rejected = "Rejected";
        public const string Quarantined = "Quarantined";
        /// <summary>Legacy: unit created before bag assignment split.</summary>
        public const string PendingScreening = "PendingScreening";
    }

    public static class RequestStatuses
    {
        public const string Pending = "Pending";
        public const string Fulfilled = "Fulfilled";
        public const string Rejected = "Rejected";
    }

    public static class ScreeningResults
    {
        public const string Passed = "Passed";
        public const string Failed = "Failed";
        public const string Pending = "Pending";
    }

    public static class PatientEventTypes
    {
        public const string DoctorBloodRequest = "Doctor Blood Request";
        public const string BloodDispensed = "Blood Dispensed";
    }

    public static class BloodComponents
    {
        public const string WholeBlood = "Whole Blood";
        /// <summary>Canonical label for PRBC / apheresis RBC products.</summary>
        public const string PackedRbc = "Packed Red Blood Cells (PRBC)";
        /// <summary>Legacy stored value — normalized on read.</summary>
        public const string PackedRbcLegacy = "Packed RBC";
        public const string Platelets = "Platelets";
        public const string Ffp = "FFP (Fresh Frozen Plasma)";
        /// <summary>Legacy stored value.</summary>
        public const string FfpLegacy = "FFP";
        public const string Cryoprecipitate = "Cryoprecipitate";
    }

    public static class TestResults
    {
        public const string Pass = "Pass";
        public const string Fail = "Fail";
    }

    public static class UrgencyLevels
    {
        public const string Routine = "Routine";
        public const string Urgent = "Urgent";
        public const string Emergency = "Emergency";
    }
}
