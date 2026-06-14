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
    public class PatientRepository : Repository<Patient>, IPatientRepository
    {
        public PatientRepository(LabDbContext context) : base(context) { }

        public async Task<IEnumerable<Patient>> SearchByNameAsync(string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                return await GetAllAsync(cancellationToken);
            return await _dbSet.Where(p => p.FullName.Contains(query)).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Patient>> SearchPatientsAsync(string query, DateTime? startDate, DateTime? endDate, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var q = _dbSet.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                q = q.Where(p => p.FullName.Contains(query) || (p.ContactPhone != null && p.ContactPhone.Contains(query)) || p.PatientId.ToString().Equals(query) || p.Uhid.Contains(query));
            }

            if (startDate.HasValue)
            {
                var sd = startDate.Value.Date;
                q = q.Where(p => p.CreatedAt >= sd);
            }

            if (endDate.HasValue)
            {
                var ed = endDate.Value.Date.AddDays(1);
                q = q.Where(p => p.CreatedAt < ed);
            }

            return await q.OrderByDescending(p => p.PatientId)
                          .Skip((page - 1) * pageSize)
                          .Take(pageSize)
                          .ToListAsync(cancellationToken);
        }

        public async Task<int> GetPatientsCountAsync(string query, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
        {
            var q = _dbSet.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                q = q.Where(p => p.FullName.Contains(query) || (p.ContactPhone != null && p.ContactPhone.Contains(query)) || p.PatientId.ToString().Equals(query) || p.Uhid.Contains(query));
            }

            if (startDate.HasValue)
            {
                var sd = startDate.Value.Date;
                q = q.Where(p => p.CreatedAt >= sd);
            }

            if (endDate.HasValue)
            {
                var ed = endDate.Value.Date.AddDays(1);
                q = q.Where(p => p.CreatedAt < ed);
            }

            return await q.CountAsync(cancellationToken);
        }

        public async Task<string> GetMaxUhidForYearAsync(int year, CancellationToken cancellationToken = default)
        {
            string prefix = $"QDC-{year}-";
            var match = await _dbSet.AsNoTracking()
                .Where(p => p.Uhid.StartsWith(prefix))
                .OrderByDescending(p => p.Uhid)
                .Select(p => p.Uhid)
                .FirstOrDefaultAsync(cancellationToken);
            return match;
        }
    }
}
