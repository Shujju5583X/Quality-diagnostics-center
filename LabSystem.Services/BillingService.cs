using System;
using ClosedXML.Excel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSystem.Core.Models;
using Serilog;
using System.Data.Entity;
using LabSystem.Data;

namespace LabSystem.Services
{
    public class BillingService : IBillingService
    {
        private readonly LabDbContext _context;

        public BillingService(LabDbContext context)
        {
            _context = context;
        }

        public async Task<Invoice> GenerateInvoiceAsync(int orderId)
        {
            try
            {
                var order = await _context.TestOrders.Include(o => o.TestTypes).FirstOrDefaultAsync(o => o.OrderId == orderId);
                if (order == null) throw new Exception("Order not found.");

                // Check if invoice already exists
                var existing = await _context.Invoices.FirstOrDefaultAsync(i => i.OrderId == orderId);
                if (existing != null) return existing;

                // Load all test panels with their test types eager-loaded
                var panels = await _context.TestPanels.Include(p => p.TestTypes).ToListAsync();

                var orderedTestTypeIds = new HashSet<int>(order.TestTypes.Select(t => t.TypeId));
                decimal total = 0;
                var testTypesAppliedToPanels = new HashSet<int>();

                // Sort panels by number of tests descending to match larger panels first
                foreach (var panel in panels.OrderByDescending(p => p.TestTypes.Count))
                {
                    var panelTestTypeIds = panel.TestTypes.Select(t => t.TypeId).ToList();
                    if (panelTestTypeIds.Count > 0 && panelTestTypeIds.All(id => orderedTestTypeIds.Contains(id) && !testTypesAppliedToPanels.Contains(id)))
                    {
                        total += panel.Price;
                        foreach (var id in panelTestTypeIds)
                        {
                            testTypesAppliedToPanels.Add(id);
                        }
                    }
                }

                // Add remaining individual test type prices
                foreach (var testType in order.TestTypes)
                {
                    if (!testTypesAppliedToPanels.Contains(testType.TypeId))
                    {
                        total += testType.Price;
                    }
                }

                var invoice = new Invoice
                {
                    OrderId = orderId,
                    TotalAmount = total,
                    IsPaid = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();
                
                return invoice;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to generate invoice for order {orderId}");
                throw;
            }
        }

        public async Task<Invoice> GetInvoiceForOrderAsync(int orderId)
        {
            return await _context.Invoices
                .Include(i => i.Order)
                .Include(i => i.Order.Patient)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.OrderId == orderId);
        }

        public async Task<IEnumerable<Invoice>> GetAllInvoicesAsync()
        {
            return await _context.Invoices
                .Include(i => i.Order)
                .Include(i => i.Order.Patient)
                .Include(i => i.Payments)
                .ToListAsync();
        }

