using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface IPdfReportService
    {
        string GenerateReport(TestOrder order);
    }

    public interface IBackupService
    {
        void BackupNow();
    }
}

    public interface IAuthService
    {
        bool VerifyPin(int staffId, string pin);
        string HashPin(string pin);
    }
    
    public interface IOrderService
    {
        void CreateOrder(TestOrder order);
        void UpdateOrderStatus(int orderId, string status);
    }
    
    public interface IResultService
    {
        void AddResult(Result result);
    }
