using System;

namespace LabSystem.Core.Models
{
    public class QCResult
    {
        public int QCResultId { get; set; }
        public int TestTypeId { get; set; }
        public virtual TestType TestType { get; set; }
        
        public string ControlLevel { get; set; } // e.g., "L1" (Normal), "L2" (Abnormal)
        
        public double ExpectedMean { get; set; }
        public double StandardDeviation { get; set; }
        public double MeasuredValue { get; set; }
        
        public DateTime RecordedAt { get; set; }
        public int TechnicianId { get; set; }
        public virtual Staff Technician { get; set; }
        
        public string Remarks { get; set; }

        // Computed property to quickly check if this QC run failed the 2SD rule
        public bool IsOutOfRange
        {
            get
            {
                // Acceptable range is within +/- 2 Standard Deviations from the mean
                return MeasuredValue > (ExpectedMean + 2 * StandardDeviation) ||
                       MeasuredValue < (ExpectedMean - 2 * StandardDeviation);
            }
        }
    }
}
