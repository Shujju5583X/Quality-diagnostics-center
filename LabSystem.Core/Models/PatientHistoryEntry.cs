using System;

namespace LabSystem.Core.Models
{
    public class PatientHistoryEntry
    {
        public DateTime OrderDate { get; set; }
        public string TestName { get; set; }
        public double? Value { get; set; }
        public string ValueText { get; set; }
        public string Unit { get; set; }
        public bool IsAbnormal { get; set; }
        public double? ReferenceLow { get; set; }
        public double? ReferenceHigh { get; set; }
    }
}
