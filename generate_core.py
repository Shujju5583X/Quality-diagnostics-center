import os

core_dir = r"E:\Quality diagnostics center\LabSystem.Core"
models_dir = os.path.join(core_dir, "Models")
enums_dir = os.path.join(core_dir, "Enums")
interfaces_dir = os.path.join(core_dir, "Interfaces")

os.makedirs(models_dir, exist_ok=True)
os.makedirs(enums_dir, exist_ok=True)
os.makedirs(interfaces_dir, exist_ok=True)

models = {
    "Patient.cs": """using System;

namespace LabSystem.Core.Models
{
    public class Patient
    {
        public int PatientId { get; set; }
        public string FullName { get; set; }
        public string DateOfBirth { get; set; }
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
        public string CreatedAt { get; set; }
    }
}
""",
    "TestType.cs": """namespace LabSystem.Core.Models
{
    public class TestType
    {
        public int TypeId { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public double? ReferenceRangeLow { get; set; }
        public double? ReferenceRangeHigh { get; set; }
        public bool IsActive { get; set; }
    }
}
""",
    "Staff.cs": """namespace LabSystem.Core.Models
{
    public class Staff
    {
        public int StaffId { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string PinHash { get; set; }
    }
}
""",
    "TestOrder.cs": """using System;

namespace LabSystem.Core.Models
{
    public class TestOrder
    {
        public int OrderId { get; set; }
        public int PatientId { get; set; }
        public virtual Patient Patient { get; set; }
        public string OrderedAt { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
    }
}
""",
    "Result.cs": """using System;

namespace LabSystem.Core.Models
{
    public class Result
    {
        public int ResultId { get; set; }
        public int OrderId { get; set; }
        public virtual TestOrder Order { get; set; }
        public int TypeId { get; set; }
        public virtual TestType TestType { get; set; }
        public double Value { get; set; }
        public string RecordedAt { get; set; }
        public int TechnicianId { get; set; }
        public virtual Staff Technician { get; set; }
        public bool IsAbnormal { get; set; }
    }
}
""",
    "Report.cs": """using System;

namespace LabSystem.Core.Models
{
    public class Report
    {
        public int ReportId { get; set; }
        public int OrderId { get; set; }
        public virtual TestOrder Order { get; set; }
        public string FilePath { get; set; }
        public string GeneratedAt { get; set; }
    }
}
""",
    "AuditLog.cs": """using System;

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
        public string Timestamp { get; set; }
        public string Details { get; set; }
    }
}
"""
}

for name, content in models.items():
    with open(os.path.join(models_dir, name), "w", encoding="utf-8") as f:
        f.write(content)

enums = {
    "OrderStatus.cs": """namespace LabSystem.Core.Enums
{
    public static class OrderStatus
    {
        public const string Pending = "Pending";
        public const string InProgress = "InProgress";
        public const string Complete = "Complete";
        public const string Cancelled = "Cancelled";
    }
}
""",
    "RoleType.cs": """namespace LabSystem.Core.Enums
{
    public static class RoleType
    {
        public const string Technician = "Technician";
        public const string Admin = "Admin";
    }
}
"""
}

for name, content in enums.items():
    with open(os.path.join(enums_dir, name), "w", encoding="utf-8") as f:
        f.write(content)

interfaces = {
    "IRepository.cs": """using System.Collections.Generic;

namespace LabSystem.Core.Interfaces
{
    public interface IRepository<T>
    {
        T GetById(int id);
        IEnumerable<T> GetAll();
        void Add(T entity);
        void Update(T entity);
        void Delete(int id);
    }
}
""",
    "IPatientRepository.cs": """using System.Collections.Generic;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface IPatientRepository : IRepository<Patient>
    {
        IEnumerable<Patient> SearchByName(string query);
    }
}
""",
    "ITestOrderRepository.cs": """using System.Collections.Generic;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface ITestOrderRepository : IRepository<TestOrder>
    {
        IEnumerable<TestOrder> GetOrdersForPatient(int patientId);
        IEnumerable<TestOrder> GetByStatus(string status);
    }
}
""",
    "IResultRepository.cs": """using System.Collections.Generic;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface IResultRepository : IRepository<Result>
    {
        IEnumerable<Result> GetResultsForOrder(int orderId);
    }
}
""",
    "IServices.cs": """using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface IPdfReportService
    {
        string GenerateReport(TestOrder order);
    }

    public interface IBackupService
    {
        void BackupNow();
    }
}
"""
}

for name, content in interfaces.items():
    with open(os.path.join(interfaces_dir, name), "w", encoding="utf-8") as f:
        f.write(content)

print("Core files generated.")
