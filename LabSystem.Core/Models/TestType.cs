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
    }
}
