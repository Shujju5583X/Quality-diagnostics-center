using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using LabSystem.Core.Enums;

namespace LabSystem.Core.Models
{
    public class TestOrder
    {
        public int OrderId { get; set; }
        public int PatientId { get; set; }
        public virtual Patient Patient { get; set; }
        public DateTime OrderedAt { get; set; }
        public string Status { get; set; }

        [NotMapped]
        public OrderStatus StatusEnum
        {
            get => Enum.TryParse<OrderStatus>(Status, true, out var s) ? s : OrderStatus.Pending;
            set => Status = value.ToString();
        }
        public string Notes { get; set; }

        // Simple string referral field (replaces Doctor entity FK)
        public string ReferredBy { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual ICollection<Specimen> Specimens { get; set; } = new HashSet<Specimen>();
        public virtual ICollection<TestType> TestTypes { get; set; } = new HashSet<TestType>();
    }
}
