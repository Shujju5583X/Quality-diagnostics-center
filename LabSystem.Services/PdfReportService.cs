using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.Core.Services;
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
        private readonly IRepository<Setting> _settingRepo;
        private readonly ITestOrderRepository _orderRepo;
        private string _letterheadPath;
        private string _labName;
        private string _labAddress;
        private string _labPhone;
        private bool _settingsLoaded;

        public PdfReportService(
            IResultRepository resultRepo, 
            IRepository<TestType> testTypeRepo, 
            IRepository<TestPanel> testPanelRepo, 
            IRepository<Setting> settingRepo,
            ITestOrderRepository orderRepo)
        {
            _resultRepo = resultRepo;
            _testTypeRepo = testTypeRepo;
            _testPanelRepo = testPanelRepo;
            _settingRepo = settingRepo;
            _orderRepo = orderRepo;

            _labName = "QUALITY DIAGNOSTICS CENTRE";
            _labAddress = "MAIN ROAD, VANDE MART BACK SIDE, BETHAMCHERLA";
            _labPhone = "86399 79746";
            _letterheadPath = GetDefaultLetterheadPath();
        }

        private void EnsureSettingsLoaded()
        {
            if (_settingsLoaded || _settingRepo == null) return;
            _settingsLoaded = true;
            try
            {
                var allSettings = _settingRepo.GetAllAsync(default(CancellationToken)).GetAwaiter().GetResult();
                _labName = GetSettingValue(allSettings, "operator_name", _labName);
                _labAddress = GetSettingValue(allSettings, "operator_address", _labAddress);
                _labPhone = GetSettingValue(allSettings, "operator_phone", _labPhone);
                var dbPath = GetSettingValue(allSettings, "letterhead_path", "");
                if (!string.IsNullOrWhiteSpace(dbPath) && File.Exists(dbPath))
                    _letterheadPath = dbPath;
            }
            catch
            {
                // Defaults already set in constructor
            }
        }

        private static string GetSettingValue(IEnumerable<Setting> settings, string key, string defaultValue)
        {
            var setting = settings.FirstOrDefault(s => s.Key == key);
            return (setting != null && !string.IsNullOrWhiteSpace(setting.Value)) ? setting.Value : defaultValue;
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
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            var path = FileUtilities.FindFileUpwards("Sample reports", "10 001.jpg.jpeg");
            if (path != null && File.Exists(path))
                return path;

            return candidates[0];
        }

        public async Task<string> GenerateReportAsync(TestOrder order, bool includeLetterhead = true, CancellationToken cancellationToken = default(CancellationToken))
        {
            EnsureSettingsLoaded();
            var results = await _resultRepo.GetResultsForOrderAsync(order.OrderId, cancellationToken);
            string dateStr = DateTime.Today.ToString("yyyy-MM-dd");
            
            Document document = new Document();
            document.Styles["Normal"].Font.Name = "Arial";
            document.Info.Title = "Laboratory Diagnostic Report";
            document.Info.Subject = "Patient Diagnostic Results";
            document.Info.Author = "Quality Diagnostics Centre";

            Section section = document.AddSection();

            // Page Margins Setup
            var pageSetup = section.PageSetup;
            pageSetup.PageFormat = PageFormat.A4;
            pageSetup.LeftMargin = Unit.FromCentimeter(1.5);
            pageSetup.RightMargin = Unit.FromCentimeter(1.5);

            if (includeLetterhead)
            {
                if (File.Exists(_letterheadPath))
                {
                    pageSetup.TopMargin = Unit.FromCentimeter(8.0);
                    pageSetup.BottomMargin = Unit.FromCentimeter(3.5);

                    var bgImage = section.Headers.Primary.AddImage(_letterheadPath);
                    bgImage.Width = pageSetup.PageWidth;
                    bgImage.Height = pageSetup.PageHeight;
                    bgImage.RelativeHorizontal = MigraDoc.DocumentObjectModel.Shapes.RelativeHorizontal.Page;
                    bgImage.RelativeVertical = MigraDoc.DocumentObjectModel.Shapes.RelativeVertical.Page;
                    bgImage.WrapFormat.Style = MigraDoc.DocumentObjectModel.Shapes.WrapStyle.Through;

                    var spacer = section.Headers.Primary.AddParagraph();
                    spacer.Format.SpaceBefore = "3.5cm";
                }
                else
                {
                    pageSetup.TopMargin = Unit.FromCentimeter(5.5);
                    pageSetup.BottomMargin = Unit.FromCentimeter(1.5);
                    
                    var headerPara = section.Headers.Primary.AddParagraph();
                    headerPara.Format.Alignment = ParagraphAlignment.Center;
                    
                    var titleText = headerPara.AddFormattedText(_labName + "\n", TextFormat.Bold);
                    titleText.Font.Name = "Verdana";
                    titleText.Size = 18;
                    titleText.Color = Color.FromRgb(0, 115, 187);
                    
                    var subtitleText = headerPara.AddFormattedText(_labAddress + "  CALL : " + _labPhone, TextFormat.Bold);
                    subtitleText.Font.Name = "Verdana";
                    subtitleText.Size = 9;
                    subtitleText.Color = Colors.Black;
                    headerPara.Format.SpaceAfter = "0.5cm";
                }
            }
            else
            {
                // Without Letterhead: leave space for pre-printed letterhead paper
                pageSetup.TopMargin = Unit.FromCentimeter(8.0);
                pageSetup.BottomMargin = Unit.FromCentimeter(3.5);

                var spacer = section.Headers.Primary.AddParagraph();
                spacer.Format.SpaceBefore = "3.5cm";
            }



            // Style configuration
            Style style = document.Styles["Normal"];
            style.Font.Name = "Verdana";
            style.Font.Size = 9.5;

            // Patient Info Section Table
            var patientTable = section.Headers.Primary.AddTable();
            patientTable.Borders.Width = 0; // No borders
            patientTable.AddColumn("2.4cm"); // Label Left
            patientTable.AddColumn("0.2cm"); // Colon Left
            patientTable.AddColumn("7.5cm"); // Value Left
            patientTable.AddColumn("3.3cm"); // Label Right
            patientTable.AddColumn("0.2cm"); // Colon Right
            patientTable.AddColumn("4.4cm"); // Value Right

            string titlePrefix = "";
            if (order.Patient != null && !string.IsNullOrEmpty(order.Patient.Title) && order.Patient.Title != "Select Title")
            {
                titlePrefix = order.Patient.Title + " ";
            }
            string patientName = titlePrefix + (order.Patient != null ? order.Patient.FullName ?? "Unknown Patient" : "Unknown Patient");
            string gender = (order.Patient != null && !string.IsNullOrWhiteSpace(order.Patient.Gender)) ? order.Patient.Gender : "Unknown";
            
            string ageStr = order.Patient != null ? order.Patient.GetFormattedAgeForReport() : "";

            string orderedDateStr = order.OrderedAt.ToString("dd-MM-yyyy");
            string reportingDateStr = order.UpdatedAt.ToString("dd-MM-yyyy hh:mm tt");

            var pr1 = patientTable.AddRow();
            pr1.Height = "0.6cm";
            pr1.Cells[0].AddParagraph("Name").Format.Font.Bold = true;
            pr1.Cells[1].AddParagraph(":");
            pr1.Cells[2].AddParagraph(patientName);
            pr1.Cells[3].AddParagraph("Collection Date").Format.Font.Bold = true;
            pr1.Cells[4].AddParagraph(":");
            pr1.Cells[5].AddParagraph(orderedDateStr);

            var pr2 = patientTable.AddRow();
            pr2.Height = "0.6cm";
            pr2.Cells[0].AddParagraph("Age/Gender").Format.Font.Bold = true;
            pr2.Cells[1].AddParagraph(":");
            pr2.Cells[2].AddParagraph(ageStr + " " + gender);
            pr2.Cells[3].AddParagraph("Reporting Date").Format.Font.Bold = true;
            pr2.Cells[4].AddParagraph(":");
            pr2.Cells[5].AddParagraph(reportingDateStr);

            string referredBy = "SELF";
            if (!string.IsNullOrWhiteSpace(order.ReferredBy))
            {
                referredBy = order.ReferredBy;
            }
            var refText = referredBy.StartsWith("Dr.", StringComparison.OrdinalIgnoreCase) ? referredBy : "Dr. " + referredBy;

            var pr3 = patientTable.AddRow();
            pr3.Height = "0.6cm";
            pr3.Cells[0].AddParagraph("Ref By").Format.Font.Bold = true;
            pr3.Cells[1].AddParagraph(":");
            
            pr3.Cells[2].MergeRight = 3;
            var docRefPara = pr3.Cells[2].AddParagraph(refText);
            docRefPara.Format.LineSpacingRule = LineSpacingRule.Multiple;
            docRefPara.Format.LineSpacing = 1.15;
            
            int seq = order.OrderId;
            if (_orderRepo != null)
            {
                seq = await _orderRepo.GetDailySequenceNumberAsync(order.OrderId, order.OrderedAt, cancellationToken);
            }
            string qdcId = string.Format("{0:yy}{0:dd}{1:D2}", order.OrderedAt, seq);
            var pr4 = patientTable.AddRow();
            pr4.Height = "0.6cm";
            pr4.Cells[0].AddParagraph("QDC ID").Format.Font.Bold = true;
            pr4.Cells[1].AddParagraph(":");
            pr4.Cells[2].AddParagraph(qdcId);

            // Draw a thin blue separator line
            var separator = section.Headers.Primary.AddParagraph();
            separator.Format.Borders.Bottom.Width = 0.75;
            separator.Format.Borders.Bottom.Color = Color.FromRgb(0, 115, 187); // Nice medical blue
            separator.Format.SpaceBefore = "0.1cm";
            separator.Format.SpaceAfter = "0.2cm";

            // Group Results by Category first
            var categoryGroups = results
                .Where(r => r.TestType != null)
                .GroupBy(r => r.TestType.Category ?? "GENERAL")
                .OrderBy(c => c.Key)
                .ToList();

            bool isFirstCategory = true;
            foreach (var categoryGroup in categoryGroups)
            {
                string categoryName = categoryGroup.Key;

                // Adjust spelling of "BIOCHEMISTRY" to "BIO-CHEMISRTY" if it matches
                if (string.Equals(categoryName, "BIOCHEMISTRY", StringComparison.OrdinalIgnoreCase))
                {
                    categoryName = "BIO-CHEMISTRY";
                }

                // Category Title (Main Heading)
                var catPara = section.AddParagraph(categoryName.ToUpper());
                catPara.Format.Alignment = ParagraphAlignment.Center;
                catPara.Format.Font.Bold = true;
                catPara.Format.Font.Underline = Underline.Single;
                catPara.Format.Font.Size = 15.0;
                catPara.Format.SpaceBefore = "0.4cm";
                catPara.Format.SpaceAfter = "0.3cm";

                if (!isFirstCategory)
                {
                    catPara.Format.PageBreakBefore = true;
                }
                isFirstCategory = false;

                // Create a new Table for this Category
                var table = section.AddTable();
                table.Borders.Width = 0; // No borders outside
                
                table.AddColumn("7.0cm"); // Test Description
                table.AddColumn("3.5cm"); // Result
                table.AddColumn("2.5cm"); // Units
                table.AddColumn("5.0cm"); // Normal

                // Headers row
                var header = table.AddRow();
                header.HeadingFormat = true;
                header.Height = "0.6cm";
                header.VerticalAlignment = VerticalAlignment.Center;

                var cell0 = header.Cells[0].AddParagraph("Test Description");
                cell0.Format.Font.Bold = true;
                cell0.Format.Font.Underline = Underline.Single;
                cell0.Format.Font.Size = 10.0;

                var cell1 = header.Cells[1].AddParagraph("Result");
                cell1.Format.Font.Bold = true;
                cell1.Format.Font.Underline = Underline.Single;
                cell1.Format.Font.Size = 10.0;

                var cell2 = header.Cells[2].AddParagraph("Units");
                cell2.Format.Font.Bold = true;
                cell2.Format.Font.Underline = Underline.Single;
                cell2.Format.Font.Size = 10.0;

                var cell3 = header.Cells[3].AddParagraph("Normal");
                cell3.Format.Font.Bold = true;
                cell3.Format.Font.Underline = Underline.Single;
                cell3.Format.Font.Size = 10.0;

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

                        string displayVal = !string.IsNullOrEmpty(r.ValueText) 
                            ? r.ValueText 
                            : FormatResultValue(r.Value, r.TestType);
                        string unitStr = r.TestType.Unit ?? "";

                        var valPara = row.Cells[1].AddParagraph();
                        var valText = valPara.AddFormattedText(displayVal);

                        if (shouldPrintGroupHeader)
                        {
                            valText.Bold = true;
                        }

                        if (r.IsAbnormal)
                        {
                            valText.Bold = true;
                            valText.Color = Color.FromRgb(198, 40, 40); // Soft dark red for abnormal
                            
                            string flagStr = ReferenceRangeEvaluator.IsLow(r.Value, r.TestType, order.Patient) ? " Low" : " High";
                            var flagText = valPara.AddFormattedText(flagStr, TextFormat.Bold);
                            flagText.Color = Color.FromRgb(198, 40, 40);
                            flagText.Size = 8.5;
                        }

                        var unitPara = row.Cells[2].AddParagraph(unitStr);
                        if (shouldPrintGroupHeader)
                        {
                            unitPara.Format.Font.Bold = true;
                        }

                        string refRangeStr = ReferenceRangeEvaluator.FormatRange(r.TestType, order.Patient);
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
                        var methodPara = methodCell.AddParagraph("(Method:- " + method.ToUpper() + ")");
                        methodPara.Format.Font.Size = 8.5;
                        methodPara.Format.Font.Italic = true;
                        methodPara.Format.Font.Color = Colors.DarkGray;
                    }

                    bool isCbc = groupName != null && (groupName.IndexOf("CBC", StringComparison.OrdinalIgnoreCase) >= 0 || groupName.IndexOf("Complete Blood Count", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (isCbc)
                    {
                        var instRow = table.AddRow();
                        instRow.Height = "0.6cm";
                        var instCell = instRow.Cells[0];
                        instCell.MergeRight = 3;
                        var instPara = instCell.AddParagraph("Instruments : Fully automated cell counter ERBA H-360");
                        instPara.Format.Font.Size = 8.5;
                        instPara.Format.Font.Italic = true;
                        instPara.Format.Font.Color = Colors.DarkGray;
                    }

                    // Interpretations removed from report per user request
                }
            }



            // Authorised Signatory Text
            var sigPara = section.Footers.Primary.AddParagraph();
            sigPara.Format.Alignment = ParagraphAlignment.Right;
            sigPara.Format.SpaceBefore = "0.5cm";
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

            string dir = Path.Combine(FileUtilities.GetWritableDataDirectory(), "Reports", dateStr);
            Directory.CreateDirectory(dir);

            string safePatientName = patientName;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                safePatientName = safePatientName.Replace(c, '_');
            }
            safePatientName = safePatientName.Replace(' ', '_');

            string filepath = Path.Combine(dir, safePatientName + "_Order" + order.OrderId + "_" + dateStr + "_" + Guid.NewGuid().ToString().Substring(0, 4) + ".pdf");
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
            if (testType.Unit == "Qualitative" || testType.Name.Contains("Urine Sugar") || testType.Name.Contains("Urine Protein"))
            {
                return val >= 1.0 ? "Present" : "Absent";
            }
            if (testType.Unit == "Titer")
            {
                if (val <= 0) return "No Agglutination";
                return "Agglutination (1:" + (int)val + ")";
            }
            if (testType.Unit == "Index" || testType.Unit == "Ratio" || testType.Name.Contains("Antibody") || testType.Name.Contains("HBsAg") || testType.Name.Contains("HCV") || testType.Name.Contains("VDRL") || testType.Name.Contains("HIV"))
            {
                if (val == 0.0) return "Negative";
                return val.ToString("F2");
            }
            return val.ToString();
        }

        public async Task<string> GenerateInvoicePdfAsync(Invoice invoice, CancellationToken cancellationToken = default(CancellationToken))
        {
            EnsureSettingsLoaded();
            string dateStr = DateTime.Today.ToString("yyyy-MM-dd");
            
            Document document = new Document();
            document.Styles["Normal"].Font.Name = "Arial";
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
            var titleText = headerPara.AddFormattedText(_labName + "\n", TextFormat.Bold);
            titleText.Size = 20;
            
            var subtitleText = headerPara.AddFormattedText(_labAddress + "\n" + _labPhone + "\n\n", TextFormat.NotBold);
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

            string titlePrefix = "";
            if (invoice.Order != null && invoice.Order.Patient != null && !string.IsNullOrEmpty(invoice.Order.Patient.Title) && invoice.Order.Patient.Title != "Select Title")
            {
                titlePrefix = invoice.Order.Patient.Title + " ";
            }
            string patientName = titlePrefix + (invoice.Order != null && invoice.Order.Patient != null ? invoice.Order.Patient.FullName ?? "Unknown Patient" : "Unknown Patient");
            string invoiceId = invoice.InvoiceId.ToString();
            string orderId = invoice.OrderId.ToString();
            string createdDate = invoice.CreatedAt.ToString("dd-MMM-yyyy hh:mm tt");

            var row1 = infoTable.AddRow();
            row1.Cells[0].AddParagraph("Patient Name").Format.Font.Bold = true;
            row1.Cells[1].AddParagraph(":");
            row1.Cells[2].AddParagraph(patientName);
            row1.Cells[3].AddParagraph("UHID").Format.Font.Bold = true;
            row1.Cells[4].AddParagraph(":");
            row1.Cells[5].AddParagraph(invoice.Order != null && invoice.Order.Patient != null ? invoice.Order.Patient.Uhid ?? "" : "");

            string invoiceReferredBy = "SELF";
            if (invoice.Order != null && !string.IsNullOrWhiteSpace(invoice.Order.ReferredBy))
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

                if (_testPanelRepo != null)
                {
                    var panels = await _testPanelRepo.GetAllAsync(cancellationToken);
                    var panelMatches = PanelMatcher.MatchPanels(tests, panels);

                    foreach (var match in panelMatches)
                    {
                        var row = itemTable.AddRow();
                        row.Height = "0.6cm";
                        row.VerticalAlignment = VerticalAlignment.Center;
                        row.Cells[0].AddParagraph(sno.ToString());
                        row.Cells[1].AddParagraph(match.Panel.Name + " (Package)");
                        row.Cells[2].AddParagraph(match.Price.ToString("F2"));
                        
                        calculatedTotal += match.Price;
                        sno++;
                    }

                    var appliedIds = new HashSet<int>(panelMatches.SelectMany(m => m.MatchedTypeIds));
                    foreach (var test in tests)
                    {
                        if (!appliedIds.Contains(test.TypeId))
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
                else
                {
                    foreach (var test in tests)
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
            var balanceLbl = balanceRow.Cells[0].AddParagraph("DUE AMOUNT");
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
                statusPara.AddFormattedText("Payment Status: PAID (" + invoice.PaymentMethod + ")", TextFormat.Bold).Color = Colors.DarkGreen;
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

            string dir = Path.Combine(FileUtilities.GetWritableDataDirectory(), "Invoices", dateStr);
            Directory.CreateDirectory(dir);

            string safePatientName = patientName;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                safePatientName = safePatientName.Replace(c, '_');
            }
            safePatientName = safePatientName.Replace(' ', '_');

            string filepath = Path.Combine(dir, "Invoice_" + invoiceId + "_" + safePatientName + "_" + dateStr + "_" + Guid.NewGuid().ToString().Substring(0, 4) + ".pdf");
            renderer.PdfDocument.Save(filepath);
            return filepath;
        }
    }
}
