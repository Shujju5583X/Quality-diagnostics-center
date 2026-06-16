using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Data.Repositories
{
    public class ReportRepository : Repository<Report>, IReportRepository
    {
        public ReportRepository(LabDbContext context) : base(context) { }

        public async Task<IEnumerable<Report>> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _dbSet.AsNoTracking()
                         .Where(r => r.OrderId == orderId)
                         .OrderByDescending(r => r.GeneratedAt)
                         .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Report>> GetByDateRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _dbSet.AsNoTracking()
                         .Where(r => r.GeneratedAt >= start && r.GeneratedAt <= end)
                         .OrderByDescending(r => r.GeneratedAt)
                         .ToListAsync(cancellationToken);
        }
    }
}