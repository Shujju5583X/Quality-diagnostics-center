using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using BCrypt.Net;

namespace LabSystem.Services
{
    public class LockoutException : Exception
    {
        public DateTime LockoutEnd { get; }
        public LockoutException(DateTime lockoutEnd) 
            : base($"Too many failed attempts. Try again after {lockoutEnd.ToLocalTime():yyyy-MM-dd HH:mm:ss}.")
        {
            LockoutEnd = lockoutEnd;
        }
    }

    public class AuthService : IAuthService
    {
        private readonly IRepository<Staff> _staffRepo;
        private static readonly Dictionary<int, (int FailedCount, DateTime LockoutEnd)> _lockouts = 
            new Dictionary<int, (int, DateTime)>();
        private static readonly object _lock = new object();
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(1);

        public AuthService(IRepository<Staff> staffRepo)
        {
            _staffRepo = staffRepo;
        }

        public async Task<bool> VerifyPinAsync(int staffId, string pin)
        {
            lock (_lock)
            {
                if (_lockouts.TryGetValue(staffId, out var lockout) && lockout.LockoutEnd > DateTime.UtcNow)
                {
                    throw new LockoutException(lockout.LockoutEnd);
                }
            }

            var staff = await _staffRepo.GetByIdAsync(staffId);
            if (staff == null) return false;
            
            bool isValid = BCrypt.Net.BCrypt.Verify(pin, staff.PinHash);

            lock (_lock)
            {
                if (isValid)
                {
                    _lockouts.Remove(staffId);
                }
                else
                {
                    _lockouts.TryGetValue(staffId, out var lockout);
                    int newCount = lockout.FailedCount + 1;
                    DateTime lockoutEnd = DateTime.MinValue;
                    if (newCount >= MaxFailedAttempts)
                    {
                        lockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                    }
                    _lockouts[staffId] = (newCount, lockoutEnd);
                }
            }

            return isValid;
        }

        public string HashPin(string pin)
        {
            return BCrypt.Net.BCrypt.HashPassword(pin);
        }
    }
}
