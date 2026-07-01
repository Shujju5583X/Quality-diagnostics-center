using System.Collections.Generic;
using LabSystem.Core.Enums;

namespace LabSystem.Core.Models
{
    public class TestType
    {
        public TestType()
        {
            ReferenceRanges = new HashSet<ReferenceRange>();
        }

        public int TypeId { get; set; }
        public ResultInputType InputType { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public double? ReferenceRangeLow { get; set; }
        public double? ReferenceRangeHigh { get; set; }
        public bool IsActive { get; set; }

        // Premium report metadata fields
        public string Category { get; set; }
        public string GroupName { get; set; }
        public string Method { get; set; }
        public string Instrument { get; set; }
        public string Interpretation { get; set; }
        public int SortOrder { get; set; }
        public decimal Price { get; set; }
        
        public bool HasBesideRefRanges { get; set; }
        public bool HasTextRefRanges { get; set; }
        public string TextReferenceString { get; set; }
        public string TextReferenceNormalValue { get; set; }
        
        public string SampleType { get; set; }
        public int? DepartmentId { get; set; }
        public virtual Department Department { get; set; }
        public virtual ICollection<ReferenceRange> ReferenceRanges { get; set; }
    }
}