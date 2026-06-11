using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface IStaffRepository : IRepository<Staff>
    {
        Task<Staff> GetByFullNameAsync(string fullName, CancellationToken cancellationToken = default);
        Task<IEnumerable<Staff>> GetByRoleAsync(string role, CancellationToken cancellationToken = default);
    }
}