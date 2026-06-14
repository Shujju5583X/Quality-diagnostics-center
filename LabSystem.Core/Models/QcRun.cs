using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabSystem.Core.Models
{
    public class QcRun
    {
        public int QcRunId { get; set; }
        public int TestTypeId { get; set; }
        public virtual TestType TestType { get; set; }
        public string ControlName { get; set; }
        public DateTime RunDate { get; set; }
        public double MeasuredValue { get; set; }
        public string LotNumber { get; set; }
        public double TargetValue { get; set; }
        public double SD { get; set; }
        public DateTime CreatedAt { get; set; }

        [NotMapped]
        public string Status { get; set; }
    }
}
