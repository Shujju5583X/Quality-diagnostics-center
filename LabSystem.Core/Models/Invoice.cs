using System;

namespace LabSystem.Core.Models
{
    public class Invoice
    {
        public int InvoiceId { get; set; }
        public int OrderId { get; set; }
        public virtual TestOrder Order { get; set; }
        
        public decimal TotalAmount { get; set; }
        public bool IsPaid { get; set; }
        public string PaidAt { get; set; }
        public string CreatedAt { get; set; }
        
        // Allowed values: "Cash", "UPI", or null
        public string PaymentMethod { get; set; }
    }
}
