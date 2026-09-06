using System;
using System.Collections.Generic;
using System.Linq;
using KMU.HisOrder.MVC.Models.BloodBank;

namespace KMU.HisOrder.MVC.Areas.BloodBank.Models
{
    public static class BloodBankLookups
    {
        public static readonly string[] BloodTypes =
            { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };

        private static readonly Dictionary<string, HashSet<string>> RbcRecipientCompatibility = new(StringComparer.OrdinalIgnoreCase)
        {
            ["O-"] = new(StringComparer.OrdinalIgnoreCase) { "O-" },
            ["O+"] = new(StringComparer.OrdinalIgnoreCase) { "O-", "O+" },
            ["A-"] = new(StringComparer.OrdinalIgnoreCase) { "O-", "A-" },
            ["A+"] = new(StringComparer.OrdinalIgnoreCase) { "O-", "O+", "A-", "A+" },
            ["B-"] = new(StringComparer.OrdinalIgnoreCase) { "O-", "B-" },
            ["B+"] = new(StringComparer.OrdinalIgnoreCase) { "O-", "O+", "B-", "B+" },
            ["AB-"] = new(StringComparer.OrdinalIgnoreCase) { "O-", "A-", "B-", "AB-" },
            ["AB+"] = new(StringComparer.OrdinalIgnoreCase) { "O-", "O+", "A-", "A+", "B-", "B+", "AB-", "AB+" }
        };

        public static string NormalizeBloodType(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var t = value.Trim().ToUpperInvariant().Replace(" ", "");
            foreach (var bt in BloodTypes)
            {
                if (bt.Equals(t, StringComparison.OrdinalIgnoreCase))
                    return bt;
            }
            return value.Trim();
        }

        public static bool IsExactBloodTypeMatch(string? recipient, string? donor) =>
            string.Equals(NormalizeBloodType(recipient), NormalizeBloodType(donor), StringComparison.OrdinalIgnoreCase);

        public static bool IsCompatibleRbc(string? recipient, string? donor)
        {
            var r = NormalizeBloodType(recipient);
            var d = NormalizeBloodType(donor);
            if (string.IsNullOrEmpty(r) || string.IsNullOrEmpty(d)) return false;
            return RbcRecipientCompatibility.TryGetValue(r, out var allowed) && allowed.Contains(d);
        }

        public static IReadOnlyList<string> GetCompatibleRbcDonorTypes(string? recipient)
        {
            var r = NormalizeBloodType(recipient);
            if (string.IsNullOrEmpty(r) || !RbcRecipientCompatibility.TryGetValue(r, out var allowed))
                return Array.Empty<string>();
            return BloodTypes.Where(allowed.Contains).ToList();
        }

        public static string DescribeRbcCompatibility(string? recipient)
        {
            var types = GetCompatibleRbcDonorTypes(recipient);
            return types.Count == 0 ? "" : string.Join(", ", types);
        }

        public static readonly string[] Components =
        {
            BloodComponents.WholeBlood,
            BloodComponents.PackedRbc,
            BloodComponents.Ffp,
            BloodComponents.Platelets,
            BloodComponents.Cryoprecipitate
        };

        public static string NormalizeComponent(string? componentType)
        {
            if (string.IsNullOrWhiteSpace(componentType)) return BloodComponents.PackedRbc;
            var t = componentType.Trim();
            if (t.Equals(BloodComponents.PackedRbcLegacy, StringComparison.OrdinalIgnoreCase)
                || t.Contains("PRBC", StringComparison.OrdinalIgnoreCase)
                || t.Contains("packed", StringComparison.OrdinalIgnoreCase) && t.Contains("rbc", StringComparison.OrdinalIgnoreCase))
                return BloodComponents.PackedRbc;
            if (t.Equals(BloodComponents.FfpLegacy, StringComparison.OrdinalIgnoreCase)
                || t.Contains("plasma", StringComparison.OrdinalIgnoreCase))
                return BloodComponents.Ffp;
            if (t.Equals(BloodComponents.WholeBlood, StringComparison.OrdinalIgnoreCase)) return BloodComponents.WholeBlood;
            if (t.Equals(BloodComponents.Platelets, StringComparison.OrdinalIgnoreCase)) return BloodComponents.Platelets;
            if (t.Equals(BloodComponents.Cryoprecipitate, StringComparison.OrdinalIgnoreCase)) return BloodComponents.Cryoprecipitate;
            return t;
        }

        public static readonly string[] UrgencyLevelOptions =
        {
            UrgencyLevels.Routine,
            UrgencyLevels.Urgent,
            UrgencyLevels.Emergency
        };

        public static readonly string[] UnitStatusesList =
        {
            UnitStatuses.Available,
            UnitStatuses.Reserved,
            UnitStatuses.Dispensed,
            UnitStatuses.Expired,
            UnitStatuses.Rejected,
            UnitStatuses.Quarantined,
            UnitStatuses.PendingScreening
        };

        public static readonly string[] PatientTypes = { "OPD", "IPD" };

        /// <summary>Primary destination when dispensing outside HisOrder.</summary>
        public static readonly string[] DispenseWardDestinations =
        {
            "Emergency",
            "ICU",
            "OT Ward",
            "Male Ward",
            "Female Ward",
            "Outside hospital",
            "Other"
        };

        /// <summary>Optional ward/area when "Other" and patient is inside the hospital.</summary>
        public static readonly string[] DispenseInsideWardOptions =
        {
            "Emergency",
            "ICU",
            "OT Ward",
            "Male Ward",
            "Female Ward"
        };

        /// <summary>How blood reached the recipient (Not in HisOrder dispense).</summary>
        public static readonly string[] DonationSources =
        {
            "Walk-in",
            "Emergency",
            "Referral",
            "Ambulance",
            "Mobile clinic",
            "Outside transfer",
            "Other"
        };
    }
}
