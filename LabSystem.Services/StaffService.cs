using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using BCrypt.Net;

namespace LabSystem.Services
{
    public class StaffService : IStaffService
    {
        private readonly IStaffRepository _staffRepo;

        public StaffService(IStaffRepository staffRepo)
        {
            _staffRepo = staffRepo;
        }

        public async Task<Staff> CreateStaffAsync(string fullName, string role, string pin, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Staff name is required.");
            if (string.IsNullOrWhiteSpace(pin) || pin.Length < 4)
                throw new ArgumentException("PIN must be at least 4 digits.");
            if (!string.IsNullOrWhiteSpace(role) && role != "Admin" && role != "Technician" && role != "Receptionist")
                throw new ArgumentException("Role must be Admin, Technician, or Receptionist.");

            var staff = new Staff
            {
                FullName = fullName.Trim(),
                Role = role ?? "Technician",
                PinHash = BCrypt.Net.BCrypt.HashPassword(pin),
                CreatedAt = DateTime.UtcNow
            };
            await _staffRepo.AddAsync(staff, cancellationToken);
            return staff;
        }

        public async Task UpdateStaffAsync(Staff staff, CancellationToken cancellationToken = default)
        {
            if (staff == null)
                throw new ArgumentNullException(nameof(staff));
            await _staffRepo.UpdateAsync(staff, cancellationToken);
        }

        public async Task ResetPinAsync(int staffId, string newPin, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(newPin) || newPin.Length < 4)
                throw new ArgumentException("PIN must be at least 4 digits.");

            var staff = await _staffRepo.GetByIdAsync(staffId, cancellationToken);
            if (staff == null)
                throw new InvalidOperationException("Staff not found.");

            staff.PinHash = BCrypt.Net.BCrypt.HashPassword(newPin);
            await _staffRepo.UpdateAsync(staff, cancellationToken);
        }

        public async Task ToggleLockoutAsync(int staffId, bool lockout, CancellationToken cancellationToken = default)
        {
            var staff = await _staffRepo.GetByIdAsync(staffId, cancellationToken);
            if (staff == null)
                throw new InvalidOperationException("Staff not found.");

            staff.LockoutEnd = lockout ? DateTime.UtcNow.AddMinutes(60) : (DateTime?)null;
            staff.FailedLoginAttempts = lockout ? 5 : 0;
            await _staffRepo.UpdateAsync(staff, cancellationToken);
        }

        public async Task<IEnumerable<Staff>> GetAllStaffAsync(CancellationToken cancellationToken = default)
        {
            return await _staffRepo.GetAllAsync(cancellationToken);
        }
    }
}