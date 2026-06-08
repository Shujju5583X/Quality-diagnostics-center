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

        public bool VerifyPin(int staffId, string pin)
        {
            var staff = _staffRepo.GetById(staffId);
            if (staff == null) return false;
            
            return BCrypt.Net.BCrypt.Verify(pin, staff.PinHash);
        }

        public string HashPin(string pin)
        {
            return BCrypt.Net.BCrypt.HashPassword(pin);
        }
    }
}
