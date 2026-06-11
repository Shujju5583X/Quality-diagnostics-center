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
    public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
    {
        public AuditLogRepository(LabDbContext context) : base(context) { }

        public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking()
                         .Include(a => a.User)
                         .Where(a => a.UserId == userId)
                         .OrderByDescending(a => a.Timestamp)
                         .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, int entityId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                return await GetAllAsync(cancellationToken);

            return await _dbSet.AsNoTracking()
                         .Include(a => a.User)
                         .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                         .OrderByDescending(a => a.Timestamp)
                         .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking()
                         .Include(a => a.User)
                         .Where(a => a.Timestamp >= start && a.Timestamp <= end)
                         .OrderByDescending(a => a.Timestamp)
                         .ToListAsync(cancellationToken);
        }
    }
}