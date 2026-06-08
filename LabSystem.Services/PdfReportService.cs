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
            
            Document document = new Document();
            Section section = document.AddSection();
            section.AddParagraph("Medical Lab Management System").Format.Font.Size = 16;
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

            PdfDocumentRenderer renderer = new PdfDocumentRenderer(true)
            {
                Document = document
            };
            renderer.RenderDocument();

            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", order.OrderId.ToString());
            Directory.CreateDirectory(dir);
            string filepath = Path.Combine(dir, $"report_{order.OrderId}.pdf");

            renderer.PdfDocument.Save(filepath);
            return filepath;
        }
    }
}
