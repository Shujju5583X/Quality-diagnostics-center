using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Data.Repositories
{
    public class TestOrderRepository : Repository<TestOrder>, ITestOrderRepository
    {
        public TestOrderRepository(LabDbContext context) : base(context) { }

        public async Task<IEnumerable<TestOrder>> GetOrdersForPatientAsync(int patientId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking()
                         .Include(o => o.Patient)
                         .Where(o => o.PatientId == patientId)
                         .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<TestOrder>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking()
                         .Include(o => o.Patient)
                         .Where(o => o.Status == status)
                         .ToListAsync(cancellationToken);
        }
    }
}
