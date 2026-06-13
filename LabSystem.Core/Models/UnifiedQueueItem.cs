using System;
using LabSystem.Core.Enums;

namespace LabSystem.Core.Models
{
    public class UnifiedQueueItem
    {
        public int OrderId { get; set; }
        public string PatientName { get; set; }
        public DateTime OrderedAt { get; set; }
        public string OrderStatus { get; set; }
        
        public bool HasAllResults { get; set; }
        public bool IsPaid { get; set; }
        public int? InvoiceId { get; set; }

        public UnifiedWorkflowState WorkflowState
        {
            get
            {
                if (!HasAllResults && !IsPaid) return UnifiedWorkflowState.AwaitingResults_Unpaid;
                if (!HasAllResults && IsPaid) return UnifiedWorkflowState.AwaitingResults_Paid;
                if (HasAllResults && !IsPaid) return UnifiedWorkflowState.ResultsReady_Unpaid;
                return UnifiedWorkflowState.Completed_Paid;
            }
        }
    }
}
