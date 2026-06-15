using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Collections.Generic;
using LabSystem.Core.Models;
using LabSystem.Data;
using LabSystem.Data.Repositories;
using LabSystem.Services;
using NUnit.Framework;

namespace LabSystem.Tests
{
    [TestFixture]
    public class QcServiceTests
    {
        private SQLiteConnection _connection;
        private LabDbContext _context;
        private QcService _service;
        private QcRepository _qcRepo;

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

            try { _context.Database.ExecuteSqlCommand("ALTER TABLE Patients ADD COLUMN BranchId INTEGER DEFAULT 1;"); } catch { }
            try { _context.Database.ExecuteSqlCommand("ALTER TABLE TestOrders ADD COLUMN BranchId INTEGER DEFAULT 1;"); } catch { }
            try { _context.Database.ExecuteSqlCommand("ALTER TABLE Staff ADD COLUMN BranchId INTEGER DEFAULT 1;"); } catch { }

            // Create QC tables
            _context.Database.ExecuteSqlCommand(@"
                CREATE TABLE IF NOT EXISTS QcRuns (
                    QcRunId INTEGER PRIMARY KEY AUTOINCREMENT,
                    TestTypeId INTEGER NOT NULL,
                    ControlName TEXT NOT NULL,
                    RunDate DATETIME NOT NULL,
                    MeasuredValue REAL NOT NULL,
                    LotNumber TEXT,
                    TargetValue REAL,
                    SD REAL,
                    CreatedAt DATETIME,
                    FOREIGN KEY(TestTypeId) REFERENCES TestTypes(TypeId)
                );
                CREATE TABLE IF NOT EXISTS QcLots (
                    QcLotId INTEGER PRIMARY KEY AUTOINCREMENT,
                    TestTypeId INTEGER NOT NULL,
                    LotNumber TEXT NOT NULL,
                    TargetValue REAL NOT NULL,
                    SD REAL NOT NULL,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedAt DATETIME,
                    FOREIGN KEY(TestTypeId) REFERENCES TestTypes(TypeId)
                );
            ");

            _qcRepo = new QcRepository(_context);
            _service = new QcService(_qcRepo);
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();
            _connection?.Dispose();
        }

        [Test]
        public async Task Westgard_1_3s_Detected()
        {
            // Arrange
            var testType = new TestType { Name = "Glucose", Unit = "mg/dL", IsActive = true };
            _context.TestTypes.Add(testType);
            await _context.SaveChangesAsync();

            var lot = new QcLot { TestTypeId = testType.TypeId, LotNumber = "LOT-001", TargetValue = 100, SD = 5, IsActive = true, CreatedAt = DateTime.UtcNow };
            _context.QcLots.Add(lot);
            await _context.SaveChangesAsync();

            // Create a run that exceeds ±3 SD (target=100, SD=5, measured=116 = +3.2 SD)
            var run = new QcRun
            {
                TestTypeId = testType.TypeId,
                ControlName = "Normal Control",
                RunDate = DateTime.UtcNow,
                MeasuredValue = 116,
                LotNumber = "LOT-001",
                TargetValue = 100,
                SD = 5
            };

            // Act
            var result = await _service.RecordQcRunAsync(run);
            var violations = await _service.EvaluateWestgardRulesAsync(run);

            // Assert
            Assert.IsTrue(violations.Any(v => v.Contains("1-3s")));
            Assert.AreEqual("Reject", result.Status);
        }

        [Test]
        public async Task Westgard_2_2s_Detected()
        {
            // Arrange
            var testType = new TestType { Name = "Glucose", Unit = "mg/dL", IsActive = true };
            _context.TestTypes.Add(testType);
            await _context.SaveChangesAsync();

            var lot = new QcLot { TestTypeId = testType.TypeId, LotNumber = "LOT-001", TargetValue = 100, SD = 5, IsActive = true, CreatedAt = DateTime.UtcNow };
            _context.QcLots.Add(lot);
            await _context.SaveChangesAsync();

            // Create two consecutive runs exceeding +2 SD on same side
            var run1 = new QcRun { TestTypeId = testType.TypeId, ControlName = "Normal Control", RunDate = DateTime.UtcNow.AddMinutes(-5), MeasuredValue = 111, LotNumber = "LOT-001", TargetValue = 100, SD = 5 };
            var run2 = new QcRun { TestTypeId = testType.TypeId, ControlName = "Normal Control", RunDate = DateTime.UtcNow, MeasuredValue = 112, LotNumber = "LOT-001", TargetValue = 100, SD = 5 };

            _context.QcRuns.Add(run1);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.RecordQcRunAsync(run2);
            var violations = await _service.EvaluateWestgardRulesAsync(run2);

            // Assert
            Assert.IsTrue(violations.Any(v => v.Contains("2-2s")));
            Assert.AreEqual("Reject", result.Status);
        }

