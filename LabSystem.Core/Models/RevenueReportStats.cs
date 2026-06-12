namespace LabSystem.Core.Models
{
    public class RevenueReportStats
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal OutstandingAmount { get; set; }
        public decimal CashCollected { get; set; }
        public decimal UpiCollected { get; set; }
    }
}
