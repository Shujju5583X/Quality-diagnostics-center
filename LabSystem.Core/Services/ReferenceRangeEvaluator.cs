using System;
using System.Linq;
using LabSystem.Core.Models;

namespace LabSystem.Core.Services
{
    public static class ReferenceRangeEvaluator
    {
        public static ReferenceRange FindMatchingRange(TestType tt, Patient patient)
        {
            if (tt == null || tt.ReferenceRanges == null || tt.ReferenceRanges.Count == 0 || patient == null)
                return null;

            int age = patient.Age;
            string gender = patient.Gender ?? "All";

            // Try age-specific match first
            var match = tt.ReferenceRanges.FirstOrDefault(r =>
                (string.Equals(r.Gender, gender, StringComparison.OrdinalIgnoreCase) || string.Equals(r.Gender, "All", StringComparison.OrdinalIgnoreCase))
                && age >= r.AgeMin && age <= r.AgeMax);
            if (match != null) return match;

            // Fallback: widest range for this gender (AgeMin=0, AgeMax >= 120)
            return tt.ReferenceRanges.FirstOrDefault(r =>
                (string.Equals(r.Gender, gender, StringComparison.OrdinalIgnoreCase) || string.Equals(r.Gender, "All", StringComparison.OrdinalIgnoreCase))
                && r.AgeMin == 0 && r.AgeMax >= 120);
        }

        public static bool IsAbnormal(double? value, TestType tt, Patient patient)
        {
            if (value == null) return false;
            if (tt == null) return false;

            var matchingRange = FindMatchingRange(tt, patient);
            if (matchingRange != null)
            {
                if (matchingRange.RangeLow.HasValue && value.Value < matchingRange.RangeLow.Value)
                    return true;
                if (matchingRange.RangeHigh.HasValue && value.Value > matchingRange.RangeHigh.Value)
                    return true;
                return false;
            }

            // Fallback to static range
            if (tt.ReferenceRangeLow.HasValue && value.Value < tt.ReferenceRangeLow.Value)
                return true;
            if (tt.ReferenceRangeHigh.HasValue && value.Value > tt.ReferenceRangeHigh.Value)
                return true;

            return false;
        }

        public static bool IsLow(double? value, TestType tt, Patient patient)
        {
            if (value == null) return false;
            if (tt == null) return false;

            var matchingRange = FindMatchingRange(tt, patient);
            if (matchingRange != null)
            {
                return matchingRange.RangeLow.HasValue && value.Value < matchingRange.RangeLow.Value;
            }
            return tt.ReferenceRangeLow.HasValue && value.Value < tt.ReferenceRangeLow.Value;
        }

        public static string FormatRange(TestType tt, Patient patient)
        {
            if (tt == null) return "N/A";

            var matchingRange = FindMatchingRange(tt, patient);
            if (matchingRange != null)
            {
                if (matchingRange.RangeLow.HasValue && matchingRange.RangeHigh.HasValue)
                {
                    return matchingRange.RangeLow.Value + " - " + matchingRange.RangeHigh.Value;
                }
                if (matchingRange.RangeLow.HasValue)
                {
                    return ">= " + matchingRange.RangeLow.Value;
                }
                if (matchingRange.RangeHigh.HasValue)
                {
                    return "<" + matchingRange.RangeHigh.Value;
                }
            }

            if (tt.Unit == "Blood Group")
            {
                return "A/B/O/AB Rh +/-";
            }
            if (tt.Name != null && tt.Name.Contains("S. Typhi"))
            {
                return "1:40";
            }
            if (tt.Name != null && tt.Name.Contains("S. Paratyphi"))
            {
                return "1:20";
            }
            if (tt.Unit == "Titer")
            {
                return "No Agglutination";
            }
            if (tt.Unit == "Qualitative" || (tt.Name != null && (tt.Name.Contains("Urine Sugar") || tt.Name.Contains("Urine Protein"))))
            {
                return "Absent";
            }
            if (tt.Name != null && tt.Name.Contains("Malaria"))
            {
                return "negative";
            }
            if (tt.Name != null && (tt.Name.Contains("HBsAg") || tt.Name.Contains("HCV") || tt.Name.Contains("VDRL") || tt.Name.Contains("HIV") || tt.Name.Contains("HBSG") || tt.Name.Contains("Hemoglobin Solubility")))
            {
                return "Negative";
            }

            if (tt.ReferenceRangeLow.HasValue && tt.ReferenceRangeHigh.HasValue)
            {
                return tt.ReferenceRangeLow.Value + " - " + tt.ReferenceRangeHigh.Value;
            }
            if (tt.ReferenceRangeLow.HasValue)
            {
                return ">= " + tt.ReferenceRangeLow.Value;
            }
            if (tt.ReferenceRangeHigh.HasValue)
            {
                return "<" + tt.ReferenceRangeHigh.Value;
            }
            return "N/A";
        }
    }
}
