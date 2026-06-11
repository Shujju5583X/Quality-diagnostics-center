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
        private string _tempLogoBackupPath;

        [SetUp]
        public void SetUp()
        {
            _mockResultRepo = new Mock<IResultRepository>();
            _mockTestTypeRepo = new Mock<IRepository<TestType>>();
            _service = new PdfReportService(_mockResultRepo.Object, _mockTestTypeRepo.Object);
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
        public async Task GenerateReport_ShouldGeneratePdf_WhenLogoDoesNotExist()
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

            _mockResultRepo.Setup(r => r.GetResultsForOrderAsync(1, default))
                           .ReturnsAsync(new List<Result>
                           {
                               new Result { ResultId = 1, Value = 15, TestType = new TestType { Name = "Glucose", Unit = "mg/dL", ReferenceRangeLow = 70, ReferenceRangeHigh = 100 } }
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
        public async Task GenerateReport_ShouldGeneratePdf_WhenLogoExists()
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
        public async Task GenerateVerificationReport_ManualInspection()
        {
            var order = new TestOrder
            {
                OrderId = 999,
                OrderedAt = DateTime.UtcNow.ToString("O"),
                PatientId = 4,
                Patient = new Patient 
                { 
                    PatientId = 4, 
                    FullName = "Yash M. Patel",
                    DateOfBirth = "2005-08-25"
                }
            };

            var mockResults = new List<Result>
            {
                // CBC Group (Hematology)
                new Result { ResultId = 101, OrderId = 999, TypeId = 1, Value = 12.5, IsAbnormal = true, TestType = new TestType { Name = "Hemoglobin (Hb)", Unit = "g/dL", ReferenceRangeLow = 13.0, ReferenceRangeHigh = 17.0, Category = "HEMATOLOGY", GroupName = "Complete Blood Count (CBC)", SortOrder = 1, Method = "Fully automated cell counter" } },
                new Result { ResultId = 102, OrderId = 999, TypeId = 2, Value = 5.2, IsAbnormal = false, TestType = new TestType { Name = "Total RBC count", Unit = "mill/cumm", ReferenceRangeLow = 4.5, ReferenceRangeHigh = 5.5, Category = "HEMATOLOGY", GroupName = "Complete Blood Count (CBC)", SortOrder = 2 } },
                new Result { ResultId = 103, OrderId = 999, TypeId = 8, Value = 9000, IsAbnormal = false, TestType = new TestType { Name = "Total WBC count", Unit = "cumm", ReferenceRangeLow = 4000, ReferenceRangeHigh = 11000, Category = "HEMATOLOGY", GroupName = "Complete Blood Count (CBC)", SortOrder = 8 } },
                new Result { ResultId = 104, OrderId = 999, TypeId = 14, Value = 150000, IsAbnormal = false, TestType = new TestType { Name = "Platelet Count", Unit = "cumm", ReferenceRangeLow = 150000, ReferenceRangeHigh = 410000, Category = "HEMATOLOGY", GroupName = "Complete Blood Count (CBC)", SortOrder = 14 } },
                
                // Lipid Profile (Biochemistry)
                new Result { ResultId = 105, OrderId = 999, TypeId = 32, Value = 250, IsAbnormal = true, TestType = new TestType { Name = "Cholesterol, Total", Unit = "mg/dL", ReferenceRangeLow = 0, ReferenceRangeHigh = 200, Category = "BIOCHEMISTRY", GroupName = "Lipid Profile", SortOrder = 1, Method = "Spectrophotometry", Interpretation = "Desirable: < 200 mg/dL.\nHigh: > 240 mg/dL." } },
                new Result { ResultId = 106, OrderId = 999, TypeId = 33, Value = 100, IsAbnormal = false, TestType = new TestType { Name = "Triglycerides", Unit = "mg/dL", ReferenceRangeLow = 0, ReferenceRangeHigh = 150, Category = "BIOCHEMISTRY", GroupName = "Lipid Profile", SortOrder = 2 } },
                new Result { ResultId = 107, OrderId = 999, TypeId = 34, Value = 50, IsAbnormal = false, TestType = new TestType { Name = "HDL Cholesterol", Unit = "mg/dL", ReferenceRangeLow = 40, ReferenceRangeHigh = 60, Category = "BIOCHEMISTRY", GroupName = "Lipid Profile", SortOrder = 3 } },
                
                // Qualitative - Blood Group (Clinical Pathology)
                new Result { ResultId = 108, OrderId = 999, TypeId = 51, Value = 7, IsAbnormal = false, TestType = new TestType { Name = "Blood Grouping & Rh", Unit = "Blood Group", ReferenceRangeLow = 1, ReferenceRangeHigh = 8, Category = "CLINICAL PATHOLOGY", GroupName = "Blood Group", SortOrder = 1, Method = "Monoclonal slide grouping (Agglutination test) by slide method" } }
            };

            _mockResultRepo.Setup(r => r.GetResultsForOrderAsync(999, default)).ReturnsAsync(mockResults);

            // Act
            string filepath = await _service.GenerateReportAsync(order);
            
            // Copy the report to the workspace root for manual inspection
            string copyPath = Path.Combine(@"E:\Quality diagnostics center", "Sample_Verification_Report.pdf");
            File.Copy(filepath, copyPath, true);

            Console.WriteLine("Verification report saved to: " + copyPath);
        }
    }
}
