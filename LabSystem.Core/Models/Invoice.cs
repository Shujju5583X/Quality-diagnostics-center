using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabSystem.Core.Models
{
    public class Invoice
    {
        public Invoice()
        {
            DiscountAmount = 0;
            TaxAmount = 0;
            AmountPaid = 0;
            DiscountPercent = 0;
            TaxPercent = 0;
            Status = "Pending";
        }

        public int InvoiceId { get; set; }
        public int OrderId { get; set; }
        public virtual TestOrder Order { get; set; }

        public decimal TotalAmount { get; set; }

        // Flat amounts (stored in DB)
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal AmountPaid { get; set; }

        // Percentage helpers (not mapped)
        [NotMapped]
        public decimal DiscountPercent { get; set; }
        [NotMapped]
        public decimal TaxPercent { get; set; }

        [NotMapped]
        public decimal GrandTotal
        {
            get { return TotalAmount - DiscountAmount + TaxAmount; }
        }

        [NotMapped]
        public decimal DueAmount
        {
            get { return GrandTotal - AmountPaid; }
        }

        public string Status { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Allowed values: "Cash", "UPI", or null
        public string PaymentMethod { get; set; }

        public virtual ICollection<Payment> Payments { get; set; }
    }
}