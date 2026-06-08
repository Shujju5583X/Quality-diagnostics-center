using System;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.Services;
using Moq;
using NUnit.Framework;

namespace LabSystem.Tests
{
    [TestFixture]
    public class OrderServiceTests
    {
        private Mock<ITestOrderRepository> _mockOrderRepo;
        private Mock<IRepository<AuditLog>> _mockAuditRepo;
        private OrderService _service;

        [SetUp]
        public void SetUp()
        {
            _mockOrderRepo = new Mock<ITestOrderRepository>();
            _mockAuditRepo = new Mock<IRepository<AuditLog>>();
            _service = new OrderService(_mockOrderRepo.Object, _mockAuditRepo.Object);
        }

        [Test]
        public void UpdateOrderStatus_ShouldUpdateStatus_AndAddAuditLog()
        {
            // Arrange
            var order = new TestOrder { OrderId = 1, Status = "Pending" };
            _mockOrderRepo.Setup(r => r.GetById(1)).Returns(order);

            // Act
            _service.UpdateOrderStatus(1, "Complete");

            // Assert
            Assert.AreEqual("Complete", order.Status);
            _mockOrderRepo.Verify(r => r.Update(order), Times.Once);
            _mockAuditRepo.Verify(r => r.Add(It.Is<AuditLog>(a => a.Action == "Updated" && a.EntityType == "TestOrder")), Times.Once);
        }

        [Test]
        public void PrintCorrectHash()
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("1234");
            Console.WriteLine("CORRECT_HASH: " + hash);
            Assert.Pass();
        }
    }
}
