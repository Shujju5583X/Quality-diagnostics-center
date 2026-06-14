using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface IAppointmentRepository : IRepository<Appointment>
    {
        Task<IEnumerable<Appointment>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default);
        Task<IEnumerable<Appointment>> GetByDateRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);
        Task<IEnumerable<Appointment>> GetByPatientAsync(int patientId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Appointment>> GetUpcomingAsync(CancellationToken cancellationToken = default);
    }
}