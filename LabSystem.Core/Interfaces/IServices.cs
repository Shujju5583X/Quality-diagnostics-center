using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface IPdfReportService
    {
        Task<string> GenerateReportAsync(TestOrder order, bool includeLetterhead = true, CancellationToken cancellationToken = default);
        Task<string> GenerateInvoicePdfAsync(Invoice invoice, CancellationToken cancellationToken = default);
    }

    public interface IBackupService
    {
        Task BackupNowAsync(CancellationToken cancellationToken = default);
    }

    public interface IOrderService
    {
        Task CreateOrderAsync(TestOrder order, List<int> testTypeIds, CancellationToken cancellationToken = default);
        Task UpdateOrderStatusAsync(int orderId, string status, CancellationToken cancellationToken = default);
    }

    public interface IResultService
    {
        Task AddResultAsync(Result result, CancellationToken cancellationToken = default);
        Task AmendResultAsync(int resultId, double newValue, string reason, int technicianId, CancellationToken cancellationToken = default);
    }
}
