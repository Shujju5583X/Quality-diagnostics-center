using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;

namespace LabSystem.Services
{
    public class PdfReportService : IPdfReportService
    {
        private readonly IResultRepository _resultRepo;
        private readonly string _letterheadPath;

        public PdfReportService(IResultRepository resultRepo)
            : this(resultRepo, GetDefaultLetterheadPath())
        {
        }

        public PdfReportService(IResultRepository resultRepo, string letterheadPath)
        {
            _resultRepo = resultRepo;
            _letterheadPath = letterheadPath;
        }

        private static string GetDefaultLetterheadPath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new[]
            {
                Path.Combine(baseDir, "Assets", "letterhead.jpg"),
                Path.Combine(baseDir, "Assets", "letterhead.jpeg"),
                Path.Combine(baseDir, "Assets", "letterhead.png"),
                Path.Combine(baseDir, "letterhead.jpg"),
                Path.Combine(baseDir, "letterhead.jpeg"),
                Path.Combine(baseDir, "letterhead.png"),
                Path.Combine(baseDir, "Sample reports", "10 001.jpg.jpeg"),
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            return candidates[0];
        }

        public async Task<string> GenerateReportAsync(TestOrder order, bool includeLetterhead = true, CancellationToken cancellationToken = default)
        {
            var results = await _resultRepo.GetResultsForOrderAsync(order.OrderId, cancellationToken);
            string dateStr = DateTime.Today.ToString("yyyy-MM-dd");
            
            Document document = new Document();
            document.Info.Title = "Laboratory Diagnostic Report";
            document.Info.Subject = "Patient Diagnostic Results";
            document.Info.Author = "Quality Diagnostics Centre";

            Section section = document.AddSection();

            // Page Margins Setup
            var pageSetup = section.PageSetup;
            pageSetup.PageFormat = PageFormat.A4;
            pageSetup.LeftMargin = Unit.FromCentimeter(1.5);
            pageSetup.RightMargin = Unit.FromCentimeter(1.5);

            if (includeLetterhead && File.Exists(_letterheadPath))
            {
                pageSetup.TopMargin = Unit.FromCentimeter(4.5);
                pageSetup.BottomMargin = Unit.FromCentimeter(3.0);

                var bgImage = section.Headers.Primary.AddImage(_letterheadPath);
                bgImage.Width = pageSetup.PageWidth;
                bgImage.Height = pageSetup.PageHeight;
                bgImage.RelativeHorizontal = MigraDoc.DocumentObjectModel.Shapes.RelativeHorizontal.Page;
                bgImage.RelativeVertical = MigraDoc.DocumentObjectModel.Shapes.RelativeVertical.Page;
                bgImage.WrapFormat.Style = MigraDoc.DocumentObjectModel.Shapes.WrapStyle.Through;
            }
            else if (includeLetterhead)
            {
                pageSetup.TopMargin = Unit.FromCentimeter(1.5);
                pageSetup.BottomMargin = Unit.FromCentimeter(1.5);
            }
            else
            {
                pageSetup.TopMargin = Unit.FromCentimeter(1.5);
                pageSetup.BottomMargin = Unit.FromCentimeter(1.5);
                
                // Add simple text header if no letterhead image
                var headerPara = section.AddParagraph();
                headerPara.Format.Alignment = ParagraphAlignment.Center;
                var titleText = headerPara.AddFormattedText("QUALITY DIAGNOSTICS CENTRE\n", TextFormat.NotBold);
                titleText.Font.Name = "Times New Roman";
                titleText.Size = 24;
                titleText.Color = Colors.Black;
                
                var subtitleText = headerPara.AddFormattedText("MAIN ROAD , VANDE MART BACK SIDE\nBETHAMCHERLA 8639979746", TextFormat.NotBold);
                subtitleText.Font.Name = "Times New Roman";
                subtitleText.Size = 10;
                subtitleText.Color = Colors.Black;
                headerPara.Format.SpaceAfter = "1.5cm";
            }

            // Style configuration
            Style style = document.Styles["Normal"];
            style.Font.Name = "Arial";
            style.Font.Size = 9.5;

            // Patient Info Section Table
            var patientTable = section.AddTable();
            patientTable.Borders.Width = 0; // No borders
            patientTable.AddColumn("3.0cm"); // Label 1
            patientTable.AddColumn("0.5cm"); // Colon
            patientTable.AddColumn("6.5cm"); // Value 1
            patientTable.AddColumn("2.5cm"); // Label 2
            patientTable.AddColumn("0.5cm"); // Colon
            patientTable.AddColumn("5.0cm"); // Value 2

            string patientName = order.Patient?.FullName ?? "Unknown Patient";
            string gender = !string.IsNullOrWhiteSpace(order.Patient?.Gender) ? order.Patient.Gender : "Unknown";
            
            string ageStr = "";
            if (!string.IsNullOrEmpty(order.Patient?.DateOfBirth))
            {
                if (DateTime.TryParse(order.Patient.DateOfBirth, out DateTime dob))
                {
                    int age = DateTime.Today.Year - dob.Year;
                    if (dob > DateTime.Today.AddYears(-age)) age--;
                    ageStr = age.ToString();
                }
                else
                {
                    ageStr = order.Patient.DateOfBirth;
                }
            }

            string orderedDateStr = order.OrderedAt;
            if (DateTime.TryParse(order.OrderedAt, out DateTime orderedTime))
            {
                orderedDateStr = orderedTime.ToLocalTime().ToString("dd-MMM-yyyy hh:mm tt");
            }

            var pr1 = patientTable.AddRow();
            pr1.Height = "0.6cm";
            pr1.Cells[0].AddParagraph("Patient Name").Format.Font.Bold = true;
            pr1.Cells[1].AddParagraph(":").Format.Font.Bold = true;
            pr1.Cells[2].AddParagraph(patientName);
            pr1.Cells[3].AddParagraph("Report.No").Format.Font.Bold = true;
            pr1.Cells[4].AddParagraph(":").Format.Font.Bold = true;
            pr1.Cells[5].AddParagraph(order.OrderId.ToString());

            var pr2 = patientTable.AddRow();
            pr2.Height = "0.6cm";
            pr2.Cells[0].AddParagraph("Referred By").Format.Font.Bold = true;
            pr2.Cells[1].AddParagraph(":").Format.Font.Bold = true;
            pr2.Cells[2].AddParagraph(!string.IsNullOrWhiteSpace(order.ReferredBy) ? order.ReferredBy : "SELF");
            pr2.Cells[3].AddParagraph("Age/Gender").Format.Font.Bold = true;
            pr2.Cells[4].AddParagraph(":").Format.Font.Bold = true;
            pr2.Cells[5].AddParagraph($"{ageStr} /{gender}");

            var pr3 = patientTable.AddRow();
            pr3.Height = "0.6cm";
            pr3.Cells[0].AddParagraph("Collected On").Format.Font.Bold = true;
            pr3.Cells[1].AddParagraph(":").Format.Font.Bold = true;
            pr3.Cells[2].AddParagraph(orderedDateStr);
            pr3.Cells[3].AddParagraph("");
            pr3.Cells[4].AddParagraph(":");
            pr3.Cells[5].AddParagraph("");

            section.AddParagraph().Format.SpaceAfter = "0.5cm";

            // Group Results by GroupName
            var groupedResults = results
                .Where(r => r.TestType != null)
                .GroupBy(r => r.TestType.GroupName ?? "General")
                .OrderBy(g => g.Key)
                .ToList();

            // Render Results Table
            var table = section.AddTable();
            table.Borders.Width = 0; // No borders outside
            
            table.AddColumn("8.0cm"); // TEST DESCRIPTION
            table.AddColumn("2.5cm"); // RESULT
            table.AddColumn("2.5cm"); // UNITS
            table.AddColumn("5.0cm"); // NORMAL

            // Headers row
            var header = table.AddRow();
            header.HeadingFormat = true;
            header.Shading.Color = Colors.LightGray;
            header.Height = "0.6cm";
            header.VerticalAlignment = VerticalAlignment.Center;

            var cell0 = header.Cells[0].AddParagraph("TEST DESCRIPTION");
            cell0.Format.Font.Bold = true;
            cell0.Format.Font.Color = Colors.Black;

            var cell1 = header.Cells[1].AddParagraph("RESULT");
            cell1.Format.Font.Bold = true;
            cell1.Format.Font.Color = Colors.Black;

            var cell2 = header.Cells[2].AddParagraph("UNITS");
            cell2.Format.Font.Bold = true;
            cell2.Format.Font.Color = Colors.Black;

            var cell3 = header.Cells[3].AddParagraph("NORMAL");
            cell3.Format.Font.Bold = true;
            cell3.Format.Font.Color = Colors.Black;

            foreach (var group in groupedResults)
            {
                string groupName = group.Key;
                var sortedResults = group.OrderBy(r => r.TestType.SortOrder).ToList();
                string method = sortedResults.First().TestType.Method ?? "";
                string interpretation = sortedResults.First().TestType.Interpretation ?? "";

                // Group Header Row
                var groupRow = table.AddRow();
                groupRow.Height = "0.6cm";
                groupRow.Shading.Color = Colors.LightGray;
                groupRow.VerticalAlignment = VerticalAlignment.Center;
                var groupCell = groupRow.Cells[0];
                groupCell.MergeRight = 3;
                var groupPara = groupCell.AddParagraph($".{groupName.ToUpper()}");
                groupPara.Format.Font.Bold = true;
                groupPara.Format.Font.Color = Colors.Black;

                foreach (var r in sortedResults)
                {
                    var row = table.AddRow();
                    row.Height = "0.6cm";
                    row.VerticalAlignment = VerticalAlignment.Center;
                    
                    row.Cells[0].AddParagraph(r.TestType.Name.ToUpper());
                    
                    string displayVal = FormatResultValue(r.Value, r.TestType);
                    var valPara = row.Cells[1].AddParagraph();
                    var valText = valPara.AddFormattedText(displayVal);
                    
                    row.Cells[2].AddParagraph(r.TestType.Unit ?? "");
                    
                    string refRangeStr = FormatReferenceRange(r.TestType);
                    row.Cells[3].AddParagraph(refRangeStr);

                    if (r.IsAbnormal)
                    {
                        row.Shading.Color = Color.FromRgb(255, 235, 238); // Premium pink shading
                        valText.Bold = true;
                        valText.Color = Color.FromRgb(198, 40, 40); // Soft dark red for abnormal
                        
                        string flagStr = " High";
                        if (r.TestType.ReferenceRangeLow.HasValue && r.Value < r.TestType.ReferenceRangeLow.Value)
                        {
                            flagStr = " Low";
                        }
                        var flagText = valPara.AddFormattedText(flagStr, TextFormat.Bold);
                        flagText.Color = Color.FromRgb(198, 40, 40);
                        flagText.Size = 8.5;
                    }
                }

                // Method note underneath table
                if (!string.IsNullOrEmpty(method))
                {
                    var methodRow = table.AddRow();
                    methodRow.Height = "0.6cm";
                    var methodCell = methodRow.Cells[0];
                    methodCell.MergeRight = 3;
                    var methodPara = methodCell.AddParagraph($"(Method:- {method.ToUpper()})");
                    methodPara.Format.Font.Color = Colors.Black;
                }

                // Interpretation block
                if (!string.IsNullOrEmpty(interpretation))
                {
                    var intHeaderRow = table.AddRow();
                    intHeaderRow.Height = "0.5cm";
                    intHeaderRow.Shading.Color = Colors.LightGray;
                    var intHeaderCell = intHeaderRow.Cells[0];
                    intHeaderCell.MergeRight = 3;
                    var intHeaderPara = intHeaderCell.AddParagraph("INTERPRETATION");
                    intHeaderPara.Format.Alignment = ParagraphAlignment.Center;
                    intHeaderPara.Format.Font.Bold = true;
                    intHeaderPara.Format.Font.Color = Colors.Black;
                    intHeaderPara.Format.Font.Size = 8.5;
                    
                    var intRow = table.AddRow();
                    var intCell = intRow.Cells[0];
                    intCell.MergeRight = 3;
                    intRow.Borders.Width = 0.5;
                    intRow.Borders.Color = Colors.Black;

                    var intPara = intCell.AddParagraph(interpretation);
                    intPara.Format.Font.Size = 9.5;
                    intPara.Format.Font.Color = Colors.Black;
                    intPara.Format.SpaceBefore = "0.2cm";
                    intPara.Format.SpaceAfter = "0.2cm";

                    // Empty row for spacing
                    var spaceRow = table.AddRow();
                    spaceRow.Height = "0.3cm";
                    spaceRow.Borders.Width = 0;
                    spaceRow.Borders.Color = Colors.White;
                }
            }

            // End of Report Text
            var endPara = section.AddParagraph();
            endPara.Format.Alignment = ParagraphAlignment.Center;
            endPara.Format.SpaceBefore = "1.0cm";
            var endText = endPara.AddFormattedText("--- End of the Report ---", TextFormat.Bold);
            endText.Size = 9;
            endText.Color = Colors.Black;

            // Render Document
            PdfDocumentRenderer renderer = new PdfDocumentRenderer()
            {
                Document = document
            };
            renderer.RenderDocument();

            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", dateStr);
            Directory.CreateDirectory(dir);

            string safePatientName = patientName;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                safePatientName = safePatientName.Replace(c, '_');
            }
            safePatientName = safePatientName.Replace(' ', '_');

