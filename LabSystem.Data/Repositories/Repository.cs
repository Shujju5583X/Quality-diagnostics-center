using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;

namespace LabSystem.Data.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly LabDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(LabDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _dbSet.FindAsync(cancellationToken, id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
        }

        public async Task AddAsync(T entity, CancellationToken cancellationToken = default(CancellationToken))
        {
            _dbSet.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            _context.Entry(entity).State = EntityState.Detached;
        }

        public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default(CancellationToken))
        {
            var entry = _context.Entry(entity);
            if (entry.State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }
            entry.State = EntityState.Modified;
            await _context.SaveChangesAsync(cancellationToken);
            entry.State = EntityState.Detached;
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default(CancellationToken))
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
