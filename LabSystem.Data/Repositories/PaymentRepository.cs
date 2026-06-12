using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Data.Repositories
{
    public class PaymentRepository : Repository<Payment>, IPaymentRepository
    {
        public PaymentRepository(LabDbContext context) : base(context) { }

        public async Task<IEnumerable<Payment>> GetByInvoiceIdAsync(int invoiceId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking()
                         .Where(p => p.InvoiceId == invoiceId)
                         .ToListAsync(cancellationToken);
        }
    }
}
