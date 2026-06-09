using System;
using System.IO;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

namespace LabSystem.Services
{
    public class PdfReportService : IPdfReportService
    {
        private readonly IResultRepository _resultRepo;

        public PdfReportService(IResultRepository resultRepo)
        {
            _resultRepo = resultRepo;
        }

        public string GenerateReport(TestOrder order)
        {
            var results = _resultRepo.GetResultsForOrder(order.OrderId);
            string dateStr = DateTime.Today.ToString("yyyy-MM-dd");
            
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
            
            Document document = new Document();
            Section section = document.AddSection();
            
            // Header section with logo and title side-by-side
            var headerTable = section.AddTable();
            headerTable.Borders.Width = 0;
            headerTable.AddColumn("2.5cm");
            headerTable.AddColumn("13.5cm");
            
            var headerRow = headerTable.AddRow();
            if (File.Exists(logoPath))
            {
                var img = headerRow.Cells[0].AddImage(logoPath);
                img.Width = "2cm";
                img.Height = "2cm";
            }
            
            var titlePara = headerRow.Cells[1].AddParagraph();
            titlePara.Format.SpaceBefore = "0.2cm";
            var titleText = titlePara.AddFormattedText("Quality Diagnostics Center", TextFormat.Bold);
            titleText.Size = 18;
            titleText.Color = Colors.DarkSlateBlue;
            titlePara.AddLineBreak();
            var subtitleText = titlePara.AddFormattedText($"Patient Report: {order.Patient?.FullName} | Date: {dateStr}", TextFormat.NotBold);
            subtitleText.Size = 11;
            subtitleText.Color = Colors.Gray;
            
            section.AddParagraph();
            section.AddParagraph($"Patient: {order.Patient?.FullName}");
            section.AddParagraph($"Order ID: {order.OrderId} | Date: {order.OrderedAt}");
            section.AddParagraph();


            // Example implementation for PDF table using MigraDoc
            var table = section.AddTable();
            table.Borders.Width = 0.75;
            table.AddColumn("4cm"); // Test Name
            table.AddColumn("2cm"); // Result
            table.AddColumn("2cm"); // Unit
            table.AddColumn("3cm"); // Ref Range
            table.AddColumn("3cm"); // Flag

            var header = table.AddRow();
            header.Cells[0].AddParagraph("Test Name");
            header.Cells[1].AddParagraph("Result");
            header.Cells[2].AddParagraph("Unit");
            header.Cells[3].AddParagraph("Ref Range");
            header.Cells[4].AddParagraph("Flag");

            foreach (var r in results)
            {
                var row = table.AddRow();
                row.Cells[0].AddParagraph(r.TestType?.Name ?? "Unknown");
                row.Cells[1].AddParagraph(r.Value.ToString());
                row.Cells[2].AddParagraph(r.TestType?.Unit ?? "");
                row.Cells[3].AddParagraph($"{r.TestType?.ReferenceRangeLow} - {r.TestType?.ReferenceRangeHigh}");
                row.Cells[4].AddParagraph(r.IsAbnormal ? "Abnormal" : "Normal");
            }

            PdfDocumentRenderer renderer = new PdfDocumentRenderer()
            {
                Document = document
            };
            renderer.RenderDocument();

            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", dateStr);
            Directory.CreateDirectory(dir);

            string patientName = order.Patient?.FullName ?? "UnknownPatient";
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                patientName = patientName.Replace(c, '_');
            }
            patientName = patientName.Replace(' ', '_');

            string filepath = Path.Combine(dir, $"{patientName}_{dateStr}.pdf");

            renderer.PdfDocument.Save(filepath);
            return filepath;
        }
    }
}
