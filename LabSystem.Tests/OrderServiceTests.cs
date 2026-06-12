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
        private Mock<IRepository<Specimen>> _mockSpecimenRepo;
        private OrderService _service;

        [SetUp]
        public void SetUp()
        {
            _mockOrderRepo = new Mock<ITestOrderRepository>();
            _mockTestTypeRepo = new Mock<ITestTypeRepository>();
            _mockSpecimenRepo = new Mock<IRepository<Specimen>>();
            _service = new OrderService(
                _mockOrderRepo.Object, 
                _mockTestTypeRepo.Object, 
                _mockSpecimenRepo.Object);
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

        [Test]
        public async Task CreateOrder_ShouldGenerateSpecimens_ForUniqueSampleTypes()
        {
            // Arrange
            var order = new TestOrder { OrderId = 123 };
            var testTypeIds = new System.Collections.Generic.List<int> { 1, 2, 3 };

            var testType1 = new TestType { TypeId = 1, SampleType = "Blood" };
            var testType2 = new TestType { TypeId = 2, SampleType = "Urine" };
            var testType3 = new TestType { TypeId = 3, SampleType = "Blood" }; // duplicate sample type

            _mockTestTypeRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(testType1);
            _mockTestTypeRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(testType2);
            _mockTestTypeRepo.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(testType3);

            _mockOrderRepo.Setup(r => r.AddOrderWithTestTypesAsync(order, testTypeIds, It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));
            _mockSpecimenRepo.Setup(r => r.AddAsync(It.IsAny<Specimen>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));

            // Act
            await _service.CreateOrderAsync(order, testTypeIds);

            // Assert
            _mockSpecimenRepo.Verify(r => r.AddAsync(It.Is<Specimen>(s => s.SampleType == "Blood"), It.IsAny<CancellationToken>()), Times.Once);
            _mockSpecimenRepo.Verify(r => r.AddAsync(It.Is<Specimen>(s => s.SampleType == "Urine"), It.IsAny<CancellationToken>()), Times.Once);
            _mockSpecimenRepo.Verify(r => r.AddAsync(It.IsAny<Specimen>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

    }
}
