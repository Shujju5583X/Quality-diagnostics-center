using System;
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

    }
}
