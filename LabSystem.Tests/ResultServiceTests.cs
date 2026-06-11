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
        public async Task AddResult_ShouldFlagAbnormal_WhenValueAboveHighRange()
        {
            // Arrange
            var testType = new TestType { TypeId = 1, ReferenceRangeLow = 10, ReferenceRangeHigh = 20 };
            _mockTestTypeRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(testType);
            _mockResultRepo.Setup(r => r.AddAsync(It.IsAny<Result>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));
            _mockAuditRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));

            var result = new Result { TypeId = 1, Value = 25 };

            // Act
            await _service.AddResultAsync(result);

            // Assert
            Assert.IsTrue(result.IsAbnormal);
            _mockResultRepo.Verify(r => r.AddAsync(It.IsAny<Result>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockAuditRepo.Verify(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task AddResult_ShouldNotFlagAbnormal_WhenValueWithinRange()
        {
            // Arrange
            var testType = new TestType { TypeId = 1, ReferenceRangeLow = 10, ReferenceRangeHigh = 20 };
            _mockTestTypeRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(testType);
            _mockResultRepo.Setup(r => r.AddAsync(It.IsAny<Result>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));
            _mockAuditRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));

            var result = new Result { TypeId = 1, Value = 15 };

            // Act
            await _service.AddResultAsync(result);

            // Assert
            Assert.IsFalse(result.IsAbnormal);
        }
    }
}
