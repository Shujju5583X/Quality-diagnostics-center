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

    public interface IAuthService
    {
        Task<bool> VerifyPinAsync(int staffId, string pin, CancellationToken cancellationToken = default);
        string HashPin(string pin);
    }
    
    public interface IOrderService
    {
        Task CreateOrderAsync(TestOrder order, CancellationToken cancellationToken = default);
        Task UpdateOrderStatusAsync(int orderId, string status, CancellationToken cancellationToken = default);
    }
    
    public interface IResultService
    {
        Task AddResultAsync(Result result, CancellationToken cancellationToken = default);
    }
}
