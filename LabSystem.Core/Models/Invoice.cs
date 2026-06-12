using System;
using System.Collections.Generic;

namespace LabSystem.Core.Models
{
    public class Invoice
    {
        public int InvoiceId { get; set; }
        public int OrderId { get; set; }
        public virtual TestOrder Order { get; set; }
        
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }

        public bool IsPaid { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Allowed values: "Cash", "UPI", or null
        public string PaymentMethod { get; set; }
        
        public virtual ICollection<Payment> Payments { get; set; }
    }
}
