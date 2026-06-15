using System.Collections.Generic;

namespace LabSystem.Core.Models
{
    public class Department
    {
        public int DepartmentId { get; set; }
        public string Name { get; set; }
        public virtual ICollection<TestType> TestTypes { get; set; } = new HashSet<TestType>();
    }
}
