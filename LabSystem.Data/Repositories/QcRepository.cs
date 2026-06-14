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
    public class QcRepository : Repository<QcRun>, IQcRepository
    {
        public QcRepository(LabDbContext context) : base(context) { }

        public async Task<IEnumerable<QcRun>> GetByDateRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
        {
            var endInclusive = end.AddDays(1);
            return await _dbSet.AsNoTracking()
                .Include(r => r.TestType)
                .Where(r => r.RunDate >= start && r.RunDate < endInclusive)
                .OrderByDescending(r => r.RunDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<QcRun>> GetByTestTypeAsync(int testTypeId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking()
                .Include(r => r.TestType)
                .Where(r => r.TestTypeId == testTypeId)
                .OrderByDescending(r => r.RunDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<QcRun>> GetByTestTypeAndDateRangeAsync(int testTypeId, DateTime start, DateTime end, CancellationToken cancellationToken = default)
        {
            var endInclusive = end.AddDays(1);
            return await _dbSet.AsNoTracking()
                .Include(r => r.TestType)
                .Where(r => r.TestTypeId == testTypeId && r.RunDate >= start && r.RunDate < endInclusive)
                .OrderByDescending(r => r.RunDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<QcLot>> GetActiveLotsAsync(int testTypeId, CancellationToken cancellationToken = default)
        {
            return await _context.QcLots
                .AsNoTracking()
                .Include(l => l.TestType)
                .Where(l => l.TestTypeId == testTypeId && l.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task<QcLot> GetLotByNumberAsync(string lotNumber, CancellationToken cancellationToken = default)
        {
            return await _context.QcLots
                .AsNoTracking()
                .Include(l => l.TestType)
                .FirstOrDefaultAsync(l => l.LotNumber == lotNumber, cancellationToken);
        }

        public async Task AddLotAsync(QcLot lot, CancellationToken cancellationToken = default)
        {
            _context.QcLots.Add(lot);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
