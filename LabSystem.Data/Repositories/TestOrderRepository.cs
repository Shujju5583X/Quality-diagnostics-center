using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Data.Repositories
{
    public class TestOrderRepository : Repository<TestOrder>, ITestOrderRepository
    {
        public TestOrderRepository(LabDbContext context) : base(context) { }

        public IEnumerable<TestOrder> GetOrdersForPatient(int patientId)
        {
            return _dbSet.AsNoTracking()
                         .Include(o => o.Patient)
                         .Where(o => o.PatientId == patientId)
                         .ToList();
        }

        public IEnumerable<TestOrder> GetByStatus(string status)
        {
            return _dbSet.AsNoTracking()
                         .Include(o => o.Patient)
                         .Where(o => o.Status == status)
                         .ToList();
        }
    }
}
