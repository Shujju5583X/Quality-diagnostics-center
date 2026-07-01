using System.Data.Entity;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Data.Repositories
{
    public class SettingRepository : Repository<Setting>, ISettingRepository
    {
        public SettingRepository(LabDbContext context) : base(context)
        {
        }

        public async Task DeleteByKeyAsync(string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            var entity = await _dbSet.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
