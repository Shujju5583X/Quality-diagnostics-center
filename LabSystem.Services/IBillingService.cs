using System.Collections.Generic;
using System.Threading.Tasks;
using LabSystem.Core.Models;

namespace LabSystem.Services
{
    public interface IBillingService
    {
        Task<Invoice> GenerateInvoiceAsync(int orderId);
        Task<Invoice> GetInvoiceForOrderAsync(int orderId);
        Task<IEnumerable<Invoice>> GetAllInvoicesAsync();
        Task UpdateInvoiceFinancialsAsync(int invoiceId, decimal discount, decimal tax);
        Task AddPaymentAsync(int invoiceId, decimal amount, string paymentMethod);
    }
}
