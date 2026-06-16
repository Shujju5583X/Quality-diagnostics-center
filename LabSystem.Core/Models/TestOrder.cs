using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using LabSystem.Core.Enums;

namespace LabSystem.Core.Models
{
    public class TestOrder
    {
        public TestOrder()
        {
            TestTypes = new HashSet<TestType>();
        }

        public int OrderId { get; set; }
        public int PatientId { get; set; }
        public virtual Patient Patient { get; set; }
        public DateTime OrderedAt { get; set; }
        public string Status { get; set; }

        [NotMapped]
        public OrderStatus StatusEnum
        {
            get
            {
                OrderStatus s;
                return Enum.TryParse<OrderStatus>(Status, true, out s) ? s : OrderStatus.Pending;
            }
            set { Status = value.ToString(); }
        }
        public string Notes { get; set; }

        // Simple string referral field (replaces Doctor entity FK)
        public string ReferredBy { get; set; }
        public int? DoctorId { get; set; }
        public virtual Doctor Doctor { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual ICollection<TestType> TestTypes { get; set; }

        [NotMapped]
        public string TestNamesSummary
        {
            get
            {
                if (TestTypes == null || TestTypes.Count == 0) return "";
                var names = new List<string>();
                foreach (var t in TestTypes)
                {
                    names.Add(t.Name);
                }
                return string.Join(", ", names);
            }
        }
    }
}