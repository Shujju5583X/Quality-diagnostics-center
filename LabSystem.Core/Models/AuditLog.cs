using System;

namespace LabSystem.Core.Models
{
    public class AuditLog
    {
        public int LogId { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public int? EntityId { get; set; }
        public int? UserId { get; set; }
        public virtual Staff User { get; set; }
        public DateTime Timestamp { get; set; }
        public string Details { get; set; }
    }
}
