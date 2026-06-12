using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
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
    }
}
