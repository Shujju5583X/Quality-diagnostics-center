using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Data.Repositories
{
    public class ResultRepository : Repository<Result>, IResultRepository
    {
        public ResultRepository(LabDbContext context) : base(context) { }

        public async Task<IEnumerable<Result>> GetResultsForOrderAsync(int orderId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking()
                         .Include(r => r.TestType)
                         .Include(r => r.Technician)
                         .Where(r => r.OrderId == orderId)
                         .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Result>> GetPatientHistoryAsync(int patientId, int testTypeId)
        {
            return await _dbSet.AsNoTracking()
                         .Include(r => r.Order)
                         .Include(r => r.TestType)
                         .Where(r => r.Order.PatientId == patientId && r.TypeId == testTypeId && r.Value != null)
                         .OrderBy(r => r.RecordedAt)
                         .ToListAsync();
        }

        public async Task<int> CountAbnormalAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.CountAsync(r => r.IsAbnormal, cancellationToken);
        }
    }
}
