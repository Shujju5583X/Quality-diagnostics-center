using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface ITestOrderRepository : IRepository<TestOrder>
    {
        Task<IEnumerable<TestOrder>> GetOrdersForPatientAsync(int patientId, CancellationToken cancellationToken = default(CancellationToken));
        Task<IEnumerable<TestOrder>> GetByStatusAsync(string status, CancellationToken cancellationToken = default(CancellationToken));
        Task AddOrderWithTestTypesAsync(TestOrder order, List<int> testTypeIds, CancellationToken cancellationToken = default(CancellationToken));
        Task UpdateOrderTestPricingAsync(int orderId, int typeId, int? packageId, decimal billedCost, CancellationToken cancellationToken = default(CancellationToken));
    }
}
