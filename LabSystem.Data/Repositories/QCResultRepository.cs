using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Data.Repositories
{
    public class QCResultRepository : Repository<QCResult>, IQCResultRepository
    {
        public QCResultRepository(LabDbContext context) : base(context) { }

        public async Task<QCResult> GetLatestQCAsync(int testTypeId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking()
                         .Where(q => q.TestTypeId == testTypeId)
                         .OrderByDescending(q => q.RecordedAt)
                         .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
