using System;

namespace LabSystem.Core.Models
{
    public class Doctor
    {
        public Doctor()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public int DoctorId { get; set; }
        public string FullName { get; set; }
        public string ContactPhone { get; set; }
        public decimal Commission { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}