using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.Data;
using LabSystem.Services;
using Moq;
using NUnit.Framework;

namespace LabSystem.Tests
{
    [TestFixture]
    public class WorkflowServiceTests
    {
        private SQLiteConnection _connection;
        private LabDbContext _context;
        private Mock<IResultService> _mockResultService;
        private Mock<IBillingService> _mockBillingService;
        private Mock<IPdfReportService> _mockReportService;
        private WorkflowService _workflowService;

        [SetUp]
        public void SetUp()
        {
            _connection = new SQLiteConnection("Data Source=:memory:");
            _connection.Open();

            _context = new LabDbContext(_connection);

            // Read V1__init.sql and run it to create tables
            var initSqlPath = FindFileUpwards("LabSystem.Data", "Migrations", "V1__init.sql");
            if (initSqlPath == null || !File.Exists(initSqlPath))
            {
                throw new FileNotFoundException("Could not find V1__init.sql for SQLite setup.");
            }
            string sql = File.ReadAllText(initSqlPath);
            _context.Database.ExecuteSqlCommand(sql);
            
            _context.Database.ExecuteSqlCommand(@"
                CREATE TABLE IF NOT EXISTS Payments (
                    PaymentId INTEGER PRIMARY KEY AUTOINCREMENT,
                    InvoiceId INTEGER NOT NULL,
                    Amount REAL NOT NULL,
                    PaymentMethod TEXT,
                    PaymentDate DATETIME NOT NULL,
                    FOREIGN KEY(InvoiceId) REFERENCES Invoices(InvoiceId) ON DELETE CASCADE
                );
            ");

            _mockResultService = new Mock<IResultService>();
            _mockBillingService = new Mock<IBillingService>();
            _mockReportService = new Mock<IPdfReportService>();

            _workflowService = new WorkflowService(_context, _mockResultService.Object, _mockBillingService.Object, _mockReportService.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();
            _connection?.Dispose();
        }

        private static string FindFileUpwards(params string[] pathParts)
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, Path.Combine(pathParts));
                if (File.Exists(candidate))
                    return candidate;
                dir = dir.Parent;
            }
            return null;
        }

        [Test]
        public async Task QuickFinalizeAsync_ShouldCallResultBillingAndReportServices()
        {
            // Arrange
            var results = new List<Result>
            {
                new Result { TypeId = 1, Value = 15.0 }
            };

            var invoice = new Invoice { InvoiceId = 1, OrderId = 1, TotalAmount = 500, IsPaid = false };

            _mockBillingService.Setup(b => b.GenerateInvoiceAsync(1)).ReturnsAsync(invoice);

            // Act
            await _workflowService.QuickFinalizeAsync(1, results, 1, "Cash", CancellationToken.None);

            // Assert
            _mockResultService.Verify(r => r.AddResultAsync(It.Is<Result>(res => res.TypeId == 1 && res.Value == 15.0), It.IsAny<CancellationToken>()), Times.Once);
            _mockBillingService.Verify(b => b.GenerateInvoiceAsync(1), Times.Once);
            _mockBillingService.Verify(b => b.AddPaymentAsync(1, 500, "Cash"), Times.Once);
            _mockReportService.Verify(r => r.GenerateReportAsync(It.IsAny<TestOrder>(), true, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void QuickFinalizeAsync_ShouldRollbackTransaction_OnException()
        {
            // Arrange
            var results = new List<Result> { new Result { TypeId = 1, Value = 15.0 } };
            
            // Force an exception in the middle of the transaction
            _mockBillingService.Setup(b => b.GenerateInvoiceAsync(1)).ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(async () => await _workflowService.QuickFinalizeAsync(1, results, 1, "Cash", CancellationToken.None));
            Assert.AreEqual("Database error", ex.Message);
            
            // Transaction should have been rolled back, but we verify it indirectly by ensuring report service is never called
            _mockReportService.Verify(r => r.GenerateReportAsync(It.IsAny<TestOrder>(), true, It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
