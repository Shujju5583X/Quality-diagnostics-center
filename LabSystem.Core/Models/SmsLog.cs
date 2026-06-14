using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabSystem.Core.Models
{
    public class SmsLog
    {
        public int SmsLogId { get; set; }

        public int? PatientId { get; set; }
        [ForeignKey(nameof(PatientId))]
        public virtual Patient Patient { get; set; }

        public string PhoneNumber { get; set; }

        public string Message { get; set; }

        public string Status { get; set; }

        public string GatewayResponse { get; set; }

        public DateTime SentAt { get; set; }
    }
}