using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.Services;
using Moq;
using NUnit.Framework;

namespace LabSystem.Tests
{
    [TestFixture]
    public class SqliteBackupServiceTests
    {
        private Mock<IPatientRepository> _mockPatientRepo;
        private Mock<ITestOrderRepository> _mockOrderRepo;
        private Mock<IResultRepository> _mockResultRepo;
        private Mock<IRepository<TestType>> _mockTestTypeRepo;
        private Mock<IRepository<Staff>> _mockStaffRepo;
        private Mock<IRepository<AuditLog>> _mockAuditLogRepo;

        private SqliteBackupService _backupService;
        private string _backupsDir;
        private string _tempDbFile;

        [SetUp]
        public void SetUp()
        {
            _mockPatientRepo = new Mock<IPatientRepository>();
            _mockOrderRepo = new Mock<ITestOrderRepository>();
            _mockResultRepo = new Mock<IResultRepository>();
            _mockTestTypeRepo = new Mock<IRepository<TestType>>();
            _mockStaffRepo = new Mock<IRepository<Staff>>();
            _mockAuditLogRepo = new Mock<IRepository<AuditLog>>();

            _backupService = new SqliteBackupService(
                _mockPatientRepo.Object,
                _mockOrderRepo.Object,
                _mockResultRepo.Object,
                _mockTestTypeRepo.Object,
                _mockStaffRepo.Object,
                _mockAuditLogRepo.Object
            );

            _backupsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            _tempDbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lab.db");

            // Create a dummy lab.db so that the service attempts to copy it
            if (!File.Exists(_tempDbFile))
            {
                File.WriteAllText(_tempDbFile, "SQLite dummy database content");
            }

            // Cleanup any old backups
            if (Directory.Exists(_backupsDir))
            {
                Directory.Delete(_backupsDir, true);
            }

            // Arrange mocks to return default empty lists to avoid exceptions
            _mockPatientRepo.Setup(r => r.GetAllAsync(default)).ReturnsAsync(new List<Patient>
            {
                new Patient { PatientId = 1, FullName = "John Doe", DateOfBirth = "1990-01-01", Gender = "Male", ContactPhone = "123456", ContactEmail = "john@example.com", CreatedAt = "2026-06-09" }
            });
            _mockOrderRepo.Setup(r => r.GetAllAsync(default)).ReturnsAsync(new List<TestOrder>
            {
                new TestOrder { OrderId = 10, PatientId = 1, Status = "Pending", OrderedAt = "2026-06-09", Notes = "101,102", ReferredBy = "Dr. Clark" }
            });
            _mockResultRepo.Setup(r => r.GetAllAsync(default)).ReturnsAsync(new List<Result>
            {
                new Result { ResultId = 50, OrderId = 10, TypeId = 101, Value = 5.5, IsAbnormal = false, RecordedAt = "2026-06-09", TechnicianId = 1 }
            });
            _mockTestTypeRepo.Setup(r => r.GetAllAsync(default)).ReturnsAsync(new List<TestType>
            {
                new TestType { TypeId = 101, Name = "Blood Sugar", Unit = "mg/dL", ReferenceRangeLow = 70, ReferenceRangeHigh = 100, IsActive = true },
                new TestType { TypeId = 102, Name = "Hemoglobin", Unit = "g/dL", ReferenceRangeLow = 12, ReferenceRangeHigh = 16, IsActive = true }
            });
            _mockStaffRepo.Setup(r => r.GetAllAsync(default)).ReturnsAsync(new List<Staff>
            {
                new Staff { StaffId = 1, FullName = "Dr. Alice Smith", Role = "Technician", PinHash = "dummyhash", FailedLoginAttempts = 1, LockoutEnd = "2026-06-09T16:00:00Z" }
            });
            _mockAuditLogRepo.Setup(r => r.GetAllAsync(default)).ReturnsAsync(new List<AuditLog>
            {
                new AuditLog { LogId = 500, Action = "Backup", EntityType = "System", EntityId = null, Timestamp = new DateTime(2026, 6, 9), UserId = 1, Details = "Backup created" }
            });
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_tempDbFile))
            {
                File.Delete(_tempDbFile);
            }
            if (Directory.Exists(_backupsDir))
            {
                Directory.Delete(_backupsDir, true);
            }
        }

        [Test]
        public async Task BackupNow_ShouldCreateDbAndExcelBackup_WithProperSheetData()
        {
            // Act
            await _backupService.BackupNowAsync();

            // Assert
            string dbBackupsDir = Path.Combine(_backupsDir, "Database");
            string excelBackupsDir = Path.Combine(_backupsDir, "Excel");

            Assert.IsTrue(Directory.Exists(dbBackupsDir), "Database backups directory should be created.");
            Assert.IsTrue(Directory.Exists(excelBackupsDir), "Excel backups directory should be created.");

            var dbFiles = Directory.GetFiles(dbBackupsDir).Select(Path.GetFileName).ToList();
            var excelFiles = Directory.GetFiles(excelBackupsDir).Select(Path.GetFileName).ToList();

            Assert.IsTrue(dbFiles.Any(f => f.StartsWith("lab_backup_") && f.EndsWith(".db")), "Should generate timestamped .db backup in Database directory.");
            Assert.IsTrue(excelFiles.Any(f => f.StartsWith("lab_backup_") && f.EndsWith(".xlsx")), "Should generate timestamped .xlsx backup in Excel directory.");

            string excelPath = Directory.GetFiles(excelBackupsDir).First(f => f.EndsWith(".xlsx"));

            // Load generated Excel file and verify content
            using (var workbook = new XLWorkbook(excelPath))
            {
                Assert.AreEqual(6, workbook.Worksheets.Count, "Should contain exactly 6 worksheets.");

                // 1. Patients Worksheet
                var wsPatients = workbook.Worksheet("Patients");
                Assert.AreEqual("Quality Diagnostics Center - Patient Directory", wsPatients.Cell(1, 1).Value.ToString());
                Assert.AreEqual("Patient ID", wsPatients.Cell(3, 1).Value.ToString());
                Assert.AreEqual(1, (double)wsPatients.Cell(4, 1).Value);
                Assert.AreEqual("John Doe", wsPatients.Cell(4, 2).Value.ToString());
                Assert.AreEqual("Male", wsPatients.Cell(4, 4).Value.ToString());

                // 2. Test Orders Worksheet
                var wsOrders = workbook.Worksheet("Test Orders");
                Assert.AreEqual("Quality Diagnostics Center - Test Orders Record", wsOrders.Cell(1, 1).Value.ToString());
                Assert.AreEqual(10, (double)wsOrders.Cell(4, 1).Value);
                Assert.AreEqual("Dr. Clark", wsOrders.Cell(4, 5).Value.ToString());
                Assert.AreEqual("Blood Sugar, Hemoglobin", wsOrders.Cell(4, 7).Value.ToString(), "Test names should be resolved from notes");

                // 3. Test Results Worksheet
                var wsResults = workbook.Worksheet("Test Results");
                Assert.AreEqual("Quality Diagnostics Center - Patient Test Results", wsResults.Cell(1, 1).Value.ToString());
                Assert.AreEqual(50, (double)wsResults.Cell(4, 1).Value);
                Assert.AreEqual("Blood Sugar", wsResults.Cell(4, 4).Value.ToString());
                Assert.AreEqual(5.5, (double)wsResults.Cell(4, 5).Value);
                Assert.AreEqual("Normal", wsResults.Cell(4, 8).Value.ToString());

                // 4. Reference Catalog Worksheet
                var wsCatalog = workbook.Worksheet("Reference Catalog");
                Assert.AreEqual(101, (double)wsCatalog.Cell(4, 1).Value);
                Assert.AreEqual("Blood Sugar", wsCatalog.Cell(4, 2).Value.ToString());

                // 5. Staff Directory Worksheet
                var wsStaff = workbook.Worksheet("Staff Directory");
                Assert.AreEqual(1, (double)wsStaff.Cell(4, 1).Value);
                Assert.AreEqual("Dr. Alice Smith", wsStaff.Cell(4, 2).Value.ToString());
                Assert.AreEqual(1, (double)wsStaff.Cell(4, 4).Value);
                Assert.AreEqual("2026-06-09T16:00:00Z", wsStaff.Cell(4, 5).Value.ToString());

                // 6. Audit Logs Worksheet
                var wsLogs = workbook.Worksheet("Audit Logs");
                Assert.AreEqual(500, (double)wsLogs.Cell(4, 1).Value);
                Assert.AreEqual("Dr. Alice Smith", wsLogs.Cell(4, 6).Value.ToString(), "User ID should be mapped to Staff name");
            }
        }
    }
}
