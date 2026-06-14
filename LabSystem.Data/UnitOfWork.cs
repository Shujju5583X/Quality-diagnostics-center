using System.Data.Entity;
using LabSystem.Core.Interfaces;

namespace LabSystem.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly LabDbContext _context;

        public UnitOfWork(LabDbContext context)
        {
            _context = context;
        }

        public IDbContextTransaction BeginTransaction()
        {
            var tx = _context.Database.BeginTransaction();
            return new EFDbContextTransaction(tx);
        }

        public void Dispose()
        {
            // Transient DbContext is disposed by container or caller,
            // but we can delegate if needed.
        }
    }

    public class EFDbContextTransaction : IDbContextTransaction
    {
        private readonly DbContextTransaction _transaction;

        public EFDbContextTransaction(DbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public void Commit()
        {
            _transaction.Commit();
        }

        public void Rollback()
        {
            _transaction.Rollback();
        }

        public void Dispose()
        {
            _transaction.Dispose();
        }
    }
}
