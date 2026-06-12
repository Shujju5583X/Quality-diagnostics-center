using System;

namespace LabSystem.Core.Models
{
    public class Result
    {
        public int ResultId { get; set; }
        public int OrderId { get; set; }
        public virtual TestOrder Order { get; set; }
        public int TypeId { get; set; }
        public virtual TestType TestType { get; set; }
        public double Value { get; set; }
        public DateTime RecordedAt { get; set; }
        public int TechnicianId { get; set; }
        public virtual Staff Technician { get; set; }
        public bool IsAbnormal { get; set; }

        // Amendment tracking
        public bool IsAmended { get; set; }
        public string AmendmentReason { get; set; }
        public DateTime? AmendedAt { get; set; }

        // Audit timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
