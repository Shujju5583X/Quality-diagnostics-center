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

        public override async Task<TestOrder> GetByIdAsync(int id, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _dbSet
                         .Include(o => o.Patient)
                         .Include(o => o.TestTypes)
                         .FirstOrDefaultAsync(o => o.OrderId == id, cancellationToken);
        }

        public override async Task<IEnumerable<TestOrder>> GetAllAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _dbSet.AsNoTracking()
                         .Include(o => o.Patient)
                         .Include(o => o.TestTypes)
                         .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<TestOrder>> GetOrdersForPatientAsync(int patientId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _dbSet.AsNoTracking()
                         .Include(o => o.Patient)
                         .Include(o => o.TestTypes)
                         .Where(o => o.PatientId == patientId)
                         .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<TestOrder>> GetByStatusAsync(string status, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _dbSet.AsNoTracking()
                         .Include(o => o.Patient)
                         .Include(o => o.TestTypes)
                         .Where(o => o.Status == status)
                         .ToListAsync(cancellationToken);
        }

        public async Task AddOrderWithTestTypesAsync(TestOrder order, List<int> testTypeIds, CancellationToken cancellationToken = default(CancellationToken))
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

        public async Task UpdateOrderTestPricingAsync(int orderId, int typeId, int? packageId, double billedCost, CancellationToken cancellationToken = default(CancellationToken))
        {
            var parameters = new object[]
            {
                new System.Data.SQLite.SQLiteParameter("@packageId", (object)packageId ?? System.DBNull.Value),
                new System.Data.SQLite.SQLiteParameter("@billedCost", billedCost),
                new System.Data.SQLite.SQLiteParameter("@orderId", orderId),
                new System.Data.SQLite.SQLiteParameter("@typeId", typeId)
            };

            await _context.Database.ExecuteSqlCommandAsync(
                "UPDATE OrderTestTypes SET PackageId = @packageId, BilledCost = @billedCost WHERE OrderId = @orderId AND TypeId = @typeId", 
                cancellationToken, 
                parameters);
        }
    }
}
