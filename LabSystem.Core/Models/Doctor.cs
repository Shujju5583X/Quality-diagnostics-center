namespace LabSystem.Core.Models
{
    public class Doctor
    {
        public int DoctorId { get; set; }
        public string Name { get; set; }
        public string Specialization { get; set; }
        public string ClinicName { get; set; }
        public string ContactPhone { get; set; }
        public decimal CommissionPercent { get; set; }
    }

    public class DoctorReferralStats
    {
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public string Specialization { get; set; }
        public string ClinicName { get; set; }
        public decimal CommissionPercent { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal CommissionPayable { get; set; }
    }
}
