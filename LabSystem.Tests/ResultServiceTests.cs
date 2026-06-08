using System;
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
