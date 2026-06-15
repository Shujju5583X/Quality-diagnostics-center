using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabSystem.Core.Models
{
    public class Invoice
    {
        public int InvoiceId { get; set; }
        public int OrderId { get; set; }
        public virtual TestOrder Order { get; set; }

        public decimal TotalAmount { get; set; }

        // Percent-based discount and tax (stored in DB)
        public decimal DiscountPercent { get; set; } = 0;
        public decimal TaxPercent { get; set; } = 0;

        // Computed properties (not mapped to DB)
        [NotMapped]
        public decimal DiscountAmount => TotalAmount * DiscountPercent / 100m;

        [NotMapped]
        public decimal TaxAmount => (TotalAmount - DiscountAmount) * TaxPercent / 100m;

        [NotMapped]
        public decimal GrandTotal => TotalAmount - DiscountAmount + TaxAmount;

        public bool IsPaid { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Allowed values: "Cash", "UPI", or null
        public string PaymentMethod { get; set; }
        public int BranchId { get; set; } = 1;

        public virtual ICollection<Payment> Payments { get; set; }
    }
}
