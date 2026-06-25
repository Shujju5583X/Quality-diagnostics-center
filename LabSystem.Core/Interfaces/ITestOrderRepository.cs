using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface ITestOrderRepository : IRepository<TestOrder>
    {
        Task AddOrderWithTestTypesAsync(TestOrder order, List<int> testTypeIds, CancellationToken cancellationToken = default(CancellationToken));
        Task UpdateOrderTestPricingAsync(int orderId, int typeId, int? packageId, decimal billedCost, CancellationToken cancellationToken = default(CancellationToken));
        Task<IEnumerable<TestOrder>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default(CancellationToken));
        Task<int> GetCountAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<int> GetDailySequenceNumberAsync(int orderId, System.DateTime orderedAt, CancellationToken cancellationToken = default(CancellationToken));
    }
}
