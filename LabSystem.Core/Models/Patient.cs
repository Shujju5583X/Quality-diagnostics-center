using System;

namespace LabSystem.Core.Models
{
    public class Patient
    {
        public int PatientId { get; set; }
        public string FullName { get; set; }
        public string DateOfBirth { get; set; }
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
        public string CreatedAt { get; set; }
        public string Gender { get; set; }
    }
}
