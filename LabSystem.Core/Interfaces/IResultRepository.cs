using System.Collections.Generic;
using System.Threading.Tasks;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface IResultRepository : IRepository<Result>
    {
        Task<IEnumerable<Result>> GetResultsForOrderAsync(int orderId);
    }
}
