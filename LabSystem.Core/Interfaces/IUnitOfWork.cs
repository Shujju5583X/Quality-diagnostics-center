using System;

namespace LabSystem.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IDbContextTransaction BeginTransaction();
    }
}
