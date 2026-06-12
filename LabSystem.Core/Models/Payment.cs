using System;

namespace LabSystem.Core.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public int InvoiceId { get; set; }
        public virtual Invoice Invoice { get; set; }
        
        public decimal Amount { get; set; }
        
        // e.g. "Cash", "UPI"
        public string PaymentMethod { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}
