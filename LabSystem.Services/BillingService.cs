using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.Core.Services;
using Serilog;

namespace LabSystem.Services
{
    public class BillingService : IBillingService
    {
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly ITestOrderRepository _orderRepo;
        private readonly IRepository<TestPanel> _panelRepo;
        private readonly IRepository<DoctorCommission> _commissionRepo;
        private readonly IRepository<Doctor> _doctorRepo;

        public BillingService(
            IInvoiceRepository invoiceRepo,
            IPaymentRepository paymentRepo,
            ITestOrderRepository orderRepo,
            IRepository<TestPanel> panelRepo,
            IRepository<DoctorCommission> commissionRepo,
            IRepository<Doctor> doctorRepo)
        {
            _invoiceRepo = invoiceRepo;
            _paymentRepo = paymentRepo;
            _orderRepo = orderRepo;
            _panelRepo = panelRepo;
            _commissionRepo = commissionRepo;
            _doctorRepo = doctorRepo;
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
                var panelMatches = PanelMatcher.MatchPanels(order.TestTypes, panels);
                decimal total = 0;

                foreach (var match in panelMatches)
                {
                    total += match.Price;
                    decimal distributedCost = match.Price / match.MatchedTypeIds.Count;
                    foreach (var id in match.MatchedTypeIds)
                        await _orderRepo.UpdateOrderTestPricingAsync(orderId, id, match.Panel.PanelId, distributedCost);
                }

                // Add remaining individual test type prices
                var appliedIds = new HashSet<int>(panelMatches.SelectMany(m => m.MatchedTypeIds));
                foreach (var testType in order.TestTypes)
                {
                    if (!appliedIds.Contains(testType.TypeId))
                    {
                        total += testType.Price;
                        await _orderRepo.UpdateOrderTestPricingAsync(orderId, testType.TypeId, null, testType.Price);
                    }
                }

                var invoice = new Invoice
                {
                    OrderId = orderId,
                    TotalAmount = total,
                    Status = "Pending",
                    IsPaid = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _invoiceRepo.AddAsync(invoice);
                
                return await _invoiceRepo.GetByOrderIdAsync(orderId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to generate invoice for order " + orderId);
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

        public async Task UpdateInvoiceFinancialsAsync(int invoiceId, decimal discountAmount, decimal taxAmount)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(invoiceId);
            if (invoice != null)
            {
                invoice.DiscountAmount = discountAmount;
                invoice.TaxAmount = taxAmount;
                var payments = await _paymentRepo.GetByInvoiceIdAsync(invoiceId);
                decimal paidAmount = payments.Sum(p => p.Amount);
                invoice.AmountPaid = paidAmount;
                invoice.IsPaid = paidAmount >= invoice.GrandTotal;
                invoice.Status = invoice.IsPaid ? "Paid" : (paidAmount > 0 ? "Partial" : "Pending");
                if (invoice.IsPaid && !invoice.PaidAt.HasValue) invoice.PaidAt = DateTime.UtcNow;
                await _invoiceRepo.UpdateAsync(invoice);
            }
        }

        public async Task AddPaymentAsync(int invoiceId, decimal amount, string paymentMethod)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(invoiceId);
            if (invoice == null)
            {
                throw new InvalidOperationException("Invoice not found.");
            }

            // Check for overpayment
            var existingPayments = await _paymentRepo.GetByInvoiceIdAsync(invoiceId);
            decimal currentPaidAmount = existingPayments.Sum(p => p.Amount);
            if (currentPaidAmount + amount > invoice.GrandTotal)
            {
                throw new InvalidOperationException(
                    string.Format("Payment of {0} would exceed outstanding balance of {1}.", amount, invoice.GrandTotal - currentPaidAmount));
            }

            var payment = new Payment
            {
                InvoiceId = invoiceId,
                Amount = amount,
                PaymentMethod = paymentMethod,
                PaymentDate = DateTime.UtcNow
            };
            await _paymentRepo.AddAsync(payment);
            
            // Recalculate IsPaid using computed GrandTotal
            var payments = await _paymentRepo.GetByInvoiceIdAsync(invoiceId);
            decimal paidAmount = payments.Sum(p => p.Amount);
            
            invoice.AmountPaid = paidAmount;
            invoice.IsPaid = paidAmount >= invoice.GrandTotal;
            invoice.Status = invoice.IsPaid ? "Paid" : (paidAmount > 0 ? "Partial" : "Pending");
            if (invoice.IsPaid && !invoice.PaidAt.HasValue)
            {
                invoice.PaidAt = DateTime.UtcNow;
                invoice.PaymentMethod = paymentMethod;

                // Implement Doctor Commission Trigger
                var order = await _orderRepo.GetByIdAsync(invoice.OrderId);
                if (order != null && order.DoctorId.HasValue)
                {
                    var doctor = await _doctorRepo.GetByIdAsync(order.DoctorId.Value);
                    if (doctor != null && doctor.Commission > 0)
                    {
                        var commissionAmount = invoice.GrandTotal * (doctor.Commission / 100.0m);
                        await _commissionRepo.AddAsync(new DoctorCommission
                        {
                            DoctorId = doctor.DoctorId,
                            InvoiceId = invoice.InvoiceId,
                            CommissionAmount = commissionAmount,
                            Status = "Unpaid",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }
            
            await _invoiceRepo.UpdateAsync(invoice);
        }

        public async Task<RevenueReportStats> GetRevenueReportAsync(DateTime start, DateTime end)
        {
            var invoices = await _invoiceRepo.GetAllWithDetailsAsync();
            var filtered = invoices.Where(i => i.CreatedAt >= start && i.CreatedAt < end.AddDays(1));

            var stats = new RevenueReportStats
            {
                TotalRevenue = filtered.Sum(i => i.GrandTotal),
                TotalCollected = filtered.Where(i => i.IsPaid).Sum(i => i.GrandTotal),
                OutstandingAmount = filtered.Where(i => !i.IsPaid).Sum(i => i.GrandTotal),
                CashCollected = filtered.Where(i => i.IsPaid && i.PaymentMethod == "Cash")
                    .Sum(i => i.GrandTotal),
                UpiCollected = filtered.Where(i => i.IsPaid && i.PaymentMethod == "UPI")
                    .Sum(i => i.GrandTotal)
            };
            return stats;
        }

        public async Task VoidInvoiceAsync(int invoiceId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var invoice = await _invoiceRepo.GetByIdAsync(invoiceId);
            if (invoice == null)
                throw new InvalidOperationException("Invoice not found.");

            if (invoice.Status == "Voided")
                throw new InvalidOperationException("Invoice is already voided.");

            // Delete all associated payments
            var payments = await _paymentRepo.GetByInvoiceIdAsync(invoiceId);
            foreach (var payment in payments)
            {
                await _paymentRepo.DeleteAsync(payment.PaymentId, cancellationToken);
            }

            invoice.Status = "Voided";
            invoice.AmountPaid = 0;
            invoice.IsPaid = false;
            invoice.PaidAt = null;
            invoice.PaymentMethod = null;
            invoice.UpdatedAt = DateTime.UtcNow;
            await _invoiceRepo.UpdateAsync(invoice);

            Log.Information("Voided invoice {InvoiceId} (order {OrderId}), removed {Count} payments", invoiceId, invoice.OrderId, payments.Count());
        }

        public async Task VoidPaymentAsync(int paymentId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var payment = await _paymentRepo.GetByIdAsync(paymentId);
            if (payment == null)
                throw new InvalidOperationException("Payment not found.");

            var invoiceId = payment.InvoiceId;
            await _paymentRepo.DeleteAsync(paymentId, cancellationToken);

            // Recalculate invoice totals
            var invoice = await _invoiceRepo.GetByIdAsync(invoiceId);
            if (invoice != null)
            {
                var remainingPayments = await _paymentRepo.GetByInvoiceIdAsync(invoiceId);
                decimal paidAmount = remainingPayments.Sum(p => p.Amount);

                invoice.AmountPaid = paidAmount;
                invoice.IsPaid = paidAmount >= invoice.GrandTotal;
                invoice.Status = invoice.IsPaid ? "Paid" : (paidAmount > 0 ? "Partial" : "Pending");
                if (!invoice.IsPaid)
                {
                    invoice.PaidAt = null;
                    invoice.PaymentMethod = null;
                }
                invoice.UpdatedAt = DateTime.UtcNow;
                await _invoiceRepo.UpdateAsync(invoice);

                Log.Information("Voided payment {PaymentId} from invoice {InvoiceId}, new paid amount: {PaidAmount}", paymentId, invoiceId, paidAmount);
            }
        }
    }
}
