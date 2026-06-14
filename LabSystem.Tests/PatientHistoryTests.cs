using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SQLite;
using LabSystem.Core.Models;
using LabSystem.Data;
using LabSystem.Data.Repositories;
using NUnit.Framework;

namespace LabSystem.Tests
{
    [TestFixture]
    public class PatientHistoryTests
    {
        private SQLiteConnection _connection;
        private LabDbContext _context;
        private PatientRepository _repository;

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

            _repository = new PatientRepository(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();
            _connection?.Dispose();
        }

        [Test]
        public async Task History_NoOrders_ReturnsEmpty()
        {
            // Arrange
            var patient = new Patient { FullName = "No Orders Patient", CreatedAt = DateTime.UtcNow };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            // Act
            var orders = await _repository.GetPatientOrdersAsync(patient.PatientId);

            // Assert
            Assert.IsEmpty(orders);
        }

        [Test]
        public async Task History_MultipleOrders_ReturnsChronological()
        {
            // Arrange
            var patient = new Patient { FullName = "Multi Order Patient", CreatedAt = DateTime.UtcNow };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var order1 = new TestOrder { PatientId = patient.PatientId, OrderedAt = DateTime.UtcNow.AddDays(-2), Status = "Complete", Notes = "", CreatedAt = DateTime.UtcNow.AddDays(-2), UpdatedAt = DateTime.UtcNow.AddDays(-2) };
            var order2 = new TestOrder { PatientId = patient.PatientId, OrderedAt = DateTime.UtcNow.AddDays(-1), Status = "Complete", Notes = "", CreatedAt = DateTime.UtcNow.AddDays(-1), UpdatedAt = DateTime.UtcNow.AddDays(-1) };
            var order3 = new TestOrder { PatientId = patient.PatientId, OrderedAt = DateTime.UtcNow, Status = "Complete", Notes = "", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _context.TestOrders.Add(order1);
            _context.TestOrders.Add(order2);
            _context.TestOrders.Add(order3);
            await _context.SaveChangesAsync();

            // Act
            var orders = await _repository.GetPatientOrdersAsync(patient.PatientId);
            var ordersList = orders.ToList();

            // Assert
            Assert.AreEqual(3, ordersList.Count);
            // Should be ordered by OrderedAt descending (most recent first)
            Assert.IsTrue(ordersList[0].OrderedAt >= ordersList[1].OrderedAt);
            Assert.IsTrue(ordersList[1].OrderedAt >= ordersList[2].OrderedAt);
        }

        [Test]
        public async Task History_IncludesTestTypeDetails()
        {
            // Arrange
            var patient = new Patient { FullName = "Test Detail Patient", CreatedAt = DateTime.UtcNow };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var testType = new TestType { Name = "Hemoglobin", Unit = "g/dL", IsActive = true };
            _context.TestTypes.Add(testType);
            await _context.SaveChangesAsync();

            var order = new TestOrder { PatientId = patient.PatientId, OrderedAt = DateTime.UtcNow, Status = "Complete", Notes = "", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            order.TestTypes.Add(testType);
            _context.TestOrders.Add(order);
            await _context.SaveChangesAsync();

            // Act
            var orders = await _repository.GetPatientOrdersAsync(patient.PatientId);
            var orderWithTests = orders.FirstOrDefault();

            // Assert
            Assert.IsNotNull(orderWithTests);
            Assert.IsNotNull(orderWithTests.TestTypes);
            Assert.AreEqual(1, orderWithTests.TestTypes.Count);
            Assert.AreEqual("Hemoglobin", orderWithTests.TestTypes.First().Name);
        }
    }
}
