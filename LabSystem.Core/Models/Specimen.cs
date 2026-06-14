using System;
using System.ComponentModel.DataAnnotations.Schema;
using LabSystem.Core.Enums;

namespace LabSystem.Core.Models
{
    public class Specimen
    {
        public int SpecimenId { get; set; }
        public int OrderId { get; set; }
        public virtual TestOrder Order { get; set; }
        public string Barcode { get; set; }
        public string SampleType { get; set; }
        public DateTime? CollectionTime { get; set; }
        public string CollectedBy { get; set; }
        public string Status { get; set; } // "Collected", "Received", "Processing", "Completed", "Rejected"

        [NotMapped]
        public SpecimenStatus StatusEnum
        {
            get => Enum.TryParse<SpecimenStatus>(Status, true, out var s) ? s : SpecimenStatus.Pending;
            set => Status = value.ToString();
        }
        public string RejectionReason { get; set; }
    }
}
