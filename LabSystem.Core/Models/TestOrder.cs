using System;
using System.Collections.Generic;

namespace LabSystem.Core.Models
{
    public class TestOrder
    {
        public int OrderId { get; set; }
        public int PatientId { get; set; }
        public virtual Patient Patient { get; set; }
        public DateTime OrderedAt { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public string ReferredBy { get; set; }
        
        public int? DoctorId { get; set; }
        public virtual Doctor Doctor { get; set; }
        
        public virtual ICollection<Specimen> Specimens { get; set; } = new HashSet<Specimen>();
        public virtual ICollection<TestType> TestTypes { get; set; } = new HashSet<TestType>();
    }
}
