using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SQLite;
using LabSystem.Core.Models;
using LabSystem.Data;
using LabSystem.Data.Repositories;
using LabSystem.Services;
using NUnit.Framework;

namespace LabSystem.Tests
{
    [TestFixture]
    public class AmendmentTests
    {
        private SQLiteConnection _connection;
        private LabDbContext _context;
        private ResultService _service;

        [SetUp]
        public void SetUp()
        {
            _connection = new SQLiteConnection("Data Source=:memory:");
            _connection.Open();

            _context = new LabDbContext(_connection);

            var initSqlPath = TestHelper.FindFileUpwards("LabSystem.Data", "Migrations", "V1__init.sql");
            if (initSqlPath == null || !File.Exists(initSqlPath))
            {
                throw new FileNotFoundException("Could not find V1__init.sql for SQLite setup.");
            }
            string sql = File.ReadAllText(initSqlPath);
            _context.Database.ExecuteSqlCommand(sql);

            // Add amendment columns if not present
            try { _context.Database.ExecuteSqlCommand("ALTER TABLE Results ADD COLUMN IsAmended INTEGER NOT NULL DEFAULT 0;"); } catch { }
            try { _context.Database.ExecuteSqlCommand("ALTER TABLE Results ADD COLUMN AmendmentReason TEXT;"); } catch { }
            try { _context.Database.ExecuteSqlCommand("ALTER TABLE Results ADD COLUMN AmendedAt DATETIME;"); } catch { }

            // Add PinHash column if not present (added in Phase 1)
            try { _context.Database.ExecuteSqlCommand("ALTER TABLE Staff ADD COLUMN PinHash TEXT;"); } catch { }
            try { _context.Database.ExecuteSqlCommand("ALTER TABLE Staff ADD COLUMN FailedLoginAttempts INTEGER NOT NULL DEFAULT 0;"); } catch { }
            try { _context.Database.ExecuteSqlCommand("ALTER TABLE Staff ADD COLUMN LockoutEnd DATETIME;"); } catch { }

            var resultRepo = new ResultRepository(_context);
            var testTypeRepo = new TestTypeRepository(_context);
            var orderRepo = new TestOrderRepository(_context);
            _service = new ResultService(resultRepo, testTypeRepo, orderRepo);
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();
            _connection?.Dispose();
        }

        [Test]
        public async Task Amend_EmptyReason_ThrowsArgumentException()
        {
            // Arrange
            var patient = new Patient { FullName = "Test Patient", CreatedAt = DateTime.UtcNow };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var order = new TestOrder { PatientId = patient.PatientId, OrderedAt = DateTime.UtcNow, Status = "Complete", Notes = "", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _context.TestOrders.Add(order);
            await _context.SaveChangesAsync();

            var staff = new Staff { FullName = "Tech", CreatedAt = DateTime.UtcNow };
            _context.Staff.Add(staff);
            await _context.SaveChangesAsync();

            var testType = new TestType { Name = "Hemoglobin", Unit = "g/dL", IsActive = true };
            _context.TestTypes.Add(testType);
            await _context.SaveChangesAsync();

            var result = new Result
            {
                OrderId = order.OrderId,
                TypeId = testType.TypeId,
                Value = 14.5,
                ValueText = "14.5",
                TechnicianId = staff.StaffId,
                IsAbnormal = false,
                RecordedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.AmendResultAsync(result.ResultId, 15.0, "15.0", "", staff.StaffId));
            Assert.That(ex.Message, Does.Contain("reason"));
        }

        [Test]
        public async Task Amend_ValidReason_SetsFlagsAndStoresReason()
        {
            // Arrange
            var patient = new Patient { FullName = "Test Patient", CreatedAt = DateTime.UtcNow };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var order = new TestOrder { PatientId = patient.PatientId, OrderedAt = DateTime.UtcNow, Status = "Complete", Notes = "", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _context.TestOrders.Add(order);
            await _context.SaveChangesAsync();

            var staff = new Staff { FullName = "Tech", CreatedAt = DateTime.UtcNow };
            _context.Staff.Add(staff);
            await _context.SaveChangesAsync();

            var testType = new TestType { Name = "Hemoglobin", Unit = "g/dL", IsActive = true, ReferenceRangeLow = 13.0, ReferenceRangeHigh = 17.0 };
            _context.TestTypes.Add(testType);
            await _context.SaveChangesAsync();

            var result = new Result
            {
                OrderId = order.OrderId,
                TypeId = testType.TypeId,
                Value = 14.5,
                ValueText = "14.5",
                TechnicianId = staff.StaffId,
                IsAbnormal = false,
                RecordedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            // Act
            await _service.AmendResultAsync(result.ResultId, 18.0, "18.0", "Correction needed", staff.StaffId);

            // Assert
            var amended = _context.Results.Find(result.ResultId);
            Assert.IsTrue(amended.IsAmended);
            Assert.AreEqual("Correction needed", amended.AmendmentReason);
            Assert.IsNotNull(amended.AmendedAt);
            Assert.AreEqual(18.0, amended.Value);
        }

        [Test]
        public async Task Amend_ReevaluatesAbnormality()
        {
            // Arrange
            var patient = new Patient { FullName = "Test Patient", DateOfBirth = DateTime.UtcNow.AddYears(-25), CreatedAt = DateTime.UtcNow };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var order = new TestOrder { PatientId = patient.PatientId, OrderedAt = DateTime.UtcNow, Status = "Complete", Notes = "", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _context.TestOrders.Add(order);
            await _context.SaveChangesAsync();

            var staff = new Staff { FullName = "Tech", CreatedAt = DateTime.UtcNow };
            _context.Staff.Add(staff);
            await _context.SaveChangesAsync();

            var testType = new TestType { Name = "Hemoglobin", Unit = "g/dL", IsActive = true, ReferenceRangeLow = 13.0, ReferenceRangeHigh = 17.0 };
            _context.TestTypes.Add(testType);
            await _context.SaveChangesAsync();

            // Value within normal range
            var result = new Result
            {
                OrderId = order.OrderId,
                TypeId = testType.TypeId,
                Value = 15.0,
                ValueText = "15.0",
                TechnicianId = staff.StaffId,
                IsAbnormal = false,
                RecordedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            // Act: Amend to abnormal value (above high)
            await _service.AmendResultAsync(result.ResultId, 20.0, "20.0", "Typo correction", staff.StaffId);

            // Assert
            var amended = _context.Results.Find(result.ResultId);
            Assert.IsTrue(amended.IsAbnormal);
        }
    }
}
