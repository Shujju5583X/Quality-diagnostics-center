using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Data.Repositories
{
    public class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
    {
        public AppointmentRepository(LabDbContext context) : base(context) { }

        public async Task<IEnumerable<Appointment>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)
        {
            var end = date.Date.AddDays(1);
            return await _dbSet
                .Where(a => a.AppointmentDate >= date.Date && a.AppointmentDate < end)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Appointment>> GetByDateRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(a => a.AppointmentDate >= start && a.AppointmentDate < end)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Appointment>> GetByPatientAsync(int patientId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Appointment>> GetUpcomingAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(a => a.AppointmentDate >= DateTime.UtcNow && a.Status == "Scheduled")
                .OrderBy(a => a.AppointmentDate)
                .Take(50)
                .ToListAsync(cancellationToken);
        }
    }
}