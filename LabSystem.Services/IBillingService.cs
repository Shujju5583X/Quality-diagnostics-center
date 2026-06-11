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
        Task MarkAsPaidAsync(int invoiceId, string paymentMethod);
    }
}
