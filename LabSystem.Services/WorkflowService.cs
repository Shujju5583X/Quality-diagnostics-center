using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using Serilog;

namespace LabSystem.Services
{
    public class WorkflowService : IWorkflowService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITestOrderRepository _orderRepo;
        private readonly IResultService _resultService;
        private readonly IBillingService _billingService;
        private readonly IPdfReportService _reportService;

        public WorkflowService(IUnitOfWork unitOfWork, ITestOrderRepository orderRepo, IResultService resultService, IBillingService billingService, IPdfReportService reportService)
        {
            _unitOfWork = unitOfWork;
            _orderRepo = orderRepo;
            _resultService = resultService;
            _billingService = billingService;
            _reportService = reportService;
        }

        public async Task QuickFinalizeAsync(int orderId, List<Result> results, int technicianId, string paymentMethod, CancellationToken cancellationToken = default)
        {
            TestOrder order = null;

            // 1. Database transaction: results + billing only
            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    foreach (var result in results)
                    {
                        result.RecordedAt = DateTime.UtcNow;
                        result.TechnicianId = technicianId;
                        await _resultService.AddResultAsync(result);
                    }

                    var invoice = await _billingService.GenerateInvoiceAsync(orderId);
                    if (invoice != null && !invoice.IsPaid)
                    {
                        await _billingService.AddPaymentAsync(invoice.InvoiceId, invoice.TotalAmount, paymentMethod);
                    }

                    order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }

            // 2. PDF generation AFTER commit (outside transaction)
            if (order != null)
            {
                try
                {
                    await _reportService.GenerateReportAsync(order, true, cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "PDF generation failed after successful order finalization for OrderId={OrderId}.", orderId);
                }
            }
        }
    }
}
