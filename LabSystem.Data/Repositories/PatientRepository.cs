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
    }
}
