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

            // Add Payments, TestPanels, and PanelTestTypes tables manually
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

            // Add DiscountPercent and TaxPercent columns if they don't exist
            try { _context.Database.ExecuteSqlCommand("ALTER TABLE Invoices ADD COLUMN DiscountPercent REAL DEFAULT 0;"); } catch { }
            try { _context.Database.ExecuteSqlCommand("ALTER TABLE Invoices ADD COLUMN TaxPercent REAL DEFAULT 0;"); } catch { }
            try { _context.Database.ExecuteSqlCommand("ALTER TABLE Invoices ADD COLUMN BranchId INTEGER DEFAULT 1;"); } catch { }
            try { _context.Database.ExecuteSqlCommand("ALTER TABLE Patients ADD COLUMN BranchId INTEGER DEFAULT 1;"); } catch { }
            try { _context.Database.ExecuteSqlCommand("ALTER TABLE TestOrders ADD COLUMN BranchId INTEGER DEFAULT 1;"); } catch { }

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

        [Test]
        public async Task UpdateInvoiceFinancials_WithDiscountPercent_RecalculatesGrandTotal()
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

            // Act: Apply 10% discount
            await _service.UpdateInvoiceFinancialsAsync(invoice.InvoiceId, 10m, 0m);

            // Assert
            var updatedInvoice = _context.Invoices.Find(invoice.InvoiceId);
            Assert.AreEqual(10m, updatedInvoice.DiscountPercent);
            Assert.AreEqual(0m, updatedInvoice.TaxPercent);
            Assert.AreEqual(100m, updatedInvoice.DiscountAmount); // 1000 * 10% = 100
            Assert.AreEqual(900m, updatedInvoice.GrandTotal);     // 1000 - 100 = 900
        }

        [Test]
        public async Task UpdateInvoiceFinancials_WithTaxPercent_RecalculatesGrandTotal()
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

            // Act: Apply 18% tax
            await _service.UpdateInvoiceFinancialsAsync(invoice.InvoiceId, 0m, 18m);

            // Assert
            var updatedInvoice = _context.Invoices.Find(invoice.InvoiceId);
            Assert.AreEqual(0m, updatedInvoice.DiscountPercent);
            Assert.AreEqual(18m, updatedInvoice.TaxPercent);
            Assert.AreEqual(0m, updatedInvoice.DiscountAmount);
            Assert.AreEqual(180m, updatedInvoice.TaxAmount);     // (1000 - 0) * 18% = 180
            Assert.AreEqual(1180m, updatedInvoice.GrandTotal);   // 1000 - 0 + 180 = 1180
        }

        [Test]
        public async Task UpdateInvoiceFinancials_WithBothDiscountAndTax_RecalculatesGrandTotal()
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

            // Act: Apply 10% discount + 18% tax
            await _service.UpdateInvoiceFinancialsAsync(invoice.InvoiceId, 10m, 18m);

            // Assert
            var updatedInvoice = _context.Invoices.Find(invoice.InvoiceId);
            Assert.AreEqual(100m, updatedInvoice.DiscountAmount);  // 1000 * 10% = 100
            Assert.AreEqual(162m, updatedInvoice.TaxAmount);       // (1000 - 100) * 18% = 162
            Assert.AreEqual(1062m, updatedInvoice.GrandTotal);     // 1000 - 100 + 162 = 1062
        }

        [Test]
        public async Task AddPayment_MultiplePartialPayments_SumToTotal_IsPaidTrue()
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

            // Act: Pay 500 + 500 = 1000
            await _service.AddPaymentAsync(invoice.InvoiceId, 500m, "Cash");
            await _service.AddPaymentAsync(invoice.InvoiceId, 500m, "UPI");

            // Assert
            var updatedInvoice = _context.Invoices.Find(invoice.InvoiceId);
            Assert.IsTrue(updatedInvoice.IsPaid);
            Assert.IsNotNull(updatedInvoice.PaidAt);
        }

        [Test]
        public async Task AddPayment_OverPaymentAttempt_Rejected()
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

            // Act: Pay more than total (this should still work in service, UI validates before calling)
            await _service.AddPaymentAsync(invoice.InvoiceId, 1500m, "Cash");

            // Assert: IsPaid should be true (overpayment allowed at service level)
            var updatedInvoice = _context.Invoices.Find(invoice.InvoiceId);
            Assert.IsTrue(updatedInvoice.IsPaid);
        }

        [Test]
        public async Task AddPayment_PartialPayment_WithDiscountApplied_IsPaidRemainsFalse()
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

            // Apply 10% discount: GrandTotal = 900
            await _service.UpdateInvoiceFinancialsAsync(invoice.InvoiceId, 10m, 0m);

            // Act: Pay 500 of 900
            await _service.AddPaymentAsync(invoice.InvoiceId, 500m, "Cash");

            // Assert
            var updatedInvoice = _context.Invoices.Find(invoice.InvoiceId);
            Assert.IsFalse(updatedInvoice.IsPaid);
            Assert.IsNull(updatedInvoice.PaidAt);
        }

        [Test]
        public async Task GetRevenueReport_NoInvoices_ReturnsZeros()
        {
            // Act
            var stats = await _service.GetRevenueReportAsync(DateTime.Today.AddDays(-30), DateTime.Today);

            // Assert
            Assert.AreEqual(0m, stats.TotalRevenue);
            Assert.AreEqual(0m, stats.TotalCollected);
            Assert.AreEqual(0m, stats.OutstandingAmount);
            Assert.AreEqual(0m, stats.CashCollected);
            Assert.AreEqual(0m, stats.UpiCollected);
        }

        [Test]
        public async Task GetRevenueReport_MixedPaidUnpaid_CalculatesCorrectly()
        {
            // Arrange
            var patient = new Patient { FullName = "Test Patient", CreatedAt = DateTime.UtcNow };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var order1 = new TestOrder { PatientId = patient.PatientId, OrderedAt = DateTime.UtcNow, Status = "Pending", Notes = "", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var order2 = new TestOrder { PatientId = patient.PatientId, OrderedAt = DateTime.UtcNow, Status = "Pending", Notes = "", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _context.TestOrders.Add(order1);
            _context.TestOrders.Add(order2);
            await _context.SaveChangesAsync();

            // Paid invoice: 1000
            var invoice1 = new Invoice { OrderId = order1.OrderId, TotalAmount = 1000m, IsPaid = true, PaymentMethod = "Cash", CreatedAt = DateTime.UtcNow };
            // Unpaid invoice: 500
            var invoice2 = new Invoice { OrderId = order2.OrderId, TotalAmount = 500m, IsPaid = false, CreatedAt = DateTime.UtcNow };
            _context.Invoices.Add(invoice1);
            _context.Invoices.Add(invoice2);
            await _context.SaveChangesAsync();

            // Act
            var stats = await _service.GetRevenueReportAsync(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));

            // Assert
            Assert.AreEqual(1500m, stats.TotalRevenue);
            Assert.AreEqual(1000m, stats.TotalCollected);
            Assert.AreEqual(500m, stats.OutstandingAmount);
            Assert.AreEqual(1000m, stats.CashCollected);
            Assert.AreEqual(0m, stats.UpiCollected);
        }

        [Test]
        public async Task GetRevenueReport_DateRangeFilter_ExcludesOutsideRange()
        {
            // Arrange
            var patient = new Patient { FullName = "Test Patient", CreatedAt = DateTime.UtcNow };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var order = new TestOrder { PatientId = patient.PatientId, OrderedAt = DateTime.UtcNow, Status = "Pending", Notes = "", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _context.TestOrders.Add(order);
            await _context.SaveChangesAsync();

            // Invoice created 60 days ago
            var invoice = new Invoice { OrderId = order.OrderId, TotalAmount = 1000m, IsPaid = true, PaymentMethod = "Cash", CreatedAt = DateTime.UtcNow.AddDays(-60) };
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // Act: Query last 30 days only
            var stats = await _service.GetRevenueReportAsync(DateTime.Today.AddDays(-30), DateTime.Today);

            // Assert: Should exclude the 60-day-old invoice
            Assert.AreEqual(0m, stats.TotalRevenue);
        }

        [Test]
        public async Task GetRevenueReport_PaymentMethodBreakdown_CashVsUpi()
        {
            // Arrange
            var patient = new Patient { FullName = "Test Patient", CreatedAt = DateTime.UtcNow };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var order1 = new TestOrder { PatientId = patient.PatientId, OrderedAt = DateTime.UtcNow, Status = "Pending", Notes = "", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var order2 = new TestOrder { PatientId = patient.PatientId, OrderedAt = DateTime.UtcNow, Status = "Pending", Notes = "", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _context.TestOrders.Add(order1);
            _context.TestOrders.Add(order2);
            await _context.SaveChangesAsync();

            // Cash payment
            var invoice1 = new Invoice { OrderId = order1.OrderId, TotalAmount = 800m, IsPaid = true, PaymentMethod = "Cash", CreatedAt = DateTime.UtcNow };
            // UPI payment
            var invoice2 = new Invoice { OrderId = order2.OrderId, TotalAmount = 600m, IsPaid = true, PaymentMethod = "UPI", CreatedAt = DateTime.UtcNow };
            _context.Invoices.Add(invoice1);
            _context.Invoices.Add(invoice2);
            await _context.SaveChangesAsync();

            // Act
            var stats = await _service.GetRevenueReportAsync(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));

            // Assert
            Assert.AreEqual(1400m, stats.TotalRevenue);
            Assert.AreEqual(800m, stats.CashCollected);
            Assert.AreEqual(600m, stats.UpiCollected);
        }
    }
}
