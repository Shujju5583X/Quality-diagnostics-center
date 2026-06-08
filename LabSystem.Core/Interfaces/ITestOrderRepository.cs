using System.Collections.Generic;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface ITestOrderRepository : IRepository<TestOrder>
    {
        IEnumerable<TestOrder> GetOrdersForPatient(int patientId);
        IEnumerable<TestOrder> GetByStatus(string status);
    }
}
