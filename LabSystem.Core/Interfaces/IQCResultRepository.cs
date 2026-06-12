using LabSystem.Core.Models;
using System.Threading;
using System.Threading.Tasks;

namespace LabSystem.Core.Interfaces
{
    public interface IQCResultRepository : IRepository<QCResult>
    {
        Task<QCResult> GetLatestQCAsync(int testTypeId, CancellationToken cancellationToken = default);
    }
}
