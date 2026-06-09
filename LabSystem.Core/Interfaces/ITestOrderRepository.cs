using System.Collections.Generic;
using System.Threading.Tasks;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface ITestOrderRepository : IRepository<TestOrder>
    {
        Task<IEnumerable<TestOrder>> GetOrdersForPatientAsync(int patientId);
        Task<IEnumerable<TestOrder>> GetByStatusAsync(string status);
    }
}
