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

        public override async Task<TestOrder> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                         .Include(o => o.Patient)
                         .Include(o => o.Doctor)
                         .Include(o => o.Specimens)
                         .Include(o => o.TestTypes)
                         .FirstOrDefaultAsync(o => o.OrderId == id, cancellationToken);
        }

        public override async Task<IEnumerable<TestOrder>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking()
                         .Include(o => o.Patient)
                         .Include(o => o.Doctor)
                         .Include(o => o.Specimens)
                         .Include(o => o.TestTypes)
                         .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<TestOrder>> GetOrdersForPatientAsync(int patientId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking()
                         .Include(o => o.Patient)
                         .Include(o => o.Doctor)
                         .Include(o => o.Specimens)
                         .Include(o => o.TestTypes)
                         .Where(o => o.PatientId == patientId)
                         .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<TestOrder>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking()
                         .Include(o => o.Patient)
                         .Include(o => o.Doctor)
                         .Include(o => o.Specimens)
                         .Include(o => o.TestTypes)
                         .Where(o => o.Status == status)
                         .ToListAsync(cancellationToken);
        }

        public async Task AddOrderWithTestTypesAsync(TestOrder order, List<int> testTypeIds, CancellationToken cancellationToken = default)
        {
            foreach (var id in testTypeIds)
            {
                var testType = _context.TestTypes.Local.FirstOrDefault(t => t.TypeId == id);
                if (testType == null)
                {
                    testType = new TestType { TypeId = id };
                    _context.TestTypes.Attach(testType);
                }
                order.TestTypes.Add(testType);
            }

            _dbSet.Add(order);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
