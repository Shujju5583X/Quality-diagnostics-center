using System;
using System.IO;
using System.Collections.Generic;
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
        private PdfReportService _service;
        private string _logoPath;
        private string _tempLogoBackupPath;

        [SetUp]
        public void SetUp()
        {
            // Enable PDFsharp to resolve installed Windows fonts automatically
            PdfSharp.Fonts.GlobalFontSettings.UseWindowsFontsUnderWindows = true;

            _mockResultRepo = new Mock<IResultRepository>();
            _service = new PdfReportService(_mockResultRepo.Object);
            _logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
            _tempLogoBackupPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo_temp_backup.png");

            // Backup existing logo if it exists in the test output directory
            if (File.Exists(_logoPath))
            {
                File.Move(_logoPath, _tempLogoBackupPath);
            }
        }

        [TearDown]
        public void TearDown()
        {
            // Restore backup logo if it was backed up
            if (File.Exists(_tempLogoBackupPath))
            {
                if (File.Exists(_logoPath))
                {
                    File.Delete(_logoPath);
                }
                File.Move(_tempLogoBackupPath, _logoPath);
            }
            else if (File.Exists(_logoPath))
            {
                File.Delete(_logoPath);
            }
        }

        [Test]
        public void GenerateReport_ShouldGeneratePdf_WhenLogoDoesNotExist()
        {
            // Arrange: Ensure logo does not exist in target path
            if (File.Exists(_logoPath))
            {
                File.Delete(_logoPath);
            }

            var order = new TestOrder
            {
                OrderId = 1,
                OrderedAt = DateTime.Now.ToString("yyyy-MM-dd"),
                Patient = new Patient { PatientId = 1, FullName = "John Doe" }
            };

            _mockResultRepo.Setup(r => r.GetResultsForOrder(1))
                           .Returns(new List<Result>
                           {
                               new Result { ResultId = 1, Value = 15, TestType = new TestType { Name = "Glucose", Unit = "mg/dL", ReferenceRangeLow = 70, ReferenceRangeHigh = 100 } }
                           });

            // Act
            string filepath = _service.GenerateReport(order);

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
        public void GenerateReport_ShouldGeneratePdf_WhenLogoExists()
        {
            // Arrange: Find and copy the actual logo file to target path
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            string sourceLogo = null;
            while (dir != null)
            {
                string candidate = Path.Combine(dir, "LabSystem.UI", "logo.png");
                if (File.Exists(candidate))
                {
                    sourceLogo = candidate;
                    break;
                }
                dir = Path.GetDirectoryName(dir);
            }

            if (sourceLogo != null)
            {
                File.Copy(sourceLogo, _logoPath, true);
            }
            else
            {
                // Fallback to creating a dummy text file if logo not found (tests behavior when file is invalid/dummy,
                // though we expect the actual image to exist and load correctly)
                Assert.Inconclusive("Actual logo.png could not be found to test image embedding.");
                return;
            }

            var order = new TestOrder
            {
                OrderId = 2,
                OrderedAt = DateTime.Now.ToString("yyyy-MM-dd"),
                Patient = new Patient { PatientId = 2, FullName = "Jane Smith" }
            };

            _mockResultRepo.Setup(r => r.GetResultsForOrder(2))
                           .Returns(new List<Result>
                           {
                               new Result { ResultId = 2, Value = 12, TestType = new TestType { Name = "Hemoglobin", Unit = "g/dL", ReferenceRangeLow = 12, ReferenceRangeHigh = 16 } }
                           });

            // Act
            string filepath = _service.GenerateReport(order);

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
