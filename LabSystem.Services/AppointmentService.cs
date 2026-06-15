using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _appointmentRepo;

        public AppointmentService(IAppointmentRepository appointmentRepo)
        {
            _appointmentRepo = appointmentRepo;
        }

        public async Task<Appointment> BookAppointmentAsync(Appointment appointment, CancellationToken cancellationToken = default)
        {
            if (appointment.PatientId <= 0)
                throw new ArgumentException("Patient is required.");
            if (appointment.AppointmentDate <= DateTime.UtcNow.AddMinutes(-1))
                throw new ArgumentException("Appointment date must be in the future.");
            if (string.IsNullOrWhiteSpace(appointment.Purpose))
                throw new ArgumentException("Purpose is required.");
            if (appointment.DurationMinutes <= 0)
                appointment.DurationMinutes = 15;

            var existing = await _appointmentRepo.GetByDateAsync(appointment.AppointmentDate.Date, cancellationToken);
            var overlap = existing.Any(a => a.Status == "Scheduled"
                && a.AppointmentDate < appointment.AppointmentDate.AddMinutes(appointment.DurationMinutes)
                && appointment.AppointmentDate < a.AppointmentDate.AddMinutes(a.DurationMinutes));

            if (overlap)
                throw new InvalidOperationException("Time slot is already booked.");

            appointment.Status = "Scheduled";
            appointment.CreatedAt = DateTime.UtcNow;
            appointment.UpdatedAt = DateTime.UtcNow;
            await _appointmentRepo.AddAsync(appointment, cancellationToken);

            return appointment;
        }

        public async Task CancelAppointmentAsync(int appointmentId, CancellationToken cancellationToken = default)
        {
            var appointment = await _appointmentRepo.GetByIdAsync(appointmentId, cancellationToken);
            if (appointment == null)
                throw new InvalidOperationException("Appointment not found.");

            appointment.Status = "Cancelled";
            appointment.UpdatedAt = DateTime.UtcNow;
            await _appointmentRepo.UpdateAsync(appointment, cancellationToken);
        }

        public async Task MarkNoShowAsync(int appointmentId, CancellationToken cancellationToken = default)
        {
            var appointment = await _appointmentRepo.GetByIdAsync(appointmentId, cancellationToken);
            if (appointment == null)
                throw new InvalidOperationException("Appointment not found.");

            appointment.Status = "NoShow";
            appointment.UpdatedAt = DateTime.UtcNow;
            await _appointmentRepo.UpdateAsync(appointment, cancellationToken);
        }

        public async Task CompleteAsync(int appointmentId, CancellationToken cancellationToken = default)
        {
            var appointment = await _appointmentRepo.GetByIdAsync(appointmentId, cancellationToken);
            if (appointment == null)
                throw new InvalidOperationException("Appointment not found.");

            appointment.Status = "Completed";
            appointment.UpdatedAt = DateTime.UtcNow;
            await _appointmentRepo.UpdateAsync(appointment, cancellationToken);
        }

        public async Task<IEnumerable<Appointment>> GetAppointmentsByDateAsync(DateTime date, CancellationToken cancellationToken = default)
        {
            return await _appointmentRepo.GetByDateAsync(date, cancellationToken);
        }

        public async Task<IEnumerable<Appointment>> GetAppointmentsByPatientAsync(int patientId, CancellationToken cancellationToken = default)
        {
            return await _appointmentRepo.GetByPatientAsync(patientId, cancellationToken);
        }

        public async Task<IEnumerable<Appointment>> GetUpcomingAppointmentsAsync(CancellationToken cancellationToken = default)
        {
            return await _appointmentRepo.GetUpcomingAsync(cancellationToken);
        }
    }
}