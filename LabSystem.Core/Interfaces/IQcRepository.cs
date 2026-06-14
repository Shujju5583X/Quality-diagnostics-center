using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface IQcRepository : IRepository<QcRun>
    {
        Task<IEnumerable<QcRun>> GetByDateRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);
        Task<IEnumerable<QcRun>> GetByTestTypeAsync(int testTypeId, CancellationToken cancellationToken = default);
        Task<IEnumerable<QcRun>> GetByTestTypeAndDateRangeAsync(int testTypeId, DateTime start, DateTime end, CancellationToken cancellationToken = default);
        Task<IEnumerable<QcLot>> GetActiveLotsAsync(int testTypeId, CancellationToken cancellationToken = default);
        Task<QcLot> GetLotByNumberAsync(string lotNumber, CancellationToken cancellationToken = default);
        Task AddLotAsync(QcLot lot, CancellationToken cancellationToken = default);
    }
}
