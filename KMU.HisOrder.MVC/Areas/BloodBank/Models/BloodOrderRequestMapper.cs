using System;
using System.Linq;
using KMU.HisOrder.MVC.Models;
using KMU.HisOrder.MVC.Models.BloodBank;

namespace KMU.HisOrder.MVC.Areas.BloodBank.Models
{
    /// <summary>
    /// Maps HIS blood orders (NonMed) to blood bank request fields.
    /// </summary>
    public static class BloodOrderRequestMapper
    {
        public static string MapUrgency(char? urgFlag) => urgFlag switch
        {
            '3' => UrgencyLevels.Emergency,
            '2' => UrgencyLevels.Urgent,
            _ => UrgencyLevels.Routine
        };

        public static string ResolveRequestedBloodType(Hisorderplan order)
        {
            if (!string.IsNullOrWhiteSpace(order.DosePath))
            {
                var fromField = order.DosePath.Trim();
                if (IsKnownBloodType(fromField))
                {
                    return NormalizeBloodType(fromField);
                }
            }

            return ExtractBloodTypeFromText(order.PlanDes, order.Remark);
        }

        public static string ResolveComponent(Hisorderplan order) =>
            ExtractComponent(order.PlanDes, order.Remark);

        /// <summary>
        /// Copies doctor-requested type, component, urgency, and units from HIS order into the blood bank request.
        /// </summary>
        public static bool ApplyHisOrder(BloodBankRequest request, Hisorderplan order)
        {
            if (order.HplanType != "Blood") return false;

            var changed = false;
            var bloodType = ResolveRequestedBloodType(order);
            var component = ResolveComponent(order);
            var urgency = MapUrgency(order.UrgFlag);
            var units = (int)(order.QtyDose ?? order.TotalQty ?? 1);

            if (!string.Equals(request.BloodType, bloodType, StringComparison.OrdinalIgnoreCase))
            {
                request.BloodType = bloodType;
                changed = true;
            }

            if (!string.Equals(request.ComponentType, component, StringComparison.Ordinal))
            {
                request.ComponentType = component;
                changed = true;
            }

            if (!string.Equals(request.UrgencyLevel, urgency, StringComparison.Ordinal))
            {
                request.UrgencyLevel = urgency;
                changed = true;
            }

            if (request.UnitsRequested != units)
            {
                request.UnitsRequested = units;
                changed = true;
            }

            return changed;
        }

        private static string NormalizeBloodType(string value)
        {
            var types = new[] { "AB+", "AB-", "A+", "A-", "B+", "B-", "O+", "O-" };
            return types.First(t => string.Equals(t, value, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsKnownBloodType(string value)
        {
            var types = new[] { "AB+", "AB-", "A+", "A-", "B+", "B-", "O+", "O-" };
            return types.Any(t => string.Equals(t, value, StringComparison.OrdinalIgnoreCase));
        }

        private static string ExtractComponent(string? planDes, string? remark)
        {
            var text = $"{planDes} {remark}".ToLowerInvariant();
            if (text.Contains("platelet")) return BloodComponents.Platelets;
            if (text.Contains("ffp") || text.Contains("plasma")) return BloodComponents.Ffp;
            if (text.Contains("whole")) return BloodComponents.WholeBlood;
            if (text.Contains("rbc") || text.Contains("packed")) return BloodComponents.PackedRbc;
            return BloodComponents.PackedRbc;
        }

        private static string ExtractBloodTypeFromText(string? planDes, string? remark)
        {
            var types = new[] { "AB+", "AB-", "A+", "A-", "B+", "B-", "O+", "O-" };
            var text = $"{planDes} {remark}";
            foreach (var t in types)
            {
                if (text.Contains(t, StringComparison.OrdinalIgnoreCase))
                {
                    return t;
                }
            }

            return "O+";
        }
    }
}
