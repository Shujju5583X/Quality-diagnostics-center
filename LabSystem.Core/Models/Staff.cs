using System;

namespace LabSystem.Core.Models
{
    public class Staff
    {
        public Staff()
        {
            Role = "Technician";
        }

        public int StaffId { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string PinHash { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }
    }
}