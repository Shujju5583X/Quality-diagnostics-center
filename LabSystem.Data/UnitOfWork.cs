using System;
using System.Data.Entity;
using System.Threading.Tasks;
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

        public async Task RunInTransactionAsync(Func<Task> action)
        {
            using (var tx = _context.Database.BeginTransaction())
            {
                try
                {
                    await action();
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public void Dispose()
        {
            if (_context != null) _context.Dispose();
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