        public async Task UpdateInvoiceFinancialsAsync(int invoiceId, decimal discount, decimal tax)
        {
            var invoice = await _context.Invoices.Include(i => i.Payments).FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
            if (invoice != null)
            {
                invoice.DiscountAmount = discount;
                invoice.TaxAmount = tax;
                decimal grandTotal = invoice.TotalAmount - invoice.DiscountAmount + invoice.TaxAmount;
                decimal paidAmount = invoice.Payments.Sum(p => p.Amount);
                invoice.IsPaid = paidAmount >= grandTotal;
                if (invoice.IsPaid && !invoice.PaidAt.HasValue) invoice.PaidAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddPaymentAsync(int invoiceId, decimal amount, string paymentMethod)
        {
            var invoice = await _context.Invoices.Include(i => i.Payments).FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
            if (invoice != null)
            {
                var payment = new Payment
                {
                    InvoiceId = invoiceId,
                    Amount = amount,
                    PaymentMethod = paymentMethod,
                    PaymentDate = DateTime.UtcNow
                };
                _context.Payments.Add(payment);
                
                // Recalculate IsPaid
                decimal grandTotal = invoice.TotalAmount - invoice.DiscountAmount + invoice.TaxAmount;
                decimal paidAmount = invoice.Payments.Sum(p => p.Amount) + amount; // Include the new payment
                
                invoice.IsPaid = paidAmount >= grandTotal;
                if (invoice.IsPaid && !invoice.PaidAt.HasValue)
                {
                    invoice.PaidAt = DateTime.UtcNow;
                    invoice.PaymentMethod = paymentMethod; // Keep legacy field updated
                }
                
                await _context.SaveChangesAsync();
            }
        }

        public async Task<RevenueReportStats> GetRevenueReportAsync(DateTime startDate, DateTime endDate)
        {
            // End date should cover the whole day
            var actualEndDate = endDate.Date.AddDays(1).AddTicks(-1);
            var invoices = await _context.Invoices
                .Include(i => i.Payments)
                .Where(i => i.CreatedAt >= startDate && i.CreatedAt <= actualEndDate)
                .ToListAsync();

            decimal totalRevenue = invoices.Sum(i => i.TotalAmount - i.DiscountAmount + i.TaxAmount);
            
            var payments = await _context.Payments
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= actualEndDate)
                .ToListAsync();

            decimal cashCollected = payments.Where(p => p.PaymentMethod == "Cash").Sum(p => p.Amount);
            decimal upiCollected = payments.Where(p => p.PaymentMethod == "UPI").Sum(p => p.Amount);
            decimal totalCollected = payments.Sum(p => p.Amount);

            decimal outstanding = invoices.Sum(i => 
            {
                decimal grandTotal = i.TotalAmount - i.DiscountAmount + i.TaxAmount;
                decimal paid = i.Payments.Sum(p => p.Amount);
                return Math.Max(0, grandTotal - paid);
            });

            return new RevenueReportStats
            {
                TotalRevenue = totalRevenue,
                TotalCollected = totalCollected,
                CashCollected = cashCollected,
                UpiCollected = upiCollected,
                OutstandingAmount = outstanding
            };
        }

        public async Task ExportRevenueReportToExcelAsync(DateTime startDate, DateTime endDate, string filePath)
        {
            var stats = await GetRevenueReportAsync(startDate, endDate);
            var actualEndDate = endDate.Date.AddDays(1).AddTicks(-1);
            
            var payments = await _context.Payments
                .Include(p => p.Invoice.Order.Patient)
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= actualEndDate)
                .OrderBy(p => p.PaymentDate)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Revenue Report");
                ws.Cell("A1").Value = "Revenue Report";
                ws.Cell("A1").Style.Font.Bold = true;
                ws.Cell("A1").Style.Font.FontSize = 16;
                ws.Range("A1:D1").Merge();

                ws.Cell("A3").Value = "Period:";
                ws.Cell("B3").Value = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}";

                ws.Cell("A5").Value = "Total Revenue:";
                ws.Cell("B5").Value = stats.TotalRevenue;
                ws.Cell("A6").Value = "Total Collected:";
                ws.Cell("B6").Value = stats.TotalCollected;
                ws.Cell("A7").Value = "Cash Collected:";
                ws.Cell("B7").Value = stats.CashCollected;
                ws.Cell("A8").Value = "UPI Collected:";
                ws.Cell("B8").Value = stats.UpiCollected;
                ws.Cell("A9").Value = "Outstanding Amount:";
                ws.Cell("B9").Value = stats.OutstandingAmount;

                ws.Cell("A11").Value = "Payment Ledger";
                ws.Cell("A11").Style.Font.Bold = true;

                ws.Cell("A12").Value = "Date";
                ws.Cell("B12").Value = "Patient Name";
                ws.Cell("C12").Value = "Amount";
                ws.Cell("D12").Value = "Method";

                int row = 13;
                foreach (var p in payments)
                {
                    ws.Cell($"A{row}").Value = p.PaymentDate.ToString("yyyy-MM-dd HH:mm");
                    ws.Cell($"B{row}").Value = p.Invoice?.Order?.Patient?.FullName ?? "Unknown";
                    ws.Cell($"C{row}").Value = p.Amount;
                    ws.Cell($"D{row}").Value = p.PaymentMethod;
                    row++;
                }

                ws.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
            }
        }

        public async Task<IEnumerable<DoctorReferralStats>> GetDoctorReferralStatsAsync(DateTime startDate, DateTime endDate)
        {
            // Filter invoices where order has a doctor and order was placed within range
            var invoices = await _context.Invoices
                .Include(i => i.Order)
                .Include(i => i.Order.Doctor)
                .Where(i => i.Order.DoctorId.HasValue && i.Order.OrderedAt >= startDate && i.Order.OrderedAt <= endDate)
                .ToListAsync();

            var stats = invoices
                .GroupBy(i => i.Order.Doctor)
                .Select(g => new DoctorReferralStats
                {
                    DoctorId = g.Key.DoctorId,
                    DoctorName = g.Key.Name,
                    Specialization = g.Key.Specialization,
                    ClinicName = g.Key.ClinicName,
                    CommissionPercent = g.Key.CommissionPercent,
                    TotalOrders = g.Select(i => i.OrderId).Distinct().Count(),
                    TotalRevenue = g.Sum(i => i.TotalAmount),
                    CommissionPayable = g.Sum(i => i.TotalAmount * (g.Key.CommissionPercent / 100m))
                })
                .ToList();

            return stats;
        }
    }
}
