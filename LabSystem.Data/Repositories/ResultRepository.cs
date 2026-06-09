using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Data.Repositories
{
    public class ResultRepository : Repository<Result>, IResultRepository
    {
        public ResultRepository(LabDbContext context) : base(context) { }

        public async Task<IEnumerable<Result>> GetResultsForOrderAsync(int orderId)
        {
            return await _dbSet.AsNoTracking()
                         .Include(r => r.TestType)
                         .Include(r => r.Technician)
                         .Where(r => r.OrderId == orderId)
                         .ToListAsync();
        }
    }
}
