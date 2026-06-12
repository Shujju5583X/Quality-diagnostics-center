using System.Collections.Generic;

namespace LabSystem.Core.Models
{
    public class TestType
    {
        public int TypeId { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public double? ReferenceRangeLow { get; set; }
        public double? ReferenceRangeHigh { get; set; }
        public bool IsActive { get; set; }

        // Premium report metadata fields
        public string Category { get; set; }
        public string GroupName { get; set; }
        public string Method { get; set; }
        public string Interpretation { get; set; }
        public int SortOrder { get; set; }
        public decimal Price { get; set; }
        
        public string SampleType { get; set; }
        public virtual ICollection<ReferenceRange> ReferenceRanges { get; set; } = new HashSet<ReferenceRange>();
    }
}
