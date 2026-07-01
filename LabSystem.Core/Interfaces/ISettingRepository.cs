using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface ISettingRepository : IRepository<Setting>
    {
        Task DeleteByKeyAsync(string key, CancellationToken cancellationToken = default(CancellationToken));
    }
}
