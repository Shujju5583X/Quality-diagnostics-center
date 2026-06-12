using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface IPatientRepository : IRepository<Patient>
    {
        Task<IEnumerable<Patient>> SearchByNameAsync(string query, CancellationToken cancellationToken = default);
        Task<IEnumerable<Patient>> SearchPatientsAsync(string query, DateTime? startDate, DateTime? endDate, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<int> GetPatientsCountAsync(string query, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);
        Task<string> GetMaxUhidForYearAsync(int year, CancellationToken cancellationToken = default);
    }
}
