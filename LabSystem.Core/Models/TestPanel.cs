using System.Collections.Generic;

namespace LabSystem.Core.Models
{
    public class TestPanel
    {
        public int PanelId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public virtual ICollection<TestType> TestTypes { get; set; } = new HashSet<TestType>();
    }
}
