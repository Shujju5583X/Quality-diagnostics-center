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

        // Flat amounts (stored in DB)
        public decimal DiscountAmount { get; set; } = 0;
        public decimal TaxAmount { get; set; } = 0;
        public decimal AmountPaid { get; set; } = 0;

        // Percentage helpers (not mapped)
        [NotMapped]
        public decimal DiscountPercent { get; set; } = 0;
        [NotMapped]
        public decimal TaxPercent { get; set; } = 0;

        [NotMapped]
        public decimal GrandTotal => TotalAmount - DiscountAmount + TaxAmount;

        [NotMapped]
        public decimal DueAmount => GrandTotal - AmountPaid;

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