        [Test]
        public async Task Westgard_10x_Detected()
        {
            // Arrange
            var testType = new TestType { Name = "Glucose", Unit = "mg/dL", IsActive = true };
            _context.TestTypes.Add(testType);
            await _context.SaveChangesAsync();

            var lot = new QcLot { TestTypeId = testType.TypeId, LotNumber = "LOT-001", TargetValue = 100, SD = 5, IsActive = true, CreatedAt = DateTime.UtcNow };
            _context.QcLots.Add(lot);
            await _context.SaveChangesAsync();

            // Create 10 consecutive runs above mean
            for (int i = 0; i < 10; i++)
            {
                var run = new QcRun { TestTypeId = testType.TypeId, ControlName = "Normal Control", RunDate = DateTime.UtcNow.AddMinutes(-10 * (10 - i)), MeasuredValue = 101, LotNumber = "LOT-001", TargetValue = 100, SD = 5 };
                _context.QcRuns.Add(run);
            }
            await _context.SaveChangesAsync();

            // 11th run also above mean
            var run11 = new QcRun { TestTypeId = testType.TypeId, ControlName = "Normal Control", RunDate = DateTime.UtcNow, MeasuredValue = 102, LotNumber = "LOT-001", TargetValue = 100, SD = 5 };

            // Act
            var result = await _service.RecordQcRunAsync(run11);
            var violations = await _service.EvaluateWestgardRulesAsync(run11);

            // Assert
            Assert.IsTrue(violations.Any(v => v.Contains("10x")));
            Assert.AreEqual("Reject", result.Status);
        }

        [Test]
        public async Task QcRun_Recording_WithLotNumber()
        {
            // Arrange
            var testType = new TestType { Name = "Glucose", Unit = "mg/dL", IsActive = true };
            _context.TestTypes.Add(testType);
            await _context.SaveChangesAsync();

            var run = new QcRun
            {
                TestTypeId = testType.TypeId,
                ControlName = "Normal Control",
                RunDate = DateTime.UtcNow,
                MeasuredValue = 100,
                LotNumber = "LOT-001",
                TargetValue = 100,
                SD = 5
            };

            // Act
            var result = await _service.RecordQcRunAsync(run);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Pass", result.Status);
            Assert.AreEqual(100, result.MeasuredValue);
            Assert.AreEqual("LOT-001", result.LotNumber);
        }

        [Test]
        public async Task GetQcRuns_DateRangeFilter()
        {
            // Arrange
            var testType = new TestType { Name = "Glucose", Unit = "mg/dL", IsActive = true };
            _context.TestTypes.Add(testType);
            await _context.SaveChangesAsync();

            var run1 = new QcRun { TestTypeId = testType.TypeId, ControlName = "Control", RunDate = DateTime.UtcNow.AddDays(-10), MeasuredValue = 100, LotNumber = "LOT-001", TargetValue = 100, SD = 5, CreatedAt = DateTime.UtcNow };
            var run2 = new QcRun { TestTypeId = testType.TypeId, ControlName = "Control", RunDate = DateTime.UtcNow, MeasuredValue = 101, LotNumber = "LOT-001", TargetValue = 100, SD = 5, CreatedAt = DateTime.UtcNow };
            _context.QcRuns.Add(run1);
            _context.QcRuns.Add(run2);
            await _context.SaveChangesAsync();

            // Act - query last 5 days only
            var runs = await _service.GetQcRunsAsync(testType.TypeId, DateTime.UtcNow.AddDays(-5), DateTime.UtcNow);
            var runsList = runs.ToList();

            // Assert - should only return run2
            Assert.AreEqual(1, runsList.Count);
            Assert.AreEqual(101, runsList[0].MeasuredValue);
        }
    }
}