            string filepath = Path.Combine(dir, $"{safePatientName}_Order{order.OrderId}_{dateStr}_{Guid.NewGuid().ToString().Substring(0,4)}.pdf");
            renderer.PdfDocument.Save(filepath);
            return filepath;
        }

        private string FormatResultValue(double value, TestType testType)
        {
            if (testType.Unit == "Blood Group")
            {
                switch ((int)value)
                {
                    case 1: return "A Rh Positive";
                    case 2: return "A Rh Negative";
                    case 3: return "B Rh Positive";
                    case 4: return "B Rh Negative";
                    case 5: return "O Rh Positive";
                    case 6: return "O Rh Negative";
                    case 7: return "AB Rh Positive";
                    case 8: return "AB Rh Negative";
                    default: return "Unknown Group";
                }
            }
            if (testType.Name.Contains("Malarial Parasite") || testType.Name.Contains("PBS Malarial"))
            {
                return value >= 1.0 ? "Detected" : "Not Detected";
            }
            if (testType.Name.Contains("Rapid Malaria"))
            {
                return value >= 1.0 ? "Positive" : "Negative";
            }
            if (testType.Unit == "Qualitative" || testType.Name.Contains("Urine Sugar") || testType.Name.Contains("Urine Protein"))
            {
                return value >= 1.0 ? "Present" : "Absent";
            }
            if (testType.Unit == "Titer")
            {
                if (value <= 0) return "No Agglutination";
                return $"Agglutination (1:{(int)value})";
            }
            if (testType.Unit == "Index" || testType.Unit == "Ratio" || testType.Name.Contains("Antibody") || testType.Name.Contains("HBsAg") || testType.Name.Contains("HCV") || testType.Name.Contains("VDRL") || testType.Name.Contains("HIV"))
            {
                if (value == 0.0) return "Negative";
                return value.ToString("F2");
            }
            return value.ToString();
        }

