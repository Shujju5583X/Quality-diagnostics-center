using System;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using BCrypt.Net;

namespace LabSystem.Services
{
    public class AuthService : IAuthService
    {
        private readonly IRepository<Staff> _staffRepo;

        public AuthService(IRepository<Staff> staffRepo)
        {
            _staffRepo = staffRepo;
        }

        public async Task<bool> VerifyPinAsync(int staffId, string pin, CancellationToken cancellationToken = default)
        {
            var staff = await _staffRepo.GetByIdAsync(staffId, cancellationToken);
            if (staff == null) return false;

            return BCrypt.Net.BCrypt.Verify(pin, staff.PinHash);
        }

        public string HashPin(string pin)
        {
            return BCrypt.Net.BCrypt.HashPassword(pin);
        }
    }
}
