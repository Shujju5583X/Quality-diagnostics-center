using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabSystem.Core.Models
{
    public class Patient
    {
        public int PatientId { get; set; }
        public string Uhid { get; set; }
        public string FullName { get; set; }
        public string Title { get; set; }
        public int? AgeYears { get; set; }
        public int? AgeMonths { get; set; }
        public int? AgeDays { get; set; }
        public DateTime? DateOfBirth { get; set; }
        
        [NotMapped]
        public int Age
        {
            get
            {
                if (AgeYears.HasValue) return AgeYears.Value;
                if (!DateOfBirth.HasValue) return 0;
                var today = DateTime.Today;
                var age = today.Year - DateOfBirth.Value.Year;
                if (DateOfBirth.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
            set
            {
                AgeYears = value;
                DateOfBirth = DateTime.Today.AddYears(-value);
            }
        }

        public string GetFormattedAgeForReport()
        {
            int? y = AgeYears;
            if (!y.HasValue && DateOfBirth.HasValue)
            {
                y = Age;
            }

            if (string.Equals(Title, "Master", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Title, "Baby", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Title, "Baby of", StringComparison.OrdinalIgnoreCase))
            {
                var parts = new System.Collections.Generic.List<string>();
                if (y.HasValue) parts.Add(y.Value + (y.Value == 1 ? " Year" : " Years"));
                if (AgeMonths.HasValue) parts.Add(AgeMonths.Value + (AgeMonths.Value == 1 ? " Month" : " Months"));
                if (AgeDays.HasValue) parts.Add(AgeDays.Value + (AgeDays.Value == 1 ? " Day" : " Days"));
                return parts.Count > 0 ? string.Join(" ", parts) : "0 Years";
            }
            else
            {
                int val = y ?? 0;
                return val + (val == 1 ? " Year" : " Years");
            }
        }

        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Gender { get; set; }

        public override string ToString()
        {
            return FullName ?? string.Empty;
        }
    }
}
