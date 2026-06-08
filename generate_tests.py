import os

tests_dir = r"E:\Quality diagnostics center\LabSystem.Tests"
os.makedirs(tests_dir, exist_ok=True)

tests_files = {
    "ResultServiceTests.cs": """using System;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.Services;
using Moq;
using NUnit.Framework;

namespace LabSystem.Tests
{
    [TestFixture]
    public class ResultServiceTests
    {
        private Mock<IResultRepository> _mockResultRepo;
        private Mock<IRepository<TestType>> _mockTestTypeRepo;
        private Mock<IRepository<AuditLog>> _mockAuditRepo;
        private ResultService _service;

        [SetUp]
        public void SetUp()
        {
            _mockResultRepo = new Mock<IResultRepository>();
            _mockTestTypeRepo = new Mock<IRepository<TestType>>();
            _mockAuditRepo = new Mock<IRepository<AuditLog>>();
            _service = new ResultService(_mockResultRepo.Object, _mockTestTypeRepo.Object, _mockAuditRepo.Object);
        }

        [Test]
        public void AddResult_ShouldFlagAbnormal_WhenValueAboveHighRange()
        {
            // Arrange
            var testType = new TestType { TypeId = 1, ReferenceRangeLow = 10, ReferenceRangeHigh = 20 };
            _mockTestTypeRepo.Setup(r => r.GetById(1)).Returns(testType);

            var result = new Result { TypeId = 1, Value = 25 };

            // Act
            _service.AddResult(result);

            // Assert
            Assert.IsTrue(result.IsAbnormal);
            _mockResultRepo.Verify(r => r.Add(It.IsAny<Result>()), Times.Once);
            _mockAuditRepo.Verify(r => r.Add(It.IsAny<AuditLog>()), Times.Once);
        }

        [Test]
        public void AddResult_ShouldNotFlagAbnormal_WhenValueWithinRange()
        {
            // Arrange
            var testType = new TestType { TypeId = 1, ReferenceRangeLow = 10, ReferenceRangeHigh = 20 };
            _mockTestTypeRepo.Setup(r => r.GetById(1)).Returns(testType);

            var result = new Result { TypeId = 1, Value = 15 };

            // Act
            _service.AddResult(result);

            // Assert
            Assert.IsFalse(result.IsAbnormal);
        }
    }
}
""",
    "OrderServiceTests.cs": """using System;
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
    }
}
"""
}

for name, content in tests_files.items():
    with open(os.path.join(tests_dir, name), "w", encoding="utf-8") as f:
        f.write(content)

print("Tests files generated.")
