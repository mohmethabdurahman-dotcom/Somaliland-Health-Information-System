using System;

namespace KMU.HisOrder.MVC.Models.BloodBank
{
    /// <summary>Canonical Blood Bank identifiers for inventory, donors, and HIS cross-reference.</summary>
    public static class BloodBankIds
    {
        public const string UnitPrefix = "BB-UNIT-";
        public const string DonorPrefix = "BB-DONOR-";
        public const string RequestPrefix = "BB-REQ-";
        public const string ScreeningPrefix = "BB-SCR-";

        public static string FormatUnit(long unitId) => $"{UnitPrefix}{unitId:D6}";
        public static string FormatDonor(long donorId) => $"{DonorPrefix}{donorId:D6}";
        public static string FormatRequest(long requestId) => $"{RequestPrefix}{requestId:D6}";
        public static string FormatScreening(long screeningId) => $"{ScreeningPrefix}{screeningId:D6}";

        public static bool TryParseUnitId(string? value, out long unitId)
        {
            unitId = 0;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var t = value.Trim();
            if (t.StartsWith(UnitPrefix, StringComparison.OrdinalIgnoreCase)
                && long.TryParse(t.Substring(UnitPrefix.Length), out unitId))
                return true;
            return long.TryParse(t, out unitId);
        }

        public static bool TryParseDonorId(string? value, out long donorId)
        {
            donorId = 0;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var t = value.Trim();
            if (t.StartsWith(DonorPrefix, StringComparison.OrdinalIgnoreCase)
                && long.TryParse(t.Substring(DonorPrefix.Length), out donorId))
                return true;
            return long.TryParse(t, out donorId);
        }
    }
}
