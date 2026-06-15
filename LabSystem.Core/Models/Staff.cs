using System;

namespace LabSystem.Core.Models
{
    public class Staff
    {
        public int StaffId { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; } = "Technician";
        public DateTime? CreatedAt { get; set; }
        public string PinHash { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public int BranchId { get; set; } = 1;
    }
}
