using System;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using LabSystem.Core.Models;
using LabSystem.Data;
using LabSystem.Data.Repositories;
using NUnit.Framework;

namespace LabSystem.Tests
{
    [TestFixture]
    public class TestOrderRepositoryTests
    {
        private SQLiteConnection _connection;
        private LabDbContext _context;
        private TestOrderRepository _repository;

        [SetUp]
        public void SetUp()
        {
            _connection = new SQLiteConnection("Data Source=:memory:");
            _connection.Open();

            _context = new LabDbContext(_connection);
            TestHelper.InitializeTestDatabase(_context);

            _repository = new TestOrderRepository(_context);
        }

        [TearDown]
        public void TearDown()
        {
            if (_context != null)
            {
                _context.Dispose();
            }
            if (_connection != null)
            {
                _connection.Dispose();
            }
        }

        [Test]
        public async Task GetDailySequenceNumber_ShouldReturnCorrectSequence_WhenMultipleOrdersOnSameDay()
        {
            // Arrange
            var patient = new Patient { FullName = "Test Patient", CreatedAt = DateTime.UtcNow };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var date = new DateTime(2026, 6, 25, 12, 0, 0, DateTimeKind.Utc);
            var order1 = new TestOrder { PatientId = patient.PatientId, OrderedAt = date, Status = "Pending", CreatedAt = date, UpdatedAt = date };
            var order2 = new TestOrder { PatientId = patient.PatientId, OrderedAt = date.AddHours(1), Status = "Pending", CreatedAt = date, UpdatedAt = date };
            var order3 = new TestOrder { PatientId = patient.PatientId, OrderedAt = date.AddHours(2), Status = "Pending", CreatedAt = date, UpdatedAt = date };

            _context.TestOrders.Add(order1);
            _context.TestOrders.Add(order2);
            _context.TestOrders.Add(order3);
            await _context.SaveChangesAsync();

            // Act
            int seq1 = await _repository.GetDailySequenceNumberAsync(order1.OrderId, order1.OrderedAt);
            int seq2 = await _repository.GetDailySequenceNumberAsync(order2.OrderId, order2.OrderedAt);
            int seq3 = await _repository.GetDailySequenceNumberAsync(order3.OrderId, order3.OrderedAt);

            // Assert
            Assert.AreEqual(1, seq1);
            Assert.AreEqual(2, seq2);
            Assert.AreEqual(3, seq3);
        }

        [Test]
        public async Task GetDailySequenceNumber_ShouldReset_WhenNextDay()
        {
            // Arrange
            var patient = new Patient { FullName = "Test Patient", CreatedAt = DateTime.UtcNow };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var day1 = new DateTime(2026, 6, 25, 12, 0, 0, DateTimeKind.Utc);
            var day2 = new DateTime(2026, 6, 26, 9, 0, 0, DateTimeKind.Utc);

            var orderDay1 = new TestOrder { PatientId = patient.PatientId, OrderedAt = day1, Status = "Pending", CreatedAt = day1, UpdatedAt = day1 };
            var orderDay2 = new TestOrder { PatientId = patient.PatientId, OrderedAt = day2, Status = "Pending", CreatedAt = day2, UpdatedAt = day2 };

            _context.TestOrders.Add(orderDay1);
            _context.TestOrders.Add(orderDay2);
            await _context.SaveChangesAsync();

            // Act
            int seqDay1 = await _repository.GetDailySequenceNumberAsync(orderDay1.OrderId, orderDay1.OrderedAt);
            int seqDay2 = await _repository.GetDailySequenceNumberAsync(orderDay2.OrderId, orderDay2.OrderedAt);

            // Assert
            Assert.AreEqual(1, seqDay1);
            Assert.AreEqual(1, seqDay2);
        }

        [Test]
        public async Task GetDailySequenceNumber_ShouldReturnSequenceCorrectly_WhenOrderIdIsZero()
        {
            // Arrange
            var patient = new Patient { FullName = "Test Patient", CreatedAt = DateTime.UtcNow };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var date = new DateTime(2026, 6, 25, 12, 0, 0, DateTimeKind.Utc);
            var order1 = new TestOrder { PatientId = patient.PatientId, OrderedAt = date, Status = "Pending", CreatedAt = date, UpdatedAt = date };

            _context.TestOrders.Add(order1);
            await _context.SaveChangesAsync();

            // Act & Assert
            int seqForNewOrder = await _repository.GetDailySequenceNumberAsync(0, date);
            Assert.AreEqual(2, seqForNewOrder);
        }
    }
}
