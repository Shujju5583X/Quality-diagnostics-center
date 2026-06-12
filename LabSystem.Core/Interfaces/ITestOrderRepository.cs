using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface ITestOrderRepository : IRepository<TestOrder>
    {
        Task<IEnumerable<TestOrder>> GetOrdersForPatientAsync(int patientId, CancellationToken cancellationToken = default);
        Task<IEnumerable<TestOrder>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
        Task AddOrderWithTestTypesAsync(TestOrder order, List<int> testTypeIds, CancellationToken cancellationToken = default);
    }
}
