using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Data.Repositories
{
    public class TestTypeRepository : Repository<TestType>, ITestTypeRepository
    {
        public TestTypeRepository(LabDbContext context) : base(context) { }

        public override async Task<TestType> GetByIdAsync(int id, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _dbSet
                         .Include(t => t.ReferenceRanges)
                         .FirstOrDefaultAsync(t => t.TypeId == id, cancellationToken);
        }

        public override async Task<IEnumerable<TestType>> GetAllAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _dbSet.AsNoTracking()
                         .Include(t => t.ReferenceRanges)
                         .ToListAsync(cancellationToken);
        }
    }
}