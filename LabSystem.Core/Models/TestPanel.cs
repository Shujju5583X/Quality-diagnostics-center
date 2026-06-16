using System.Collections.Generic;

namespace LabSystem.Core.Models
{
    public class TestPanel
    {
        public TestPanel()
        {
            TestTypes = new HashSet<TestType>();
        }

        public int PanelId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public virtual ICollection<TestType> TestTypes { get; set; }
    }
}