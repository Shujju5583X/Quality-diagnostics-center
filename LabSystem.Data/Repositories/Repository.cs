using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
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

        public T GetById(int id)
        {
            return _dbSet.Find(id);
        }

        public IEnumerable<T> GetAll()
        {
            return _dbSet.ToList();
        }

        public void Add(T entity)
        {
            _dbSet.Add(entity);
        }

        public void Update(T entity)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }

        public void Delete(int id)
        {
            var entity = GetById(id);
            if (entity != null)
                _dbSet.Remove(entity);
        }
    }
}
