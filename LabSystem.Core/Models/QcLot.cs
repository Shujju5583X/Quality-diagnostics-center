using System;

namespace LabSystem.Core.Models
{
    public class QcLot
    {
        public int QcLotId { get; set; }
        public int TestTypeId { get; set; }
        public virtual TestType TestType { get; set; }
        public string LotNumber { get; set; }
        public double TargetValue { get; set; }
        public double SD { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}
