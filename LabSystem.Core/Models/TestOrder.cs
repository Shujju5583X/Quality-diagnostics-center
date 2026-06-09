using System;

namespace LabSystem.Core.Models
{
    public class TestOrder
    {
        public int OrderId { get; set; }
        public int PatientId { get; set; }
        public virtual Patient Patient { get; set; }
        public string OrderedAt { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public string ReferredBy { get; set; }
    }
}
