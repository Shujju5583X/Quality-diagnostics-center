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

        public async Task<IEnumerable<TestType>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking()
                         .Where(t => t.IsActive)
                         .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<TestType>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(category))
                return await GetAllAsync(cancellationToken);

            return await _dbSet.AsNoTracking()
                         .Where(t => t.Category == category)
                         .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<TestType>> GetByGroupNameAsync(string groupName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                return await GetAllAsync(cancellationToken);

            return await _dbSet.AsNoTracking()
                         .Where(t => t.GroupName == groupName)
                         .OrderBy(t => t.SortOrder)
                         .ThenBy(t => t.Name)
                         .ToListAsync(cancellationToken);
        }
    }
}