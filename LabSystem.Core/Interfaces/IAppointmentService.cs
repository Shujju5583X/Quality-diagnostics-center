using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface IAppointmentService
    {
        Task<Appointment> BookAppointmentAsync(Appointment appointment, CancellationToken cancellationToken = default);
        Task CancelAppointmentAsync(int appointmentId, CancellationToken cancellationToken = default);
        Task MarkNoShowAsync(int appointmentId, CancellationToken cancellationToken = default);
        Task CompleteAsync(int appointmentId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Appointment>> GetAppointmentsByDateAsync(DateTime date, CancellationToken cancellationToken = default);
        Task<IEnumerable<Appointment>> GetAppointmentsByPatientAsync(int patientId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Appointment>> GetUpcomingAppointmentsAsync(CancellationToken cancellationToken = default);
    }
}