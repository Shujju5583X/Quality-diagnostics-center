using System;
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
    public class OrderServiceTests
    {
        private Mock<ITestOrderRepository> _mockOrderRepo;
        private Mock<ITestTypeRepository> _mockTestTypeRepo;
        private Mock<IStaffRepository> _mockStaffRepo;
        private OrderService _service;

        [SetUp]
        public void SetUp()
        {
            _mockOrderRepo = new Mock<ITestOrderRepository>();
            _mockTestTypeRepo = new Mock<ITestTypeRepository>();
            _mockStaffRepo = new Mock<IStaffRepository>();
            _service = new OrderService(
                _mockOrderRepo.Object, 
                _mockTestTypeRepo.Object, 
                _mockStaffRepo.Object);
        }

        [Test]
        public async Task UpdateOrderStatus_ShouldUpdateStatus()
        {
            // Arrange
            var order = new TestOrder { OrderId = 1, Status = "Pending" };
            _mockOrderRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
            _mockOrderRepo.Setup(r => r.UpdateAsync(order, It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));

            // Act
            await _service.UpdateOrderStatusAsync(1, "Complete");

            // Assert
            Assert.AreEqual("Complete", order.Status);
            _mockOrderRepo.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        }

    }
}
