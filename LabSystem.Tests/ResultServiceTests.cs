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
        private Mock<ITestOrderRepository> _mockOrderRepo;
        private ResultService _service;

        [SetUp]
        public void SetUp()
        {
            _mockResultRepo = new Mock<IResultRepository>();
            _mockTestTypeRepo = new Mock<IRepository<TestType>>();
            _mockOrderRepo = new Mock<ITestOrderRepository>();
            _service = new ResultService(
                _mockResultRepo.Object, 
                _mockTestTypeRepo.Object, 
                _mockOrderRepo.Object);
        }

        [Test]
        public async Task AddResult_ShouldFlagAbnormal_WhenValueAboveHighRange()
        {
            // Arrange
            var testType = new TestType { TypeId = 1, ReferenceRangeLow = 10, ReferenceRangeHigh = 20 };
            _mockTestTypeRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(testType);
            _mockResultRepo.Setup(r => r.AddAsync(It.IsAny<Result>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));

            var result = new Result { TypeId = 1, Value = 25 };

            // Act
            await _service.AddResultAsync(result);

            // Assert
            Assert.IsTrue(result.IsAbnormal);
            _mockResultRepo.Verify(r => r.AddAsync(It.IsAny<Result>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task AddResult_ShouldNotFlagAbnormal_WhenValueWithinRange()
        {
            // Arrange
            var testType = new TestType { TypeId = 1, ReferenceRangeLow = 10, ReferenceRangeHigh = 20 };
            _mockTestTypeRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(testType);
            _mockResultRepo.Setup(r => r.AddAsync(It.IsAny<Result>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));

            var result = new Result { TypeId = 1, Value = 15 };

            // Act
            await _service.AddResultAsync(result);

            // Assert
            Assert.IsFalse(result.IsAbnormal);
        }

        [Test]
        public async Task AddResult_ShouldUseBiologicalRange_BasedOnAgeAndGender()
        {
            // Arrange
            var patient = new Patient { Gender = "Female", Age = 25 }; // Female, 25 years old
            var order = new TestOrder { OrderId = 10, Patient = patient };
            order.TestTypes = new System.Collections.Generic.List<TestType> { new TestType { TypeId = 1 } };
            
            var testType = new TestType 
            { 
                TypeId = 1, 
                ReferenceRangeLow = 10, 
                ReferenceRangeHigh = 20, // default fallback
                ReferenceRanges = new System.Collections.Generic.List<ReferenceRange>
                {
                    new ReferenceRange { Gender = "Male", AgeMin = 0, AgeMax = 100, RangeLow = 10, RangeHigh = 20 },
                    new ReferenceRange { Gender = "Female", AgeMin = 18, AgeMax = 45, RangeLow = 5, RangeHigh = 12 } // female specific range
                }
            };

            _mockOrderRepo.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(order);
            _mockTestTypeRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(testType);
            _mockResultRepo.Setup(r => r.AddAsync(It.IsAny<Result>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));

            var result = new Result { OrderId = 10, TypeId = 1, Value = 15 }; // 15 is abnormal for female (range 5-12), but normal for fallback (10-20)

            // Act
            await _service.AddResultAsync(result);

            // Assert
            Assert.IsTrue(result.IsAbnormal);
        }

        [Test]
        public void AmendResult_ShouldThrow_WhenReasonIsEmpty()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => 
                await _service.AmendResultAsync(1, 15.0, "15", "", 1));
        }

        [Test]
        public async Task AmendResult_ShouldUpdateValue_AndSetIsAmendedFlag()
        {
            // Arrange
            var result = new Result { ResultId = 1, TypeId = 1, OrderId = 10, Value = 15, IsAmended = false };
            var testType = new TestType { TypeId = 1, ReferenceRangeLow = 10, ReferenceRangeHigh = 20 };
            
            _mockResultRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(result);
            _mockTestTypeRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(testType);
            _mockResultRepo.Setup(r => r.UpdateAsync(result, It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));

            // Act
            await _service.AmendResultAsync(1, 17.5, "17.5", "Correction", 1);

            // Assert
            Assert.AreEqual(17.5, result.Value);
            Assert.IsTrue(result.IsAmended);
            Assert.AreEqual("Correction", result.AmendmentReason);
            _mockResultRepo.Verify(r => r.UpdateAsync(result, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task AmendResult_ShouldReevaluateAbnormality_AfterAmendment()
        {
            // Arrange
            var result = new Result { ResultId = 1, TypeId = 1, OrderId = 10, Value = 15, IsAbnormal = false };
            var testType = new TestType { TypeId = 1, ReferenceRangeLow = 10, ReferenceRangeHigh = 20 };
            var patient = new Patient { Gender = "Male", Age = 30 };
            var order = new TestOrder { OrderId = 10, Patient = patient };

            _mockResultRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(result);
            _mockTestTypeRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(testType);
            _mockOrderRepo.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(order);
            _mockResultRepo.Setup(r => r.UpdateAsync(result, It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));

            // Act
            await _service.AmendResultAsync(1, 25.0, "25", "Input correction", 1);

            // Assert
            Assert.IsTrue(result.IsAbnormal);
        }
    }
}
