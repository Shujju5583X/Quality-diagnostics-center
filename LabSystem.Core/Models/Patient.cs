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
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Gender { get; set; }

        [NotMapped]
        public GenderType GenderEnum
        {
            get => Enum.TryParse<GenderType>(Gender, true, out var g) ? g : GenderType.Male;
            set => Gender = value.ToString();
        }
    }
}
