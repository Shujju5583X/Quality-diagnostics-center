using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.Core.Services;
using LabSystem.Core.Enums;
using LabSystem.Core;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;

namespace LabSystem.Services
{
    public class PdfReportService : IPdfReportService
    {
        private readonly IResultRepository _resultRepo;
        private readonly IRepository<TestType> _testTypeRepo;
        private readonly IRepository<TestPanel> _testPanelRepo;
        private readonly string _letterheadPath;

        public PdfReportService(IResultRepository resultRepo, IRepository<TestType> testTypeRepo)
            : this(resultRepo, testTypeRepo, null, GetDefaultLetterheadPath())
        {
        }

        public PdfReportService(IResultRepository resultRepo, IRepository<TestType> testTypeRepo, string letterheadPath)
            : this(resultRepo, testTypeRepo, null, letterheadPath)
        {
        }

        public PdfReportService(IResultRepository resultRepo, IRepository<TestType> testTypeRepo, IRepository<TestPanel> testPanelRepo, string letterheadPath)
        {
            _resultRepo = resultRepo;
            _testTypeRepo = testTypeRepo;
            _testPanelRepo = testPanelRepo;
            _letterheadPath = letterheadPath;
        }

        private static string GetDefaultLetterheadPath()
        {
            var path = FileUtilities.FindFileUpwards("Sample reports", "10 001.jpg.jpeg");
            if (path != null && File.Exists(path))
                return path;

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new[]
            {
                Path.Combine(baseDir, "Assets", "letterhead.jpg"),
                Path.Combine(baseDir, "Assets", "letterhead.jpeg"),
                Path.Combine(baseDir, "Assets", "letterhead.png"),
                Path.Combine(baseDir, "letterhead.jpg"),
                Path.Combine(baseDir, "letterhead.jpeg"),
                Path.Combine(baseDir, "letterhead.png"),
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
            bool isAmendedReport = results.Any(r => r.IsAmended);
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
                pageSetup.TopMargin = Unit.FromCentimeter(5.0);
                pageSetup.BottomMargin = Unit.FromCentimeter(3.5);

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
                
                var headerPara = section.AddParagraph();
                headerPara.Format.Alignment = ParagraphAlignment.Center;
                
                var titleText = headerPara.AddFormattedText("QUALITY DIAGNOSTICS CENTRE\n", TextFormat.Bold);
                titleText.Font.Name = "Verdana";
                titleText.Size = 18;
                titleText.Color = Color.FromRgb(0, 115, 187);
                
                var subtitleText = headerPara.AddFormattedText("MAIN ROAD , VANDE MART BACK SIDE , BETHAMCHERLA  CALL : 86399 79746", TextFormat.Bold);
                subtitleText.Font.Name = "Verdana";
                subtitleText.Size = 9;
                subtitleText.Color = Colors.Black;
                headerPara.Format.SpaceAfter = "1.0cm";
            }

            if (isAmendedReport)
            {
                var amendedPara = section.AddParagraph();
                amendedPara.Format.Alignment = ParagraphAlignment.Center;
                amendedPara.Format.SpaceBefore = "0.5cm";
                amendedPara.Format.SpaceAfter = "0.5cm";
                var amendedText = amendedPara.AddFormattedText("*** AMENDED REPORT ***", TextFormat.Bold);
                amendedText.Font.Size = 16;
                amendedText.Color = Colors.Red;
            }

            // Style configuration
            Style style = document.Styles["Normal"];
            style.Font.Name = "Verdana";
            style.Font.Size = 9.5;

            // Patient Info Section Table
            var patientTable = section.AddTable();
            patientTable.Borders.Width = 0; // No borders
            patientTable.AddColumn("2.8cm"); // Label Left
            patientTable.AddColumn("0.4cm"); // Colon Left
            patientTable.AddColumn("7.0cm"); // Value Left
            patientTable.AddColumn("3.2cm"); // Label Right
            patientTable.AddColumn("0.4cm"); // Colon Right
            patientTable.AddColumn("4.2cm"); // Value Right

            string patientName = order.Patient?.FullName ?? "Unknown Patient";
            string gender = !string.IsNullOrWhiteSpace(order.Patient?.Gender) ? order.Patient.Gender : "Unknown";
            
            string ageStr = "";
            if (order.Patient?.DateOfBirth != null)
            {
                DateTime dob = order.Patient.DateOfBirth.Value;
                int age = DateTime.Today.Year - dob.Year;
                if (dob > DateTime.Today.AddYears(-age)) age--;
                ageStr = age.ToString();
            }

            string orderedDateStr = order.OrderedAt.ToLocalTime().ToString("dd-MM-yyyy");
            string reportingDateStr = (order.UpdatedAt == default(DateTime))
                ? DateTime.Today.ToString("dd-MM-yyyy")
                : order.UpdatedAt.ToLocalTime().ToString("dd-MM-yyyy");

            var pr1 = patientTable.AddRow();
            pr1.Height = "0.5cm";
            pr1.Cells[0].AddParagraph("Name").Format.Font.Bold = true;
            pr1.Cells[1].AddParagraph(":");
            pr1.Cells[2].AddParagraph(patientName);
            pr1.Cells[3].AddParagraph("Collection Date").Format.Font.Bold = true;
            pr1.Cells[4].AddParagraph(":");
            pr1.Cells[5].AddParagraph(orderedDateStr);

            var pr2 = patientTable.AddRow();
            pr2.Height = "0.5cm";
            pr2.Cells[0].AddParagraph("Age/Gender").Format.Font.Bold = true;
            pr2.Cells[1].AddParagraph(":");
            pr2.Cells[2].AddParagraph($"{ageStr} (Year's) {gender}");
            pr2.Cells[3].AddParagraph("Reporting Date").Format.Font.Bold = true;
            pr2.Cells[4].AddParagraph(":");
            pr2.Cells[5].AddParagraph(reportingDateStr);

            string referredBy = "SELF";
            if (!string.IsNullOrWhiteSpace(order.ReferredBy))
            {
                referredBy = order.ReferredBy;
            }
            var refText = referredBy.StartsWith("Dr.", StringComparison.OrdinalIgnoreCase) ? referredBy : "Dr. " + referredBy;
            refText += ",";

            var pr3 = patientTable.AddRow();
            pr3.Height = "0.5cm";
            pr3.Cells[0].AddParagraph("Ref By").Format.Font.Bold = true;
            pr3.Cells[1].AddParagraph(":");
            pr3.Cells[2].AddParagraph(refText);

            // Draw a thin blue separator line
            var separator = section.AddParagraph();
            separator.Format.Borders.Bottom.Width = 0.75;
            separator.Format.Borders.Bottom.Color = Color.FromRgb(0, 115, 187); // Nice medical blue
            separator.Format.SpaceBefore = "0.4cm";
            separator.Format.SpaceAfter = "0.4cm";

            // Specimen Details Section
            if (order.Specimens != null && order.Specimens.Any())
            {
                var specimenTable = section.AddTable();
                specimenTable.Borders.Width = 0.5;
                specimenTable.Borders.Color = Colors.LightGray;
                specimenTable.AddColumn("4.5cm");  // Barcode
                specimenTable.AddColumn("3.5cm");  // Sample Type
                specimenTable.AddColumn("5.0cm");  // Collection Time
                specimenTable.AddColumn("5.0cm");  // Status / Reason

                var specHeader = specimenTable.AddRow();
                specHeader.Height = "0.5cm";
                specHeader.VerticalAlignment = VerticalAlignment.Center;
                specHeader.Cells[0].AddParagraph("Specimen Barcode").Format.Font.Bold = true;
                specHeader.Cells[1].AddParagraph("Sample Type").Format.Font.Bold = true;
                specHeader.Cells[2].AddParagraph("Collection Time").Format.Font.Bold = true;
                specHeader.Cells[3].AddParagraph("Status").Format.Font.Bold = true;

                foreach (var spec in order.Specimens)
                {
                    var specRow = specimenTable.AddRow();
                    specRow.Height = "0.5cm";
                    specRow.VerticalAlignment = VerticalAlignment.Center;
                    specRow.Cells[0].AddParagraph(spec.Barcode ?? "");
                    specRow.Cells[1].AddParagraph(spec.SampleType ?? "");
                    specRow.Cells[2].AddParagraph(spec.CollectionTime.HasValue ? spec.CollectionTime.Value.ToLocalTime().ToString("dd-MMM-yyyy hh:mm tt") : "N/A");
                    
                    var statusCell = specRow.Cells[3];
                    if (spec.StatusEnum == SpecimenStatus.Rejected)
                    {
                        var statusPara = statusCell.AddParagraph();
                        var statusText = statusPara.AddFormattedText("REJECTED", TextFormat.Bold);
                        statusText.Color = Colors.Red;
                        if (!string.IsNullOrWhiteSpace(spec.RejectionReason))
                        {
                            statusPara.AddFormattedText($" ({spec.RejectionReason})");
                        }
                    }
                    else
                    {
                        statusCell.AddParagraph(spec.Status ?? "");
                    }
                }

                section.AddParagraph().Format.SpaceAfter = "0.4cm";
            }

            // Group Results by Category first
            var categoryGroups = results
                .Where(r => r.TestType != null)
                .GroupBy(r => r.TestType.Category ?? "GENERAL")
                .OrderBy(c => c.Key)
                .ToList();

            // Render Results Table
            var table = section.AddTable();
            table.Borders.Width = 0; // No borders outside
            
            table.AddColumn("6.5cm"); // Parameter
            table.AddColumn("0.5cm"); // Colon
            table.AddColumn("5.5cm"); // Result Values (value + unit)
            table.AddColumn("5.5cm"); // Normal Values

            // Headers row
            var header = table.AddRow();
            header.HeadingFormat = true;
            header.Height = "0.6cm";
            header.VerticalAlignment = VerticalAlignment.Center;

            var cell0 = header.Cells[0].AddParagraph("Parameter");
            cell0.Format.Font.Bold = true;
            cell0.Format.Font.Underline = Underline.Single;
            cell0.Format.Font.Size = 10.0;

            header.Cells[1].AddParagraph(""); // Empty for colon

            var cell2 = header.Cells[2].AddParagraph("Result Values");
            cell2.Format.Font.Bold = true;
            cell2.Format.Font.Underline = Underline.Single;
            cell2.Format.Font.Size = 10.0;

            var cell3 = header.Cells[3].AddParagraph("Normal Values");
            cell3.Format.Font.Bold = true;
            cell3.Format.Font.Underline = Underline.Single;
            cell3.Format.Font.Size = 10.0;

            foreach (var categoryGroup in categoryGroups)
            {
                string categoryName = categoryGroup.Key;

                // Adjust spelling of "BIOCHEMISTRY" to "BIO-CHEMISRTY" if it matches
                if (string.Equals(categoryName, "BIOCHEMISTRY", StringComparison.OrdinalIgnoreCase))
                {
                    categoryName = "BIO-CHEMISTRY";
                }

                // Category Header Row
                var catRow = table.AddRow();
                catRow.Height = "0.8cm";
                catRow.VerticalAlignment = VerticalAlignment.Center;
                var catCell = catRow.Cells[0];
                catCell.MergeRight = 3;
                var catPara = catCell.AddParagraph(categoryName.ToUpper());
                catPara.Format.Alignment = ParagraphAlignment.Center;
                catPara.Format.Font.Bold = true;
                catPara.Format.Font.Underline = Underline.Single;
                catPara.Format.Font.Size = 15.0;
                catPara.Format.SpaceBefore = "0.4cm";
                catPara.Format.SpaceAfter = "0.3cm";

                var groupedResults = categoryGroup
                    .GroupBy(r => r.TestType.GroupName ?? "General")
                    .OrderBy(g => g.Key)
                    .ToList();

                foreach (var group in groupedResults)
                {
                    string groupName = group.Key;
                    var sortedResults = group.OrderBy(r => r.TestType.SortOrder).ToList();
                    string method = sortedResults.First().TestType.Method ?? "";
                    string interpretation = sortedResults.First().TestType.Interpretation ?? "";

                    bool shouldPrintGroupHeader = sortedResults.Count > 1 ||
                                                  groupName.Contains("Profile") ||
                                                  groupName.Contains("Panel") ||
                                                  groupName.Contains("Electrolytes") ||
                                                  groupName.Contains("Widal") ||
                                                  groupName.Contains("Routine");

                    if (shouldPrintGroupHeader)
                    {
                        var groupRow = table.AddRow();
                        groupRow.Height = "0.6cm";
                        groupRow.VerticalAlignment = VerticalAlignment.Center;
                        var groupCell = groupRow.Cells[0];
                        groupCell.MergeRight = 3;
                        var groupPara = groupCell.AddParagraph(groupName.ToUpper());
                        groupPara.Format.Font.Bold = true;
                        groupPara.Format.Font.Underline = Underline.Single;
                        groupPara.Format.Font.Size = 10.0;
                    }

                    foreach (var r in sortedResults)
                    {
                        var row = table.AddRow();
                        row.Height = "0.6cm";
                        row.VerticalAlignment = VerticalAlignment.Center;

                        // Clean up test name
                        string testName = r.TestType.Name;
                        if (testName.EndsWith(" (KFT)", StringComparison.OrdinalIgnoreCase))
                            testName = testName.Substring(0, testName.Length - 6);
                        if (testName.EndsWith(" (LFT)", StringComparison.OrdinalIgnoreCase))
                            testName = testName.Substring(0, testName.Length - 6);
                        if (testName.EndsWith(" (TFT)", StringComparison.OrdinalIgnoreCase))
                            testName = testName.Substring(0, testName.Length - 6);

                        if (string.Equals(testName, "Cholesterol, Total", StringComparison.OrdinalIgnoreCase))
                            testName = "Total Cholesterol";
                        else if (string.Equals(testName, "Calcium, Total", StringComparison.OrdinalIgnoreCase))
                            testName = "Total Calcium";
                        else if (string.Equals(testName, "Bilirubin Total", StringComparison.OrdinalIgnoreCase))
                            testName = "Total Bilirubin";
                        else if (string.Equals(testName, "Glucose, Fasting (Plasma)", StringComparison.OrdinalIgnoreCase))
                            testName = "Glucose Fasting";
                        else if (string.Equals(testName, "Glucose, Post Prandial (Plasma)", StringComparison.OrdinalIgnoreCase))
                            testName = "Glucose Post Prandial";
                        else if (string.Equals(testName, "Glucose, Random (Plasma)", StringComparison.OrdinalIgnoreCase))
                            testName = "Glucose Random";

                        var namePara = row.Cells[0].AddParagraph(testName.ToUpper());
                        if (shouldPrintGroupHeader)
                        {
                            namePara.Format.Font.Bold = true;
                        }

                        var colonPara = row.Cells[1].AddParagraph(":");
                        if (shouldPrintGroupHeader)
                        {
                            colonPara.Format.Font.Bold = true;
                        }

                        string displayVal = !string.IsNullOrEmpty(r.ValueText) 
                            ? r.ValueText 
                            : FormatResultValue(r.Value, r.TestType);
                        string unitStr = r.TestType.Unit ?? "";
                        string valTextStr = string.IsNullOrEmpty(unitStr) ? displayVal : $"{displayVal} {unitStr}";

                        var valPara = row.Cells[2].AddParagraph();
                        var valText = valPara.AddFormattedText(valTextStr);

                        if (shouldPrintGroupHeader)
                        {
                            valText.Bold = true;
                        }

                        if (r.IsAbnormal)
                        {
                            valText.Bold = true;
                            valText.Color = Color.FromRgb(198, 40, 40); // Soft dark red for abnormal
                            
                            string flagStr = IsValueLow(r.Value, r.TestType, order.Patient) ? " Low" : " High";
                            var flagText = valPara.AddFormattedText(flagStr, TextFormat.Bold);
                            flagText.Color = Color.FromRgb(198, 40, 40);
                            flagText.Size = 8.5;
                        }

                        string refRangeStr = FormatReferenceRange(r.TestType, order.Patient);
                        var refPara = row.Cells[3].AddParagraph(refRangeStr);
                        if (shouldPrintGroupHeader)
                        {
                            refPara.Format.Font.Bold = true;
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
                        methodPara.Format.Font.Size = 8.5;
                        methodPara.Format.Font.Italic = true;
                        methodPara.Format.Font.Color = Colors.DarkGray;
                    }

                    // Interpretation block
                    if (!string.IsNullOrEmpty(interpretation))
                    {
                        var intHeaderRow = table.AddRow();
                        intHeaderRow.Height = "0.5cm";
                        var intHeaderCell = intHeaderRow.Cells[0];
                        intHeaderCell.MergeRight = 3;
                        var intHeaderPara = intHeaderCell.AddParagraph("INTERPRETATION");
                        intHeaderPara.Format.Alignment = ParagraphAlignment.Left;
                        intHeaderPara.Format.Font.Bold = true;
                        intHeaderPara.Format.Font.Underline = Underline.Single;
                        intHeaderPara.Format.Font.Size = 9.0;
                        
                        var intRow = table.AddRow();
                        var intCell = intRow.Cells[0];
                        intCell.MergeRight = 3;

                        var intPara = intCell.AddParagraph(interpretation);
                        intPara.Format.Font.Size = 9.0;
                        intPara.Format.Font.Color = Colors.Black;
                        intPara.Format.SpaceBefore = "0.1cm";
                        intPara.Format.SpaceAfter = "0.1cm";

                        // Empty row for spacing
                        var spaceRow = table.AddRow();
                        spaceRow.Height = "0.3cm";
                    }
                }
            }

            if (isAmendedReport)
            {
                var amendments = results.Where(r => r.IsAmended).ToList();
                foreach (var r in amendments)
                {
                    var reasonPara = section.AddParagraph($"* Amendment for {r.TestType.Name}: {r.AmendmentReason}");
                    reasonPara.Format.Font.Size = 8;
                    reasonPara.Format.Font.Italic = true;
                    reasonPara.Format.Font.Color = Colors.Red;
                }
                section.AddParagraph().Format.SpaceAfter = "0.5cm";
            }

            // Authorised Signatory Text
            var sigPara = section.AddParagraph();
            sigPara.Format.Alignment = ParagraphAlignment.Right;
            sigPara.Format.SpaceBefore = "1.5cm";
            sigPara.Format.RightIndent = "1.0cm";
            var sigText = sigPara.AddFormattedText("AUTHORISED SIGNATORY", TextFormat.Bold);
            sigText.Size = 9.0;
            sigText.Color = Colors.Black;

            // Render Document
            var renderer = new PdfDocumentRenderer()
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

        private string FormatResultValue(double? value, TestType testType)
        {
            if (value == null)
            {
                return "Sample Rejected";
            }
            double val = value.Value;
            if (testType.Unit == "Blood Group")
            {
                switch ((int)val)
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
                return val >= 1.0 ? "Detected" : "Not Detected";
            }
            if (testType.Name.Contains("Rapid Malaria"))
            {
                return val >= 1.0 ? "Positive" : "Negative";
            }
            if (testType.Unit == "Qualitative" || testType.Name.Contains("Urine Sugar") || testType.Name.Contains("Urine Protein"))
            {
                return val >= 1.0 ? "Present" : "Absent";
            }
            if (testType.Unit == "Titer")
            {
                if (val <= 0) return "No Agglutination";
                return $"Agglutination (1:{(int)val})";
            }
            if (testType.Unit == "Index" || testType.Unit == "Ratio" || testType.Name.Contains("Antibody") || testType.Name.Contains("HBsAg") || testType.Name.Contains("HCV") || testType.Name.Contains("VDRL") || testType.Name.Contains("HIV"))
            {
                if (val == 0.0) return "Negative";
                return val.ToString("F2");
            }
            return val.ToString();
        }

        private bool IsValueLow(double? value, TestType tt, Patient patient)
        {
            return ReferenceRangeEvaluator.IsLow(value, tt, patient);
        }

        private string FormatReferenceRange(TestType tt, Patient patient)
        {
            return ReferenceRangeEvaluator.FormatRange(tt, patient);
        }

        public async Task<string> GenerateInvoicePdfAsync(Invoice invoice, CancellationToken cancellationToken = default)
        {
            string dateStr = DateTime.Today.ToString("yyyy-MM-dd");
            
            Document document = new Document();
            document.Info.Title = "Invoice / Bill";
            document.Info.Subject = "Patient Invoice";
            document.Info.Author = "Quality Diagnostics Centre";

            Section section = document.AddSection();

            var pageSetup = section.PageSetup;
            pageSetup.PageFormat = PageFormat.A4;
            pageSetup.LeftMargin = Unit.FromCentimeter(2.0);
            pageSetup.RightMargin = Unit.FromCentimeter(2.0);
            pageSetup.TopMargin = Unit.FromCentimeter(2.0);
            pageSetup.BottomMargin = Unit.FromCentimeter(2.0);

            // Add Header
            var headerPara = section.AddParagraph();
            headerPara.Format.Alignment = ParagraphAlignment.Center;
            var titleText = headerPara.AddFormattedText("QUALITY DIAGNOSTICS CENTRE\n", TextFormat.Bold);
            titleText.Size = 20;
            
            var subtitleText = headerPara.AddFormattedText("MAIN ROAD , VANDE MART BACK SIDE\nBETHAMCHERLA 8639979746\n\n", TextFormat.NotBold);
            subtitleText.Size = 10;

            var invoiceTitle = headerPara.AddFormattedText("INVOICE / BILL", TextFormat.Bold);
            invoiceTitle.Size = 16;
            headerPara.Format.SpaceAfter = "1.0cm";

            // Patient & Order Info
            var infoTable = section.AddTable();
            infoTable.Borders.Width = 0;
            infoTable.AddColumn("3.5cm");
            infoTable.AddColumn("0.5cm");
            infoTable.AddColumn("7.0cm");
            infoTable.AddColumn("2.5cm");
            infoTable.AddColumn("0.5cm");
            infoTable.AddColumn("3.0cm");

            string patientName = invoice.Order?.Patient?.FullName ?? "Unknown Patient";
            string invoiceId = invoice.InvoiceId.ToString();
            string orderId = invoice.OrderId.ToString();
            string createdDate = invoice.CreatedAt.ToLocalTime().ToString("dd-MMM-yyyy hh:mm tt");

            var row1 = infoTable.AddRow();
            row1.Cells[0].AddParagraph("Patient Name").Format.Font.Bold = true;
            row1.Cells[1].AddParagraph(":");
            row1.Cells[2].AddParagraph(patientName);
            row1.Cells[3].AddParagraph("UHID").Format.Font.Bold = true;
            row1.Cells[4].AddParagraph(":");
            row1.Cells[5].AddParagraph(invoice.Order?.Patient?.Uhid ?? "");

            string invoiceReferredBy = "SELF";
            if (!string.IsNullOrWhiteSpace(invoice.Order?.ReferredBy))
            {
                invoiceReferredBy = invoice.Order.ReferredBy;
            }

            var row2 = infoTable.AddRow();
            row2.Cells[0].AddParagraph("Referred By").Format.Font.Bold = true;
            row2.Cells[1].AddParagraph(":");
            row2.Cells[2].AddParagraph(invoiceReferredBy);
            row2.Cells[3].AddParagraph("Invoice No").Format.Font.Bold = true;
            row2.Cells[4].AddParagraph(":");
            row2.Cells[5].AddParagraph(invoiceId);

            var row3 = infoTable.AddRow();
            row3.Cells[0].AddParagraph("Date").Format.Font.Bold = true;
            row3.Cells[1].AddParagraph(":");
            row3.Cells[2].AddParagraph(createdDate);
            row3.Cells[3].AddParagraph("Order ID").Format.Font.Bold = true;
            row3.Cells[4].AddParagraph(":");
            row3.Cells[5].AddParagraph(orderId);
            
            section.AddParagraph().Format.SpaceAfter = "1.0cm";

            // Items Table
            var itemTable = section.AddTable();
            itemTable.Borders.Width = 0.5;
            itemTable.Borders.Color = Colors.LightGray;
            itemTable.AddColumn("2.0cm"); // S.No
            itemTable.AddColumn("10.0cm"); // Particulars
            itemTable.AddColumn("5.0cm"); // Amount

            var header = itemTable.AddRow();
            header.HeadingFormat = true;
            header.Shading.Color = Colors.LightGray;
            header.Height = "0.7cm";
            header.VerticalAlignment = VerticalAlignment.Center;
            header.Cells[0].AddParagraph("S.No").Format.Font.Bold = true;
            header.Cells[1].AddParagraph("Particulars (Test Name)").Format.Font.Bold = true;
            header.Cells[2].AddParagraph("Amount (Rs.)").Format.Font.Bold = true;

            int sno = 1;
            decimal calculatedTotal = 0;

            if (invoice.Order != null && invoice.Order.TestTypes != null && invoice.Order.TestTypes.Any())
            {
                var tests = invoice.Order.TestTypes.ToList();
                var testTypesAppliedToPanels = new HashSet<int>();

                if (_testPanelRepo != null)
                {
                    var panels = await _testPanelRepo.GetAllAsync(cancellationToken);

                    var orderedTestTypeIds = new HashSet<int>(tests.Select(t => t.TypeId));

                    foreach (var panel in panels.OrderByDescending(p => p.TestTypes.Count))
                    {
                        var panelTestTypeIds = panel.TestTypes.Select(t => t.TypeId).ToList();
                        if (panelTestTypeIds.Count > 0 && panelTestTypeIds.All(id => orderedTestTypeIds.Contains(id) && !testTypesAppliedToPanels.Contains(id)))
                        {
                            var row = itemTable.AddRow();
                            row.Height = "0.6cm";
                            row.VerticalAlignment = VerticalAlignment.Center;
                            row.Cells[0].AddParagraph(sno.ToString());
                            row.Cells[1].AddParagraph($"{panel.Name} (Package)");
                            row.Cells[2].AddParagraph(panel.Price.ToString("F2"));
                            
                            calculatedTotal += panel.Price;
                            sno++;

                            foreach (var id in panelTestTypeIds)
                            {
                                testTypesAppliedToPanels.Add(id);
                            }
                        }
                    }
                }

                foreach (var test in tests)
                {
                    if (!testTypesAppliedToPanels.Contains(test.TypeId))
                    {
                        var row = itemTable.AddRow();
                        row.Height = "0.6cm";
                        row.VerticalAlignment = VerticalAlignment.Center;
                        row.Cells[0].AddParagraph(sno.ToString());
                        row.Cells[1].AddParagraph(test.Name);
                        row.Cells[2].AddParagraph(test.Price.ToString("F2"));
                        
                        calculatedTotal += test.Price;
                        sno++;
                    }
                }
            }

            // Total Row
            var totalRow = itemTable.AddRow();
            totalRow.Height = "0.8cm";
            totalRow.VerticalAlignment = VerticalAlignment.Center;
            totalRow.Cells[0].MergeRight = 1;
            var totalLbl = totalRow.Cells[0].AddParagraph("SUBTOTAL");
            totalLbl.Format.Font.Bold = true;
            totalLbl.Format.Alignment = ParagraphAlignment.Right;
            totalRow.Cells[0].Format.RightIndent = "0.5cm";
            
            var totalVal = totalRow.Cells[2].AddParagraph(invoice.TotalAmount.ToString("F2"));
            totalVal.Format.Font.Bold = true;

            if (invoice.DiscountAmount > 0)
            {
                var discountRow = itemTable.AddRow();
                discountRow.Height = "0.6cm";
                discountRow.VerticalAlignment = VerticalAlignment.Center;
                discountRow.Cells[0].MergeRight = 1;
                var discountLbl = discountRow.Cells[0].AddParagraph("DISCOUNT");
                discountLbl.Format.Alignment = ParagraphAlignment.Right;
                discountRow.Cells[0].Format.RightIndent = "0.5cm";
                discountRow.Cells[2].AddParagraph("-" + invoice.DiscountAmount.ToString("F2"));
            }

            if (invoice.TaxAmount > 0)
            {
                var taxRow = itemTable.AddRow();
                taxRow.Height = "0.6cm";
                taxRow.VerticalAlignment = VerticalAlignment.Center;
                taxRow.Cells[0].MergeRight = 1;
                var taxLbl = taxRow.Cells[0].AddParagraph("TAX");
                taxLbl.Format.Alignment = ParagraphAlignment.Right;
                taxRow.Cells[0].Format.RightIndent = "0.5cm";
                taxRow.Cells[2].AddParagraph(invoice.TaxAmount.ToString("F2"));
            }

            decimal grandTotal = invoice.TotalAmount - invoice.DiscountAmount + invoice.TaxAmount;
            decimal paidAmount = 0;
            if (invoice.Payments != null) {
                paidAmount = invoice.Payments.Sum(p => p.Amount);
            }
            decimal balance = Math.Max(0, grandTotal - paidAmount);

            var grandTotalRow = itemTable.AddRow();
            grandTotalRow.Height = "0.8cm";
            grandTotalRow.VerticalAlignment = VerticalAlignment.Center;
            grandTotalRow.Shading.Color = Colors.LightGray;
            grandTotalRow.Cells[0].MergeRight = 1;
            var grandTotalLbl = grandTotalRow.Cells[0].AddParagraph("GRAND TOTAL");
            grandTotalLbl.Format.Font.Bold = true;
            grandTotalLbl.Format.Alignment = ParagraphAlignment.Right;
            grandTotalRow.Cells[0].Format.RightIndent = "0.5cm";
            
            var grandTotalVal = grandTotalRow.Cells[2].AddParagraph(grandTotal.ToString("F2"));
            grandTotalVal.Format.Font.Bold = true;

            var paidRow = itemTable.AddRow();
            paidRow.Height = "0.6cm";
            paidRow.VerticalAlignment = VerticalAlignment.Center;
            paidRow.Cells[0].MergeRight = 1;
            var paidLbl = paidRow.Cells[0].AddParagraph("PAID AMOUNT");
            paidLbl.Format.Alignment = ParagraphAlignment.Right;
            paidRow.Cells[0].Format.RightIndent = "0.5cm";
            paidRow.Cells[2].AddParagraph(paidAmount.ToString("F2"));

            var balanceRow = itemTable.AddRow();
            balanceRow.Height = "0.6cm";
            balanceRow.VerticalAlignment = VerticalAlignment.Center;
            balanceRow.Cells[0].MergeRight = 1;
            var balanceLbl = balanceRow.Cells[0].AddParagraph("BALANCE DUE");
            balanceLbl.Format.Font.Bold = true;
            balanceLbl.Format.Alignment = ParagraphAlignment.Right;
            balanceRow.Cells[0].Format.RightIndent = "0.5cm";
            var balanceVal = balanceRow.Cells[2].AddParagraph(balance.ToString("F2"));
            balanceVal.Format.Font.Bold = true;
            if (balance > 0) balanceVal.Format.Font.Color = Colors.Red;

            section.AddParagraph().Format.SpaceAfter = "1.0cm";

            // Payment Status
            var statusPara = section.AddParagraph();
            statusPara.Format.Font.Size = 12;
            if (invoice.IsPaid)
            {
                statusPara.AddFormattedText($"Payment Status: PAID ({invoice.PaymentMethod})", TextFormat.Bold).Color = Colors.DarkGreen;
            }
            else
            {
                statusPara.AddFormattedText("Payment Status: PENDING", TextFormat.Bold).Color = Colors.DarkRed;
            }

            // Render Document
            var renderer = new PdfDocumentRenderer()
            {
                Document = document
            };
            renderer.RenderDocument();

            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Invoices", dateStr);
            Directory.CreateDirectory(dir);

            string safePatientName = patientName;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                safePatientName = safePatientName.Replace(c, '_');
            }
            safePatientName = safePatientName.Replace(' ', '_');

            string filepath = Path.Combine(dir, $"Invoice_{invoiceId}_{safePatientName}_{dateStr}_{Guid.NewGuid().ToString().Substring(0,4)}.pdf");
            renderer.PdfDocument.Save(filepath);
            return filepath;
        }
    }
}
