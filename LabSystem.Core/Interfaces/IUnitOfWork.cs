using System;
using System.Threading.Tasks;

namespace LabSystem.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IDbContextTransaction BeginTransaction();
        Task RunInTransactionAsync(Func<Task> action);
    }
}
