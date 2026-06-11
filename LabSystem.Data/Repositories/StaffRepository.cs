using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Data.Repositories
{
    public class StaffRepository : Repository<Staff>, IStaffRepository
    {
        public StaffRepository(LabDbContext context) : base(context) { }

        public async Task<Staff> GetByFullNameAsync(string fullName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return null;

            return await _dbSet.AsNoTracking()
                         .FirstOrDefaultAsync(s => s.FullName == fullName, cancellationToken);
        }

        public async Task<IEnumerable<Staff>> GetByRoleAsync(string role, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(role))
                return await GetAllAsync(cancellationToken);

            return await _dbSet.AsNoTracking()
                         .Where(s => s.Role == role)
                         .ToListAsync(cancellationToken);
        }
    }
}