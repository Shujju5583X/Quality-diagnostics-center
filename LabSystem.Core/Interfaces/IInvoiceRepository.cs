using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface IInvoiceRepository : IRepository<Invoice>
    {
        Task<Invoice> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Invoice>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
    }
}
