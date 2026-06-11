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
                var order = await _context.TestOrders.FirstOrDefaultAsync(o => o.OrderId == orderId);
                if (order == null) throw new Exception("Order not found.");

                // Check if invoice already exists
                var existing = await _context.Invoices.FirstOrDefaultAsync(i => i.OrderId == orderId);
                if (existing != null) return existing;

                decimal total = 0;
                if (!string.IsNullOrWhiteSpace(order.Notes))
                {
                    var testIds = order.Notes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(id => int.Parse(id)).ToList();
                    
                    var tests = await _context.TestTypes.Where(t => testIds.Contains(t.TypeId)).ToListAsync();
                    total = tests.Sum(t => t.Price);
                }

                var invoice = new Invoice
                {
                    InvoiceId = orderId,
                    OrderId = orderId,
                    TotalAmount = total,
                    IsPaid = false,
                    CreatedAt = DateTime.UtcNow.ToString("O")
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
                .FirstOrDefaultAsync(i => i.OrderId == orderId);
        }

        public async Task<IEnumerable<Invoice>> GetAllInvoicesAsync()
        {
            return await _context.Invoices
                .Include(i => i.Order)
                .Include(i => i.Order.Patient)
                .ToListAsync();
        }

        public async Task MarkAsPaidAsync(int invoiceId, string paymentMethod)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice != null)
            {
                invoice.IsPaid = true;
                invoice.PaidAt = DateTime.UtcNow.ToString("O");
                invoice.PaymentMethod = paymentMethod;
                await _context.SaveChangesAsync();
            }
        }
    }
}
