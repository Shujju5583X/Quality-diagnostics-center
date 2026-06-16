using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface ITestTypeRepository : IRepository<TestType>
    {
        Task<IEnumerable<TestType>> GetActiveAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<IEnumerable<TestType>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default(CancellationToken));
        Task<IEnumerable<TestType>> GetByGroupNameAsync(string groupName, CancellationToken cancellationToken = default(CancellationToken));
    }
}