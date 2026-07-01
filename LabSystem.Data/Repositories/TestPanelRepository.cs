using System.Collections.Generic;
using System.Data.Entity;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Models;

namespace LabSystem.Data.Repositories
{
    public class TestPanelRepository : Repository<TestPanel>
    {
        public TestPanelRepository(LabDbContext context) : base(context) { }

        public override async Task<IEnumerable<TestPanel>> GetAllAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _dbSet.AsNoTracking()
                         .Include(p => p.TestTypes)
                         .ToListAsync(cancellationToken);
        }

        public override async Task<TestPanel> GetByIdAsync(int id, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _dbSet
                         .Include(p => p.TestTypes)
                         .FirstOrDefaultAsync(p => p.PanelId == id, cancellationToken);
        }

        public async Task UpdatePanelTestTypesAsync(int panelId, List<int> testTypeIds, CancellationToken cancellationToken = default(CancellationToken))
        {
            await _context.Database.ExecuteSqlCommandAsync(
                "DELETE FROM PanelTestTypes WHERE PanelId = @panelId",
                new System.Data.SQLite.SQLiteParameter("@panelId", panelId)
            );

            foreach (var typeId in testTypeIds)
            {
                await _context.Database.ExecuteSqlCommandAsync(
                    "INSERT INTO PanelTestTypes (PanelId, TypeId) VALUES (@panelId, @typeId)",
                    new System.Data.SQLite.SQLiteParameter("@panelId", panelId),
                    new System.Data.SQLite.SQLiteParameter("@typeId", typeId)
                );
            }
        }
    }
}
