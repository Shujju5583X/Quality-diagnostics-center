using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.Data;

namespace LabSystem.Services
{
    public class WorkflowService : IWorkflowService
    {
        private readonly LabDbContext _context;
        private readonly IResultService _resultService;
        private readonly IBillingService _billingService;
        private readonly IPdfReportService _reportService;

        public WorkflowService(LabDbContext context, IResultService resultService, IBillingService billingService, IPdfReportService reportService)
        {
            _context = context;
            _resultService = resultService;
            _billingService = billingService;
            _reportService = reportService;
        }

        public async Task QuickFinalizeAsync(int orderId, List<Result> results, int technicianId, string paymentMethod, CancellationToken cancellationToken = default)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Save results logic
                    foreach (var result in results)
                    {
                        result.RecordedAt = DateTime.UtcNow;
                        result.TechnicianId = technicianId;
                        await _resultService.AddResultAsync(result);
                    }

                    // 2. Invoice logic (generate and pay)
                    var invoice = await _billingService.GenerateInvoiceAsync(orderId);
                    if (invoice != null && !invoice.IsPaid)
                    {
                        // Assume full payment
                        await _billingService.AddPaymentAsync(invoice.InvoiceId, invoice.TotalAmount, paymentMethod);
                    }

                    // 3. Document generation
                    var order = await _context.TestOrders.FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);
                    if (order != null)
                    {
                        await _reportService.GenerateReportAsync(order, true, cancellationToken);
                    }
                    
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
}
