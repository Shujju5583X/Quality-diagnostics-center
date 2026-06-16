using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LabSystem.Core.Interfaces
{
    public interface IRepository<T>
    {
        Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default(CancellationToken));
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task AddAsync(T entity, CancellationToken cancellationToken = default(CancellationToken));
        Task UpdateAsync(T entity, CancellationToken cancellationToken = default(CancellationToken));
        Task DeleteAsync(int id, CancellationToken cancellationToken = default(CancellationToken));
    }
}
