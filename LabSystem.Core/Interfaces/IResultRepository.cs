using System.Collections.Generic;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface IResultRepository : IRepository<Result>
    {
        IEnumerable<Result> GetResultsForOrder(int orderId);
    }
}
