using System;

namespace LabSystem.Core.Interfaces
{
    public interface IDbContextTransaction : IDisposable
    {
        void Commit();
        void Rollback();
    }
}
