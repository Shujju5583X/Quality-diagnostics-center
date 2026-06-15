using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface IResultRepository : IRepository<Result>
    {
        Task<IEnumerable<Result>> GetResultsForOrderAsync(int orderId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Result>> GetPatientHistoryAsync(int patientId, int testTypeId);
        Task<int> CountAbnormalAsync(CancellationToken cancellationToken = default);
    }
}
