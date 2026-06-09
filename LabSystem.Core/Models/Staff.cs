namespace LabSystem.Core.Models
{
    public class Staff
    {
        public int StaffId { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string PinHash { get; set; }
        public int FailedLoginAttempts { get; set; }
        public string LockoutEnd { get; set; }
    }
}
