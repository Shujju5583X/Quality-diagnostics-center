using System;

namespace LabSystem.Core.Models
{
    public class DoctorCommission
    {
        public DoctorCommission()
        {
            Status = "Unpaid";
            CreatedAt = DateTime.Now;
        }

        public int CommissionId { get; set; }
        public int DoctorId { get; set; }
        public virtual Doctor Doctor { get; set; }
        public int InvoiceId { get; set; }
        public virtual Invoice Invoice { get; set; }
        public double CommissionAmount { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}