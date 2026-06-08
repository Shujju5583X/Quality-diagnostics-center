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
        public string RecordedAt { get; set; }
        public int TechnicianId { get; set; }
        public virtual Staff Technician { get; set; }
        public bool IsAbnormal { get; set; }
    }
}
