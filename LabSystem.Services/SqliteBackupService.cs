using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Services
{
    public class SqliteBackupService : IBackupService
    {
        private readonly IPatientRepository _patientRepo;
        private readonly ITestOrderRepository _orderRepo;
        private readonly IResultRepository _resultRepo;
        private readonly IRepository<TestType> _testTypeRepo;
        private readonly IRepository<Staff> _staffRepo;

        public SqliteBackupService(
            IPatientRepository patientRepo,
            ITestOrderRepository orderRepo,
            IResultRepository resultRepo,
            IRepository<TestType> testTypeRepo,
            IRepository<Staff> staffRepo)
        {
            _patientRepo = patientRepo;
            _orderRepo = orderRepo;
            _resultRepo = resultRepo;
            _testTypeRepo = testTypeRepo;
            _staffRepo = staffRepo;
        }

        public async Task BackupNowAsync(CancellationToken cancellationToken = default)
        {
            string dbBackupsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups", "Database");
            string excelBackupsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups", "Excel");
            
            Directory.CreateDirectory(dbBackupsDir);
            Directory.CreateDirectory(excelBackupsDir);
            
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmm");
            
            // 1. Core SQLite Database Backup (for Developers/Admins)
            string sourceFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lab.db");
            if (File.Exists(sourceFile))
            {
                string dbDestFile = Path.Combine(dbBackupsDir, $"lab_backup_{timestamp}.db");
                File.Copy(sourceFile, dbDestFile, true);
            }

            // 2. Human-Readable Excel Spreadsheet Backup (for Lab Technicians)
            string excelDestFile = Path.Combine(excelBackupsDir, $"lab_backup_{timestamp}.xlsx");
            await GenerateExcelBackupAsync(excelDestFile, cancellationToken);
        }

        private async Task GenerateExcelBackupAsync(string filePath, CancellationToken cancellationToken = default)
        {
            // Load all data into memory dictionaries for fast mapping and lookup
            var patientsDict = (await _patientRepo.GetAllAsync(cancellationToken)).ToDictionary(p => p.PatientId);
            var testTypesDict = (await _testTypeRepo.GetAllAsync(cancellationToken)).ToDictionary(t => t.TypeId);
            var staffDict = (await _staffRepo.GetAllAsync(cancellationToken)).ToDictionary(s => s.StaffId);
            var ordersDict = (await _orderRepo.GetAllAsync(cancellationToken)).ToDictionary(o => o.OrderId);
            var resultsList = await _resultRepo.GetAllAsync(cancellationToken);

            using (var workbook = new XLWorkbook())
            {
                // Styling Helper Colors
                var sidebarDark = XLColor.FromHtml("#1E1926");
                var headerTextColor = XLColor.White;

                // ==========================================
                // 1. Patients Worksheet
                // ==========================================
                var wsPatients = workbook.Worksheets.Add("Patients");
                var patientAccent = XLColor.FromHtml("#3F51B5"); // Indigo
                var patientBorder = XLColor.FromHtml("#1A237E");

                // Banner
                wsPatients.Cell(1, 1).Value = "Quality Diagnostics Center - Patient Directory";
                var rPatientTitle = wsPatients.Range(1, 1, 1, 7);
                rPatientTitle.Merge();
                StyleTitleRange(rPatientTitle, sidebarDark, headerTextColor);
                wsPatients.Row(1).Height = 40;

                // Headers
                string[] pHeaders = { "Patient ID", "Full Name", "Date of Birth", "Gender", "Contact Phone", "Contact Email", "Registered Date" };
                StyleHeaderRow(wsPatients, 3, pHeaders, patientAccent, headerTextColor, patientBorder);
                wsPatients.Row(3).Height = 28;

                int pRow = 4;
                foreach (var p in patientsDict.Values.OrderBy(x => x.PatientId))
                {
                    wsPatients.Cell(pRow, 1).Value = p.PatientId;
                    wsPatients.Cell(pRow, 2).Value = p.FullName;
                    wsPatients.Cell(pRow, 3).Value = p.DateOfBirth.HasValue ? p.DateOfBirth.Value.ToString("yyyy-MM-dd") : "";
                    wsPatients.Cell(pRow, 4).Value = p.Gender;
                    wsPatients.Cell(pRow, 5).Value = p.ContactPhone;
                    wsPatients.Cell(pRow, 6).Value = p.ContactEmail;
                    wsPatients.Cell(pRow, 7).Value = p.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");

                    StyleDataRow(wsPatients, pRow, 7);
                    pRow++;
                }
                wsPatients.Columns().AdjustToContents();

                // ==========================================
                // 2. Test Orders Worksheet
                // ==========================================
                var wsOrders = workbook.Worksheets.Add("Test Orders");
                var orderAccent = XLColor.FromHtml("#FF9800"); // Orange
                var orderBorder = XLColor.FromHtml("#E65100");

                // Banner
                wsOrders.Cell(1, 1).Value = "Quality Diagnostics Center - Test Orders Record";
                var rOrderTitle = wsOrders.Range(1, 1, 1, 8);
                rOrderTitle.Merge();
                StyleTitleRange(rOrderTitle, sidebarDark, headerTextColor);
                wsOrders.Row(1).Height = 40;

                // Headers
                string[] oHeaders = { "Order ID", "Patient ID", "Patient Name", "Order Date", "Referred By", "Status", "Requested Tests", "Notes" };
                StyleHeaderRow(wsOrders, 3, oHeaders, orderAccent, headerTextColor, orderBorder);
                wsOrders.Row(3).Height = 28;

                int oRow = 4;
                foreach (var o in ordersDict.Values.OrderBy(x => x.OrderId))
                {
                    wsOrders.Cell(oRow, 1).Value = o.OrderId;
                    wsOrders.Cell(oRow, 2).Value = o.PatientId;
                    wsOrders.Cell(oRow, 3).Value = patientsDict.TryGetValue(o.PatientId, out var p) ? p.FullName : "Unknown Patient";
                    wsOrders.Cell(oRow, 4).Value = o.OrderedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    wsOrders.Cell(oRow, 5).Value = o.ReferredBy;
                    wsOrders.Cell(oRow, 6).Value = o.Status;
                    wsOrders.Cell(oRow, 7).Value = string.Join(", ", o.TestTypes.Select(tt => tt.Name));
                    wsOrders.Cell(oRow, 8).Value = o.Notes;

                    StyleDataRow(wsOrders, oRow, 8);
                    
                    // Specific formatting for Pending vs Complete status
                    var statusCell = wsOrders.Cell(oRow, 6);
                    if (o.Status == "Complete")
                    {
                        statusCell.Style.Font.FontColor = XLColor.FromHtml("#00796B");
                        statusCell.Style.Font.Bold = true;
                    }
                    else
                    {
                        statusCell.Style.Font.FontColor = XLColor.FromHtml("#E65100");
                        statusCell.Style.Font.Bold = true;
                    }

                    oRow++;
                }
                wsOrders.Columns().AdjustToContents();

                // ==========================================
                // 3. Test Results Worksheet
                // ==========================================
                var wsResults = workbook.Worksheets.Add("Test Results");
                var resultAccent = XLColor.FromHtml("#673AB7"); // Purple
                var resultBorder = XLColor.FromHtml("#4527A0");

                // Banner
                wsResults.Cell(1, 1).Value = "Quality Diagnostics Center - Patient Test Results";
                var rResultTitle = wsResults.Range(1, 1, 1, 10);
                rResultTitle.Merge();
                StyleTitleRange(rResultTitle, sidebarDark, headerTextColor);
                wsResults.Row(1).Height = 40;

                // Headers
                string[] rHeaders = { "Result ID", "Order ID", "Patient Name", "Test Name", "Measured Value", "Normal Range", "Unit", "Abnormality Flag", "Recorded Date", "Technician" };
                StyleHeaderRow(wsResults, 3, rHeaders, resultAccent, headerTextColor, resultBorder);
                wsResults.Row(3).Height = 28;

                int resRow = 4;
                foreach (var r in resultsList.OrderBy(x => x.ResultId))
                {
                    var order = ordersDict.TryGetValue(r.OrderId, out var o) ? o : null;
                    var patientName = (order != null && patientsDict.TryGetValue(order.PatientId, out var pat)) ? pat.FullName : "Unknown Patient";
                    var testType = testTypesDict.TryGetValue(r.TypeId, out var tt) ? tt : null;

                    string testName = testType?.Name ?? "Unknown Test";
                    string unit = testType?.Unit ?? "";
                    string normalRange = FormatReferenceRange(testType);

                    wsResults.Cell(resRow, 1).Value = r.ResultId;
                    wsResults.Cell(resRow, 2).Value = r.OrderId;
                    wsResults.Cell(resRow, 3).Value = patientName;
                    wsResults.Cell(resRow, 4).Value = testName;
                    wsResults.Cell(resRow, 5).Value = r.Value;
                    wsResults.Cell(resRow, 6).Value = normalRange;
                    wsResults.Cell(resRow, 7).Value = unit;
                    wsResults.Cell(resRow, 8).Value = r.IsAbnormal ? "ABNORMAL" : "Normal";
                    wsResults.Cell(resRow, 9).Value = r.RecordedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    wsResults.Cell(resRow, 10).Value = staffDict.TryGetValue(r.TechnicianId, out var tech) ? tech.FullName : "System";

                    StyleDataRow(wsResults, resRow, 10);

                    // If abnormal, apply striking visual highlighting
                    if (r.IsAbnormal)
                    {
                        var rowRange = wsResults.Range(resRow, 1, resRow, 10);
                        rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEBEE"); // Light pink/red highlight
                        
                        var flagCell = wsResults.Cell(resRow, 8);
                        flagCell.Style.Font.FontColor = XLColor.FromHtml("#C62828"); // Dark Red
                        flagCell.Style.Font.Bold = true;
                    }
                    else
                    {
                        var flagCell = wsResults.Cell(resRow, 8);
                        flagCell.Style.Font.FontColor = XLColor.FromHtml("#2E7D32"); // Dark Green
                    }

                    resRow++;
                }
                wsResults.Columns().AdjustToContents();

                // ==========================================
                // 4. Test Types (Reference Ranges) Worksheet
                // ==========================================
                var wsTestTypes = workbook.Worksheets.Add("Reference Catalog");
                var catalogAccent = XLColor.FromHtml("#607D8B"); // Slate Gray
                var catalogBorder = XLColor.FromHtml("#37474F");

                // Banner
                wsTestTypes.Cell(1, 1).Value = "Quality Diagnostics Center - Laboratory Test Reference Ranges";
                var rCatalogTitle = wsTestTypes.Range(1, 1, 1, 6);
                rCatalogTitle.Merge();
                StyleTitleRange(rCatalogTitle, sidebarDark, headerTextColor);
                wsTestTypes.Row(1).Height = 40;

                // Headers
                string[] tHeaders = { "Type ID", "Test Name", "Unit", "Reference Low", "Reference High", "Active Status" };
                StyleHeaderRow(wsTestTypes, 3, tHeaders, catalogAccent, headerTextColor, catalogBorder);
                wsTestTypes.Row(3).Height = 28;

                int tRow = 4;
                foreach (var tt in testTypesDict.Values.OrderBy(x => x.TypeId))
                {
                    wsTestTypes.Cell(tRow, 1).Value = tt.TypeId;
                    wsTestTypes.Cell(tRow, 2).Value = tt.Name;
                    wsTestTypes.Cell(tRow, 3).Value = tt.Unit;
                    wsTestTypes.Cell(tRow, 4).Value = tt.ReferenceRangeLow.HasValue ? tt.ReferenceRangeLow.Value : "";
                    wsTestTypes.Cell(tRow, 5).Value = tt.ReferenceRangeHigh.HasValue ? tt.ReferenceRangeHigh.Value : "";
                    wsTestTypes.Cell(tRow, 6).Value = tt.IsActive ? "Active" : "Inactive";

                    StyleDataRow(wsTestTypes, tRow, 6);
                    tRow++;
                }
                wsTestTypes.Columns().AdjustToContents();

                // ==========================================
                // 5. Staff Directory Worksheet
                // ==========================================
                var wsStaff = workbook.Worksheets.Add("Staff Directory");
                var staffAccent = XLColor.FromHtml("#4CAF50"); // Emerald Green
                var staffBorder = XLColor.FromHtml("#2E7D32");

                // Banner
                wsStaff.Cell(1, 1).Value = "Quality Diagnostics Center - Active Staff Directory";
                var rStaffTitle = wsStaff.Range(1, 1, 1, 2);
                rStaffTitle.Merge();
                StyleTitleRange(rStaffTitle, sidebarDark, headerTextColor);
                wsStaff.Row(1).Height = 40;

                // Headers
                string[] sHeaders = { "Staff ID", "Full Name" };
                StyleHeaderRow(wsStaff, 3, sHeaders, staffAccent, headerTextColor, staffBorder);
                wsStaff.Row(3).Height = 28;

                int sRow = 4;
                foreach (var staff in staffDict.Values.OrderBy(x => x.StaffId))
                {
                    wsStaff.Cell(sRow, 1).Value = staff.StaffId;
                    wsStaff.Cell(sRow, 2).Value = staff.FullName;

                    StyleDataRow(wsStaff, sRow, 2);
                    sRow++;
                }
                wsStaff.Columns().AdjustToContents();

                // Save completed Excel Workbook
                workbook.SaveAs(filePath);
            }

            // Force immediate memory reclaim after heavy Excel generation
            // Critical on 4GB RAM systems where ClosedXML can spike to 200-400MB
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        // ==========================================
        // STYLING HELPERS
        // ==========================================

        private void StyleTitleRange(IXLRange range, XLColor background, XLColor text)
        {
            range.Style.Font.Bold = true;
            range.Style.Font.FontSize = 16;
            range.Style.Font.FontColor = text;
            range.Style.Font.FontName = "Segoe UI";
            range.Style.Fill.BackgroundColor = background;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        private void StyleHeaderRow(IXLWorksheet ws, int rowNum, string[] headers, XLColor background, XLColor text, XLColor borderColor)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(rowNum, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 11;
                cell.Style.Font.FontColor = text;
                cell.Style.Font.FontName = "Segoe UI";
                cell.Style.Fill.BackgroundColor = background;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                cell.Style.Border.OutsideBorderColor = borderColor;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
        }

        private void StyleDataRow(IXLWorksheet ws, int rowNum, int totalCols)
        {
            // Subtle zebra-striping
            var bg = (rowNum % 2 == 0) ? XLColor.FromHtml("#FAF9FC") : XLColor.White;

            for (int col = 1; col <= totalCols; col++)
            {
                var cell = ws.Cell(rowNum, col);
                cell.Style.Fill.BackgroundColor = bg;
                cell.Style.Font.FontName = "Segoe UI";
                cell.Style.Font.FontSize = 10;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.LightGray;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                
                // Align numeric values to right, text and IDs to center/left
                if (cell.DataType == XLDataType.Number)
                {
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }
                else if (col == 1 || (col == 2 && totalCols > 3)) // IDs usually col 1, col 2
                {
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                else
                {
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                }
            }
            ws.Row(rowNum).Height = 22;
        }

        private string GetRequestedTestNames(string notes, Dictionary<int, TestType> testTypesDict)
        {
            if (string.IsNullOrWhiteSpace(notes)) return string.Empty;
            
            var names = new List<string>();
            var ids = notes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var idStr in ids)
            {
                if (int.TryParse(idStr, out int id) && testTypesDict.TryGetValue(id, out var tt))
                {
                    names.Add(tt.Name);
                }
            }
            return string.Join(", ", names);
        }

        private string FormatReferenceRange(TestType tt)
        {
            if (tt == null) return "N/A";
            
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

