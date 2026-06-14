using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface IStaffService
    {
        Task<Staff> CreateStaffAsync(string fullName, string role, string pin, CancellationToken cancellationToken = default);
        Task UpdateStaffAsync(Staff staff, CancellationToken cancellationToken = default);
        Task ResetPinAsync(int staffId, string newPin, CancellationToken cancellationToken = default);
        Task ToggleLockoutAsync(int staffId, bool lockout, CancellationToken cancellationToken = default);
        Task<IEnumerable<Staff>> GetAllStaffAsync(CancellationToken cancellationToken = default);
    }
}