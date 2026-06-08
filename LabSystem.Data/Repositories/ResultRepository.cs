using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Data.Repositories
{
    public class ResultRepository : Repository<Result>, IResultRepository
    {
        public ResultRepository(LabDbContext context) : base(context) { }

        public IEnumerable<Result> GetResultsForOrder(int orderId)
        {
            return _dbSet.Include(r => r.TestType)
                         .Include(r => r.Technician)
                         .Where(r => r.OrderId == orderId)
                         .ToList();
        }
    }
}
