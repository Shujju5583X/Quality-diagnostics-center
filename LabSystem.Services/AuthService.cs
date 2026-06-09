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
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(1);

        public AuthService(IRepository<Staff> staffRepo)
        {
            _staffRepo = staffRepo;
        }

        public async Task<bool> VerifyPinAsync(int staffId, string pin)
        {
            var staff = await _staffRepo.GetByIdAsync(staffId);
            if (staff == null) return false;

            if (!string.IsNullOrEmpty(staff.LockoutEnd) && DateTime.TryParse(staff.LockoutEnd, null, System.Globalization.DateTimeStyles.RoundtripKind, out var lockoutEnd))
            {
                if (lockoutEnd > DateTime.UtcNow)
                {
                    throw new LockoutException(lockoutEnd);
                }
            }

            bool isValid = BCrypt.Net.BCrypt.Verify(pin, staff.PinHash);

            if (isValid)
            {
                staff.FailedLoginAttempts = 0;
                staff.LockoutEnd = null;
            }
            else
            {
                staff.FailedLoginAttempts++;
                if (staff.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    staff.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration).ToString("O");
                }
            }

            await _staffRepo.UpdateAsync(staff);
            return isValid;
        }

        public string HashPin(string pin)
        {
            return BCrypt.Net.BCrypt.HashPassword(pin);
        }
    }
}
