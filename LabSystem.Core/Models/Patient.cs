using System;
using System.ComponentModel.DataAnnotations.Schema;
using LabSystem.Core.Enums;

namespace LabSystem.Core.Models
{
    public class Patient
    {
        public int PatientId { get; set; }
        public string Uhid { get; set; }
        public string FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        
        [NotMapped]
        public int Age
        {
            get
            {
                if (!DateOfBirth.HasValue) return 0;
                var today = DateTime.Today;
                var age = today.Year - DateOfBirth.Value.Year;
                if (DateOfBirth.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
            set
            {
                DateOfBirth = DateTime.Today.AddYears(-value);
            }
        }
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Gender { get; set; }

        [NotMapped]
        public GenderType GenderEnum
        {
            get
            {
                GenderType g;
                if (Enum.TryParse<GenderType>(Gender, true, out g))
                    return g;
                return GenderType.Male;
            }
            set { Gender = value.ToString(); }
        }
        public override string ToString()
        {
            return FullName ?? string.Empty;
        }
    }
}
