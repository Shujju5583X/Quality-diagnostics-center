using System.Threading.Tasks;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface IPdfReportService
    {
        Task<string> GenerateReportAsync(TestOrder order);
    }

    public interface IBackupService
    {
        Task BackupNowAsync();
    }
}

    public interface IAuthService
    {
        Task<bool> VerifyPinAsync(int staffId, string pin);
        string HashPin(string pin);
    }
    
    public interface IOrderService
    {
        Task CreateOrderAsync(TestOrder order);
        Task UpdateOrderStatusAsync(int orderId, string status);
    }
    
    public interface IResultService
    {
        Task AddResultAsync(Result result);
    }