        private string FormatReferenceRange(TestType tt)
        {
            if (tt == null) return "N/A";
            
            if (tt.Unit == "Blood Group")
            {
                return "A/B/O/AB Rh +/-";
            }
            if (tt.Unit == "Titer")
            {
                return "No Agglutination";
            }
            if (tt.Unit == "Qualitative" || tt.Name.Contains("Urine Sugar") || tt.Name.Contains("Urine Protein"))
            {
                return "Absent";
            }
            if (tt.Name.Contains("Malarial Parasite"))
            {
                return "Not Detected";
            }
            if (tt.Name.Contains("Rapid Malaria") || tt.Name.Contains("HBsAg") || tt.Name.Contains("HCV") || tt.Name.Contains("VDRL") || tt.Name.Contains("HIV"))
            {
                return "Negative";
            }
            
            if (tt.ReferenceRangeLow.HasValue && tt.ReferenceRangeHigh.HasValue)
            {
                return $"{tt.ReferenceRangeLow.Value} - {tt.ReferenceRangeHigh.Value}";
            }
            if (tt.ReferenceRangeLow.HasValue)
            {
                return $">= {tt.ReferenceRangeLow.Value}";
            }
            if (tt.ReferenceRangeHigh.HasValue)
            {
                return $"<= {tt.ReferenceRangeHigh.Value}";
            }
            return "N/A";
        }
    }
}
