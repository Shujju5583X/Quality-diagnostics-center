using System;

namespace LabSystem.Core.Models
{
    public class Report
    {
        public int ReportId { get; set; }
        public int OrderId { get; set; }
        public virtual TestOrder Order { get; set; }
        public string FilePath { get; set; }
        public string GeneratedAt { get; set; }
    }
}
