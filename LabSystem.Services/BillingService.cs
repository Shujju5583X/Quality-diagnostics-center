using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using Serilog;

namespace LabSystem.Services
{
    public class BillingService : IBillingService
    {
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly ITestOrderRepository _orderRepo;
        private readonly IRepository<TestPanel> _panelRepo;

        public BillingService(
            IInvoiceRepository invoiceRepo,
            IPaymentRepository paymentRepo,
            ITestOrderRepository orderRepo,
            IRepository<TestPanel> panelRepo)
        {
            _invoiceRepo = invoiceRepo;
            _paymentRepo = paymentRepo;
            _orderRepo = orderRepo;
            _panelRepo = panelRepo;
        }

        public async Task<Invoice> GenerateInvoiceAsync(int orderId)
        {
            try
            {
                var order = await _orderRepo.GetByIdAsync(orderId);
                if (order == null) throw new Exception("Order not found.");

                // Check if invoice already exists
                var existing = await _invoiceRepo.GetByOrderIdAsync(orderId);
                if (existing != null) return existing;

                // Load all test panels with their test types eager-loaded
                var panels = await _panelRepo.GetAllAsync();

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

                await _invoiceRepo.AddAsync(invoice);
                
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
            return await _invoiceRepo.GetByOrderIdAsync(orderId);
        }

        public async Task<IEnumerable<Invoice>> GetAllInvoicesAsync()
        {
            return await _invoiceRepo.GetAllWithDetailsAsync();
        }

        public async Task UpdateInvoiceFinancialsAsync(int invoiceId, decimal discount, decimal tax)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(invoiceId);
            if (invoice != null)
            {
                invoice.DiscountAmount = discount;
                invoice.TaxAmount = tax;
                decimal grandTotal = invoice.TotalAmount - invoice.DiscountAmount + invoice.TaxAmount;
                var payments = await _paymentRepo.GetByInvoiceIdAsync(invoiceId);
                decimal paidAmount = payments.Sum(p => p.Amount);
                invoice.IsPaid = paidAmount >= grandTotal;
                if (invoice.IsPaid && !invoice.PaidAt.HasValue) invoice.PaidAt = DateTime.UtcNow;
                await _invoiceRepo.UpdateAsync(invoice);
            }
        }

        public async Task AddPaymentAsync(int invoiceId, decimal amount, string paymentMethod)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(invoiceId);
            if (invoice != null)
            {
                var payment = new Payment
                {
                    InvoiceId = invoiceId,
                    Amount = amount,
                    PaymentMethod = paymentMethod,
                    PaymentDate = DateTime.UtcNow
                };
                await _paymentRepo.AddAsync(payment);
                
                // Recalculate IsPaid
                var payments = await _paymentRepo.GetByInvoiceIdAsync(invoiceId);
                decimal grandTotal = invoice.TotalAmount - invoice.DiscountAmount + invoice.TaxAmount;
                decimal paidAmount = payments.Sum(p => p.Amount);
                
                invoice.IsPaid = paidAmount >= grandTotal;
                if (invoice.IsPaid && !invoice.PaidAt.HasValue)
                {
                    invoice.PaidAt = DateTime.UtcNow;
                    invoice.PaymentMethod = paymentMethod;
                }
                
                await _invoiceRepo.UpdateAsync(invoice);
            }
        }
    }
}
