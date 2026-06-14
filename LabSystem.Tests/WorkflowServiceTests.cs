using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.Services;
using Moq;
using NUnit.Framework;

namespace LabSystem.Tests
{
    [TestFixture]
    public class WorkflowServiceTests
    {
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<ITestOrderRepository> _mockOrderRepo;
        private Mock<IDbContextTransaction> _mockTransaction;
        private Mock<IResultService> _mockResultService;
        private Mock<IBillingService> _mockBillingService;
        private Mock<IPdfReportService> _mockReportService;
        private WorkflowService _workflowService;

        [SetUp]
        public void SetUp()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockOrderRepo = new Mock<ITestOrderRepository>();
            _mockTransaction = new Mock<IDbContextTransaction>();
            _mockResultService = new Mock<IResultService>();
            _mockBillingService = new Mock<IBillingService>();
            _mockReportService = new Mock<IPdfReportService>();

            _mockUnitOfWork.Setup(u => u.BeginTransaction()).Returns(_mockTransaction.Object);

            _workflowService = new WorkflowService(
                _mockUnitOfWork.Object,
                _mockOrderRepo.Object,
                _mockResultService.Object,
                _mockBillingService.Object,
                _mockReportService.Object);
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
            var order = new TestOrder { OrderId = 1 };

            _mockBillingService.Setup(b => b.GenerateInvoiceAsync(1)).ReturnsAsync(invoice);
            _mockOrderRepo.Setup(o => o.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);

            // Act
            await _workflowService.QuickFinalizeAsync(1, results, 1, "Cash", CancellationToken.None);

            // Assert
            _mockResultService.Verify(r => r.AddResultAsync(It.Is<Result>(res => res.TypeId == 1 && res.Value == 15.0), It.IsAny<CancellationToken>()), Times.Once);
            _mockBillingService.Verify(b => b.GenerateInvoiceAsync(1), Times.Once);
            _mockBillingService.Verify(b => b.AddPaymentAsync(1, 500, "Cash"), Times.Once);
            _mockReportService.Verify(r => r.GenerateReportAsync(order, true, It.IsAny<CancellationToken>()), Times.Once);
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
            
            _mockTransaction.Verify(t => t.Rollback(), Times.Once);
            _mockReportService.Verify(r => r.GenerateReportAsync(It.IsAny<TestOrder>(), true, It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
