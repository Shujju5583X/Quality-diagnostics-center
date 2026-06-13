using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface IWorkflowService
    {
        Task QuickFinalizeAsync(int orderId, List<Result> results, int technicianId, string paymentMethod, CancellationToken cancellationToken = default);
    }
}
