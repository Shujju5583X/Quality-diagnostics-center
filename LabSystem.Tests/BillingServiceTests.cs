using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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
    public class BillingServiceTests
    {
        private SQLiteConnection _connection;
        private LabDbContext _context;
        private BillingService _service;

        [SetUp]
        public void SetUp()
        {
            _connection = new SQLiteConnection("Data Source=:memory:");
            _connection.Open();

            _context = new LabDbContext(_connection);

            // Read V1__init.sql and run it to create tables
            var initSqlPath = TestHelper.FindFileUpwards("LabSystem.Data", "Migrations", "V1__init.sql");
            if (initSqlPath == null || !File.Exists(initSqlPath))
            {
                throw new FileNotFoundException("Could not find V1__init.sql for SQLite setup.");
            }
            string sql = File.ReadAllText(initSqlPath);
            _context.Database.ExecuteSqlCommand(sql);

            // Add Payments, TestPanels, and PanelTestTypes tables manually as they are defined in App.xaml.cs EnsureSchemaUpToDate
            _context.Database.ExecuteSqlCommand(@"
                CREATE TABLE IF NOT EXISTS TestPanels (
                    PanelId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    Price REAL NOT NULL
                );
                CREATE TABLE IF NOT EXISTS PanelTestTypes (
                    PanelId INTEGER NOT NULL,
                    TypeId INTEGER NOT NULL,
                    PRIMARY KEY(PanelId, TypeId),
                    FOREIGN KEY(PanelId) REFERENCES TestPanels(PanelId) ON DELETE CASCADE,
                    FOREIGN KEY(TypeId) REFERENCES TestTypes(TypeId) ON DELETE CASCADE
                );
                CREATE TABLE IF NOT EXISTS Payments (
                    PaymentId INTEGER PRIMARY KEY AUTOINCREMENT,
                    InvoiceId INTEGER NOT NULL,
                    Amount REAL NOT NULL,
                    PaymentMethod TEXT,
                    PaymentDate DATETIME NOT NULL,
                    FOREIGN KEY(InvoiceId) REFERENCES Invoices(InvoiceId) ON DELETE CASCADE
                );
            ");

            var invoiceRepo = new InvoiceRepository(_context);
            var paymentRepo = new PaymentRepository(_context);
            var orderRepo = new TestOrderRepository(_context);
            var panelRepo = new TestPanelRepository(_context);
            _service = new BillingService(invoiceRepo, paymentRepo, orderRepo, panelRepo);
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();
            _connection?.Dispose();
        }

        [Test]
        public async Task GenerateInvoice_ShouldUsePanelPrice_WhenAllPanelTestsOrdered()
        {
            // Arrange: Create a panel with 2 test types
            var patient = new Patient { FullName = "Test Patient", CreatedAt = DateTime.UtcNow };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var tt1 = new TestType { Name = "Test 1", Price = 500, IsActive = true };
            var tt2 = new TestType { Name = "Test 2", Price = 600, IsActive = true };
            _context.TestTypes.Add(tt1);
            _context.TestTypes.Add(tt2);
            await _context.SaveChangesAsync();

            var panel = new TestPanel { Name = "Lipid Panel", Price = 800 };
            panel.TestTypes.Add(tt1);
            panel.TestTypes.Add(tt2);
            _context.TestPanels.Add(panel);
            await _context.SaveChangesAsync();

            var order = new TestOrder { PatientId = patient.PatientId, OrderedAt = DateTime.UtcNow, Status = "Pending", Notes = "", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            order.TestTypes.Add(tt1);
            order.TestTypes.Add(tt2);
            _context.TestOrders.Add(order);
            await _context.SaveChangesAsync();

            // Act
            var invoice = await _service.GenerateInvoiceAsync(order.OrderId);

            // Assert
            Assert.AreEqual(800m, invoice.TotalAmount);
        }

        [Test]
        public async Task GenerateInvoice_ShouldNotDuplicateInvoice_WhenCalledTwice()
        {
            // Arrange
            var patient = new Patient { FullName = "Test Patient", CreatedAt = DateTime.UtcNow };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var tt = new TestType { Name = "Test 1", Price = 500, IsActive = true };
            _context.TestTypes.Add(tt);
            await _context.SaveChangesAsync();

            var order = new TestOrder { PatientId = patient.PatientId, OrderedAt = DateTime.UtcNow, Status = "Pending", Notes = "", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            order.TestTypes.Add(tt);
            _context.TestOrders.Add(order);
            await _context.SaveChangesAsync();

            // Act
            var inv1 = await _service.GenerateInvoiceAsync(order.OrderId);
            var inv2 = await _service.GenerateInvoiceAsync(order.OrderId);

            // Assert: verify same invoice is returned and only 1 invoice in DB
            Assert.AreEqual(inv1.InvoiceId, inv2.InvoiceId);
            var count = _context.Invoices.Count(i => i.OrderId == order.OrderId);
            Assert.AreEqual(1, count);
        }

        [Test]
        public async Task AddPayment_ShouldNotSetIsPaid_WhenPartialAmountPaid()
        {
            // Arrange
            var patient = new Patient { FullName = "Test Patient", CreatedAt = DateTime.UtcNow };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var order = new TestOrder { PatientId = patient.PatientId, OrderedAt = DateTime.UtcNow, Status = "Pending", Notes = "", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _context.TestOrders.Add(order);
            await _context.SaveChangesAsync();

            var invoice = new Invoice { OrderId = order.OrderId, TotalAmount = 1000m, IsPaid = false, CreatedAt = DateTime.UtcNow };
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // Act: Pay partial
            await _service.AddPaymentAsync(invoice.InvoiceId, 500m, "Cash");

            // Assert
            var updatedInvoice = _context.Invoices.Find(invoice.InvoiceId);
            Assert.IsFalse(updatedInvoice.IsPaid);
            Assert.IsNull(updatedInvoice.PaidAt);
        }

        [Test]
        public async Task AddPayment_ShouldSetIsPaid_WhenFullAmountPaid()
        {
            // Arrange
            var patient = new Patient { FullName = "Test Patient", CreatedAt = DateTime.UtcNow };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var order = new TestOrder { PatientId = patient.PatientId, OrderedAt = DateTime.UtcNow, Status = "Pending", Notes = "", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _context.TestOrders.Add(order);
            await _context.SaveChangesAsync();

            var invoice = new Invoice { OrderId = order.OrderId, TotalAmount = 1000m, IsPaid = false, CreatedAt = DateTime.UtcNow };
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // Act: Pay full
            await _service.AddPaymentAsync(invoice.InvoiceId, 1000m, "Cash");

            // Assert
            var updatedInvoice = _context.Invoices.Find(invoice.InvoiceId);
            Assert.IsTrue(updatedInvoice.IsPaid);
            Assert.IsNotNull(updatedInvoice.PaidAt);
        }
    }
}
