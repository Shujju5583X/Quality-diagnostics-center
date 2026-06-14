using System;
using System.IO;
using System.Collections.Generic;
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
        private PdfReportService _service;
        private string _logoPath;

        [SetUp]
        public void SetUp()
        {
            _mockResultRepo = new Mock<IResultRepository>();
            _mockTestTypeRepo = new Mock<IRepository<TestType>>();
            
            var testDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
            Directory.CreateDirectory(testDataDir);
            _logoPath = Path.Combine(testDataDir, "test_logo.png");

            // Generate 1x1 white pixel PNG programmatically using System.Drawing
            using (var bitmap = new System.Drawing.Bitmap(1, 1))
            {
                bitmap.SetPixel(0, 0, System.Drawing.Color.White);
                bitmap.Save(_logoPath, System.Drawing.Imaging.ImageFormat.Png);
            }

            _service = new PdfReportService(_mockResultRepo.Object, _mockTestTypeRepo.Object, null, _logoPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_logoPath))
            {
                try { File.Delete(_logoPath); } catch { }
            }
        }

        [Test]
        public async Task GenerateReport_ShouldGeneratePdf_WhenLogoDoesNotExist()
        {
            // Arrange: Temporary service instance with non-existent logo path
            var nonExistentLogoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "non_existent.png");
            var tempService = new PdfReportService(_mockResultRepo.Object, _mockTestTypeRepo.Object, null, nonExistentLogoPath);

            var order = new TestOrder
            {
                OrderId = 1,
                OrderedAt = DateTime.Now,
                Patient = new Patient { PatientId = 1, FullName = "John Doe" }
            };

            _mockResultRepo.Setup(r => r.GetResultsForOrderAsync(1, default))
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

            _mockResultRepo.Setup(r => r.GetResultsForOrderAsync(2, default))
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
                Patient = new Patient { PatientId = 3, FullName = "Abnormal Patient", DateOfBirth = DateTime.Now.AddYears(-30) }
            };

            _mockResultRepo.Setup(r => r.GetResultsForOrderAsync(3, default))
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
                Patient = new Patient { PatientId = 4, FullName = "Amended Patient", DateOfBirth = DateTime.Now.AddYears(-40) }
            };

            _mockResultRepo.Setup(r => r.GetResultsForOrderAsync(4, default))
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
                Patient = new Patient { PatientId = 5, FullName = "Empty Patient", DateOfBirth = DateTime.Now.AddYears(-20) }
            };

            _mockResultRepo.Setup(r => r.GetResultsForOrderAsync(5, default))
                           .ReturnsAsync(new List<Result>()); // Empty result list

            string filepath = await _service.GenerateReportAsync(order);
            Assert.IsTrue(File.Exists(filepath));
            Assert.IsTrue(new FileInfo(filepath).Length > 0);
            File.Delete(filepath);
        }

        [Test]
        public async Task GenerateReport_ShouldHandleNullDateOfBirth()
        {
            var order = new TestOrder
            {
                OrderId = 6,
                OrderedAt = DateTime.Now,
                Patient = new Patient { PatientId = 6, FullName = "Null DOB Patient", DateOfBirth = null }
            };

            _mockResultRepo.Setup(r => r.GetResultsForOrderAsync(6, default))
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
                DiscountAmount = 50,
                TaxAmount = 30,
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

        [Test]
        public void SpecimenStatus_Enum_Serialization_RoundTrip()
        {
            foreach (SpecimenStatus status in Enum.GetValues(typeof(SpecimenStatus)))
            {
                string str = status.ToString();
                SpecimenStatus parsed = (SpecimenStatus)Enum.Parse(typeof(SpecimenStatus), str);
                Assert.AreEqual(status, parsed);
            }
        }
    }
}
