using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface IBillingService
    {
        Task<Invoice> GenerateInvoiceAsync(int orderId);
        Task<Invoice> GetInvoiceForOrderAsync(int orderId);
        Task<IEnumerable<Invoice>> GetAllInvoicesAsync();
        Task UpdateInvoiceFinancialsAsync(int invoiceId, decimal discountAmount, decimal taxAmount);
        Task AddPaymentAsync(int invoiceId, decimal amount, string paymentMethod);
        Task<RevenueReportStats> GetRevenueReportAsync(DateTime start, DateTime end);
        Task VoidInvoiceAsync(int invoiceId, CancellationToken cancellationToken = default(CancellationToken));
        Task VoidPaymentAsync(int paymentId, CancellationToken cancellationToken = default(CancellationToken));
    }

    public interface IPdfReportService
    {
        Task<string> GenerateReportAsync(TestOrder order, bool includeLetterhead = true, CancellationToken cancellationToken = default(CancellationToken));
        Task<string> GenerateInvoicePdfAsync(Invoice invoice, CancellationToken cancellationToken = default(CancellationToken));
    }

    public interface IBackupService
    {
        Task BackupNowAsync(CancellationToken cancellationToken = default(CancellationToken));
    }

    public interface ICsvBackupService
    {
        Task ExportToCsvAsync(string filePath, CancellationToken cancellationToken = default(CancellationToken));
        Task ImportFromCsvAsync(string filePath, CancellationToken cancellationToken = default(CancellationToken));
    }

    public interface IOrderService
    {
        Task CreateOrderAsync(TestOrder order, List<int> testTypeIds, int operatorStaffId = 1, CancellationToken cancellationToken = default(CancellationToken));
        Task UpdateOrderStatusAsync(int orderId, string status, CancellationToken cancellationToken = default(CancellationToken));
        Task UpdateOrderAsync(int orderId, string notes, string referredBy, CancellationToken cancellationToken = default(CancellationToken));
        Task VoidOrderAsync(int orderId, CancellationToken cancellationToken = default(CancellationToken));
    }

    public interface IResultService
    {
        Task AddResultAsync(Result result, CancellationToken cancellationToken = default(CancellationToken));
        Task AmendResultAsync(int resultId, double? newValue, string valueText, string reason, int technicianId, CancellationToken cancellationToken = default(CancellationToken));
        Task DeleteResultAsync(int resultId, string reason, CancellationToken cancellationToken = default(CancellationToken));
    }
}
