using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Data.Repositories
{
    public class TestOrderRepository : Repository<TestOrder>, ITestOrderRepository
    {
        public TestOrderRepository(LabDbContext context) : base(context) { }

        public async Task<IEnumerable<TestOrder>> GetOrdersForPatientAsync(int patientId)
        {
            return await _dbSet.AsNoTracking()
                         .Include(o => o.Patient)
                         .Where(o => o.PatientId == patientId)
                         .ToListAsync();
        }

        public async Task<IEnumerable<TestOrder>> GetByStatusAsync(string status)
        {
            return await _dbSet.AsNoTracking()
                         .Include(o => o.Patient)
                         .Where(o => o.Status == status)
                         .ToListAsync();
        }
    }
}
