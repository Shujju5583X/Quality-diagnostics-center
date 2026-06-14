using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabSystem.Core.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }

        public int PatientId { get; set; }
        [ForeignKey(nameof(PatientId))]
        public virtual Patient Patient { get; set; }

        public DateTime AppointmentDate { get; set; }

        public int DurationMinutes { get; set; } = 15;

        public string Purpose { get; set; }

        public string Status { get; set; } = "Scheduled";

        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        [NotMapped]
        public DateTime EndTime => AppointmentDate.AddMinutes(DurationMinutes);
    }
}