using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.Core.Enums;
using LabSystem.Services;
using Moq;
using NUnit.Framework;

namespace LabSystem.Tests
{
    [TestFixture]
    public class PdfReportServiceTests
    {
        private Mock<IResultRepository> _mockResultRepo;
        private Mock<IRepository<TestType>> _mockTestTypeRepo;
        private Mock<ISettingRepository> _mockSettingRepo;
        private Mock<ITestOrderRepository> _mockOrderRepo;
        private PdfReportService _service;

        [SetUp]
        public void SetUp()
        {
            _mockResultRepo = new Mock<IResultRepository>();
            _mockTestTypeRepo = new Mock<IRepository<TestType>>();
            _mockSettingRepo = new Mock<ISettingRepository>();
            _mockSettingRepo.Setup(r => r.GetAllAsync(default(CancellationToken)))
                           .ReturnsAsync(new List<Setting>());

            _mockOrderRepo = new Mock<ITestOrderRepository>();
            _mockOrderRepo.Setup(r => r.GetDailySequenceNumberAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync((int oId, DateTime dt, CancellationToken ct) => oId);

            _service = new PdfReportService(_mockResultRepo.Object, _mockTestTypeRepo.Object, null, _mockSettingRepo.Object, _mockOrderRepo.Object);
        }

        [Test]
        public async Task GenerateReport_ShouldGeneratePdf_WhenLogoDoesNotExist()
        {
            // Arrange: Temporary service instance with non-existent logo path
            var tempService = new PdfReportService(_mockResultRepo.Object, _mockTestTypeRepo.Object, null, _mockSettingRepo.Object, _mockOrderRepo.Object);

            var order = new TestOrder
            {
                OrderId = 1,
                OrderedAt = DateTime.Now,
                Patient = new Patient { PatientId = 1, FullName = "John Doe" }
            };

            _mockResultRepo.Setup(r => r.GetResultsForOrderAsync(1, default(CancellationToken)))
                           .ReturnsAsync(new List<Result>
                           {
                               new Result { ResultId = 1, Value = 15, TestType = new TestType { Name = "Glucose", Unit = "mg/dL", ReferenceRangeLow = 70, ReferenceRangeHigh = 100 } }
                           });

            // Act
            string filepath = await tempService.GenerateReportAsync(order);

            // Assert
            Assert.IsTrue(File.Exists(filepath));
            Assert.IsTrue(new FileInfo(filepath).Length > 0);

            // Clean up report
            if (File.Exists(filepath))
            {
                File.Delete(filepath);
            }
        }

        [Test]
        public async Task GenerateReport_ShouldGeneratePdf_WhenLogoExists()
        {
            // Arrange
            var order = new TestOrder
            {
                OrderId = 2,
                OrderedAt = DateTime.Now,
                Patient = new Patient { PatientId = 2, FullName = "Jane Smith" }
            };

            _mockResultRepo.Setup(r => r.GetResultsForOrderAsync(2, default(CancellationToken)))
                           .ReturnsAsync(new List<Result>
                           {
                               new Result { ResultId = 2, Value = 12, TestType = new TestType { Name = "Hemoglobin", Unit = "g/dL", ReferenceRangeLow = 12, ReferenceRangeHigh = 16 } }
                           });

            // Act
            string filepath = await _service.GenerateReportAsync(order);

            // Assert
            Assert.IsTrue(File.Exists(filepath));
            Assert.IsTrue(new FileInfo(filepath).Length > 0);

            // Clean up report
            if (File.Exists(filepath))
            {
                File.Delete(filepath);
            }
        }

        [Test]
        public async Task GenerateReport_ShouldHandleAbnormalResults()
        {
            var order = new TestOrder
            {
                OrderId = 3,
                OrderedAt = DateTime.Now,
                Patient = new Patient { PatientId = 3, FullName = "Abnormal Patient", Age = 30 }
            };

            _mockResultRepo.Setup(r => r.GetResultsForOrderAsync(3, default(CancellationToken)))
                           .ReturnsAsync(new List<Result>
                           {
                               new Result { ResultId = 3, Value = 150, IsAbnormal = true, TestType = new TestType { Name = "Glucose", Unit = "mg/dL", ReferenceRangeLow = 70, ReferenceRangeHigh = 100 } }
                           });

            string filepath = await _service.GenerateReportAsync(order);
            Assert.IsTrue(File.Exists(filepath));
            Assert.IsTrue(new FileInfo(filepath).Length > 0);
            File.Delete(filepath);
        }

        [Test]
        public async Task GenerateReport_ShouldHandleAmendedResults()
        {
            var order = new TestOrder
            {
                OrderId = 4,
                OrderedAt = DateTime.Now,
                Patient = new Patient { PatientId = 4, FullName = "Amended Patient", Age = 40 }
            };

            _mockResultRepo.Setup(r => r.GetResultsForOrderAsync(4, default(CancellationToken)))
                           .ReturnsAsync(new List<Result>
                           {
                               new Result { ResultId = 4, Value = 14, IsAmended = true, AmendmentReason = "Typo correction", AmendedAt = DateTime.Now, TestType = new TestType { Name = "Hemoglobin", Unit = "g/dL", ReferenceRangeLow = 12, ReferenceRangeHigh = 16 } }
                           });

            string filepath = await _service.GenerateReportAsync(order);
            Assert.IsTrue(File.Exists(filepath));
            Assert.IsTrue(new FileInfo(filepath).Length > 0);
            File.Delete(filepath);
        }

        [Test]
        public async Task GenerateReport_ShouldHandleEmptyOrderLines()
        {
            var order = new TestOrder
            {
                OrderId = 5,
                OrderedAt = DateTime.Now,
                Patient = new Patient { PatientId = 5, FullName = "Empty Patient", Age = 20 }
            };

            _mockResultRepo.Setup(r => r.GetResultsForOrderAsync(5, default(CancellationToken)))
                           .ReturnsAsync(new List<Result>()); // Empty result list

            string filepath = await _service.GenerateReportAsync(order);
            Assert.IsTrue(File.Exists(filepath));
            Assert.IsTrue(new FileInfo(filepath).Length > 0);
            File.Delete(filepath);
        }

        [Test]
        public async Task GenerateReport_ShouldHandleZeroAge()
        {
            var order = new TestOrder
            {
                OrderId = 6,
                OrderedAt = DateTime.Now,
                Patient = new Patient { PatientId = 6, FullName = "Null DOB Patient", Age = 0 }
            };

            _mockResultRepo.Setup(r => r.GetResultsForOrderAsync(6, default(CancellationToken)))
                           .ReturnsAsync(new List<Result>
                           {
                               new Result { ResultId = 6, Value = 5.0, TestType = new TestType { Name = "Potassium", Unit = "mEq/L", ReferenceRangeLow = 3.5, ReferenceRangeHigh = 5.1 } }
                           });

            string filepath = await _service.GenerateReportAsync(order);
            Assert.IsTrue(File.Exists(filepath));
            Assert.IsTrue(new FileInfo(filepath).Length > 0);
            File.Delete(filepath);
        }

        [Test]
        public async Task GenerateInvoicePdf_ShouldGeneratePdf()
        {
            var patient = new Patient { PatientId = 7, FullName = "Invoice Patient", Uhid = "QDC-12345" };
            var order = new TestOrder
            {
                OrderId = 7,
                OrderedAt = DateTime.Now,
                Patient = patient,
                ReferredBy = "Dr. Tester",
                TestTypes = new List<TestType>
                {
                    new TestType { TypeId = 1, Name = "Glucose", Price = 150, Unit = "mg/dL" },
                    new TestType { TypeId = 2, Name = "Potassium", Price = 200, Unit = "mEq/L" }
                }
            };
            var invoice = new Invoice
            {
                InvoiceId = 101,
                OrderId = 7,
                Order = order,
                TotalAmount = 350,
                DiscountPercent = 50m / 350m * 100m,
                TaxPercent = 30m / (350m - 50m) * 100m,
                IsPaid = true,
                PaymentMethod = "Card",
                CreatedAt = DateTime.Now,
                Payments = new List<Payment>
                {
                    new Payment { PaymentId = 1, InvoiceId = 101, Amount = 330, PaymentMethod = "Card", PaymentDate = DateTime.Now }
                }
            };

            string filepath = await _service.GenerateInvoicePdfAsync(invoice);
            Assert.IsTrue(File.Exists(filepath));
            Assert.IsTrue(new FileInfo(filepath).Length > 0);
            File.Delete(filepath);
        }

        [Test]
        public async Task GenerateReport_ShouldIncludeInstruments_ForCbcTest()
        {
            // Arrange
            var order = new TestOrder
            {
                OrderId = 8,
                OrderedAt = DateTime.Now,
                Patient = new Patient { PatientId = 8, FullName = "CBC Patient", Age = 25 }
            };

            _mockResultRepo.Setup(r => r.GetResultsForOrderAsync(8, default(CancellationToken)))
                           .ReturnsAsync(new List<Result>
                           {
                               new Result 
                               { 
                                   ResultId = 8, 
                                   Value = 14.5, 
                                   TestType = new TestType 
                                   { 
                                       Name = "Hemoglobin (Hb)", 
                                       Unit = "g/dL", 
                                       ReferenceRangeLow = 13, 
                                       ReferenceRangeHigh = 17,
                                       GroupName = "Complete Blood Count (CBC)",
                                       Category = "HEMATOLOGY"
                                   } 
                               }
                           });

            // Act
            string filepath = await _service.GenerateReportAsync(order);

            // Assert
            Assert.IsTrue(File.Exists(filepath));
            Assert.IsTrue(new FileInfo(filepath).Length > 0);

            // Clean up report
            if (File.Exists(filepath))
            {
                File.Delete(filepath);
            }
        }
    }

    [TestFixture]
    public class EnumSerializationTests
    {
        [Test]
        public void OrderStatus_Enum_Serialization_RoundTrip()
        {
            foreach (OrderStatus status in Enum.GetValues(typeof(OrderStatus)))
            {
                string str = status.ToString();
                OrderStatus parsed = (OrderStatus)Enum.Parse(typeof(OrderStatus), str);
                Assert.AreEqual(status, parsed);
            }
        }
    }
}
