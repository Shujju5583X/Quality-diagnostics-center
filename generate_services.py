import os

core_dir = r"E:\Quality diagnostics center\LabSystem.Core"
services_dir = r"E:\Quality diagnostics center\LabSystem.Services"

# Add interfaces
with open(os.path.join(core_dir, "Interfaces", "IServices.cs"), "a", encoding="utf-8") as f:
    f.write("""
    public interface IAuthService
    {
        bool VerifyPin(int staffId, string pin);
        string HashPin(string pin);
    }
    
    public interface IOrderService
    {
        void CreateOrder(TestOrder order);
        void UpdateOrderStatus(int orderId, string status);
    }
    
    public interface IResultService
    {
        void AddResult(Result result);
    }
""")

services = {
    "AuthService.cs": """using LabSystem.Core.Interfaces;
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
""",
    "OrderService.cs": """using System;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Services
{
    public class OrderService : IOrderService
    {
        private readonly ITestOrderRepository _orderRepo;
        private readonly IRepository<AuditLog> _auditRepo;

        public OrderService(ITestOrderRepository orderRepo, IRepository<AuditLog> auditRepo)
        {
            _orderRepo = orderRepo;
            _auditRepo = auditRepo;
        }

        public void CreateOrder(TestOrder order)
        {
            order.OrderedAt = DateTime.UtcNow.ToString("O");
            _orderRepo.Add(order);
            
            _auditRepo.Add(new AuditLog
            {
                Action = "Created",
                EntityType = "TestOrder",
                Timestamp = DateTime.UtcNow.ToString("O"),
                Details = "New test order created."
            });
        }

        public void UpdateOrderStatus(int orderId, string status)
        {
            var order = _orderRepo.GetById(orderId);
            if (order != null)
            {
                order.Status = status;
                _orderRepo.Update(order);
                
                _auditRepo.Add(new AuditLog
                {
                    Action = "Updated",
                    EntityType = "TestOrder",
                    EntityId = orderId,
                    Timestamp = DateTime.UtcNow.ToString("O"),
                    Details = $"Order status updated to {status}."
                });
            }
        }
    }
}
""",
    "ResultService.cs": """using System;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Services
{
    public class ResultService : IResultService
    {
        private readonly IResultRepository _resultRepo;
        private readonly IRepository<TestType> _testTypeRepo;
        private readonly IRepository<AuditLog> _auditRepo;

        public ResultService(IResultRepository resultRepo, IRepository<TestType> testTypeRepo, IRepository<AuditLog> auditRepo)
        {
            _resultRepo = resultRepo;
            _testTypeRepo = testTypeRepo;
            _auditRepo = auditRepo;
        }

        public void AddResult(Result result)
        {
            var testType = _testTypeRepo.GetById(result.TypeId);
            if (testType != null)
            {
                if (testType.ReferenceRangeLow.HasValue && result.Value < testType.ReferenceRangeLow.Value)
                    result.IsAbnormal = true;
                else if (testType.ReferenceRangeHigh.HasValue && result.Value > testType.ReferenceRangeHigh.Value)
                    result.IsAbnormal = true;
                else
                    result.IsAbnormal = false;
            }

            result.RecordedAt = DateTime.UtcNow.ToString("O");
            _resultRepo.Add(result);

            _auditRepo.Add(new AuditLog
            {
                Action = "Created",
                EntityType = "Result",
                Timestamp = DateTime.UtcNow.ToString("O"),
                Details = $"Result added for OrderId {result.OrderId}."
            });
        }
    }
}
""",
    "PdfReportService.cs": """using System;
using System.IO;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

namespace LabSystem.Services
{
    public class PdfReportService : IPdfReportService
    {
        private readonly IResultRepository _resultRepo;

        public PdfReportService(IResultRepository resultRepo)
        {
            _resultRepo = resultRepo;
        }

        public string GenerateReport(TestOrder order)
        {
            var results = _resultRepo.GetResultsForOrder(order.OrderId);
            
            Document document = new Document();
            Section section = document.AddSection();
            section.AddParagraph("Medical Lab Management System").Format.Font.Size = 16;
            section.AddParagraph($"Patient: {order.Patient?.FullName}");
            section.AddParagraph($"Order ID: {order.OrderId} | Date: {order.OrderedAt}");
            section.AddParagraph();

            // Example implementation for PDF table using MigraDoc
            var table = section.AddTable();
            table.Borders.Width = 0.75;
            table.AddColumn("4cm"); // Test Name
            table.AddColumn("2cm"); // Result
            table.AddColumn("2cm"); // Unit
            table.AddColumn("3cm"); // Ref Range
            table.AddColumn("3cm"); // Flag

            var header = table.AddRow();
            header.Cells[0].AddParagraph("Test Name");
            header.Cells[1].AddParagraph("Result");
            header.Cells[2].AddParagraph("Unit");
            header.Cells[3].AddParagraph("Ref Range");
            header.Cells[4].AddParagraph("Flag");

            foreach (var r in results)
            {
                var row = table.AddRow();
                row.Cells[0].AddParagraph(r.TestType?.Name ?? "Unknown");
                row.Cells[1].AddParagraph(r.Value.ToString());
                row.Cells[2].AddParagraph(r.TestType?.Unit ?? "");
                row.Cells[3].AddParagraph($"{r.TestType?.ReferenceRangeLow} - {r.TestType?.ReferenceRangeHigh}");
                row.Cells[4].AddParagraph(r.IsAbnormal ? "Abnormal" : "Normal");
            }

            PdfDocumentRenderer renderer = new PdfDocumentRenderer(true)
            {
                Document = document
            };
            renderer.RenderDocument();

            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", order.OrderId.ToString());
            Directory.CreateDirectory(dir);
            string filepath = Path.Combine(dir, $"report_{order.OrderId}.pdf");

            renderer.PdfDocument.Save(filepath);
            return filepath;
        }
    }
}
""",
    "SqliteBackupService.cs": """using System;
using System.IO;
using LabSystem.Core.Interfaces;

namespace LabSystem.Services
{
    public class SqliteBackupService : IBackupService
    {
        public void BackupNow()
        {
            string sourceFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lab.db");
            if (File.Exists(sourceFile))
            {
                string backupsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                Directory.CreateDirectory(backupsDir);
                
                string filename = $"lab_backup_{DateTime.Now:yyyy-MM-dd_HHmm}.db";
                string destFile = Path.Combine(backupsDir, filename);
                
                File.Copy(sourceFile, destFile, true);
            }
        }
    }
}
"""
}

for name, content in services.items():
    with open(os.path.join(services_dir, name), "w", encoding="utf-8") as f:
        f.write(content)

print("Services files generated.")
