using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Data.Repositories
{
    public class InvoiceRepository : Repository<Invoice>, IInvoiceRepository
    {
        public InvoiceRepository(LabDbContext context) : base(context) { }

        public async Task<Invoice> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking()
                         .Include(i => i.Order)
                         .Include(i => i.Order.Patient)
                         .Include(i => i.Payments)
                         .FirstOrDefaultAsync(i => i.OrderId == orderId, cancellationToken);
        }

        public async Task<IEnumerable<Invoice>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking()
                         .Include(i => i.Order)
                         .Include(i => i.Order.Patient)
                         .Include(i => i.Payments)
                         .ToListAsync(cancellationToken);
        }
    }
}
