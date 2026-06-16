using System.Collections.Generic;

namespace LabSystem.Core.Models
{
    public class Department
    {
        public Department()
        {
            TestTypes = new HashSet<TestType>();
        }

        public int DepartmentId { get; set; }
        public string Name { get; set; }
        public virtual ICollection<TestType> TestTypes { get; set; }
    }
}