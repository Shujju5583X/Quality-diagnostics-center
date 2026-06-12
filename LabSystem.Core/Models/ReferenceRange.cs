namespace LabSystem.Core.Models
{
    public class ReferenceRange
    {
        public int ReferenceRangeId { get; set; }
        public int TestTypeId { get; set; }
        public virtual TestType TestType { get; set; }
        public string Gender { get; set; } // "Male", "Female", "Other", or "All"
        public int AgeMin { get; set; }
        public int AgeMax { get; set; }
        public double? RangeLow { get; set; }
        public double? RangeHigh { get; set; }
    }
}
