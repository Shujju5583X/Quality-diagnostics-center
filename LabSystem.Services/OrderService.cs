using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using Serilog;

namespace LabSystem.Services
{
    public class OrderService : IOrderService
    {
        private readonly ITestOrderRepository _orderRepo;
        private readonly ITestTypeRepository _testTypeRepo;
        private readonly IStaffRepository _staffRepo;

        public OrderService(
            ITestOrderRepository orderRepo, 
            ITestTypeRepository testTypeRepo,
            IStaffRepository staffRepo)
        {
            _orderRepo = orderRepo;
            _testTypeRepo = testTypeRepo;
            _staffRepo = staffRepo;
        }

        public async Task CreateOrderAsync(TestOrder order, List<int> testTypeIds, int operatorStaffId = 1, CancellationToken cancellationToken = default(CancellationToken))
        {
            order.OrderedAt = DateTime.UtcNow;
            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            await _orderRepo.AddOrderWithTestTypesAsync(order, testTypeIds, cancellationToken);
        }

        public async Task UpdateOrderStatusAsync(int orderId, string status, CancellationToken cancellationToken = default(CancellationToken))
        {
            var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);
            if (order != null)
            {
                order.Status = status;
                order.UpdatedAt = DateTime.UtcNow;
                await _orderRepo.UpdateAsync(order, cancellationToken);
            }
        }

        public async Task UpdateOrderAsync(int orderId, string notes, string referredBy, CancellationToken cancellationToken = default(CancellationToken))
        {
            var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);
            if (order == null)
                throw new InvalidOperationException("Order not found.");

            order.Notes = notes ?? "";
            order.ReferredBy = referredBy ?? "SELF";
            order.UpdatedAt = DateTime.UtcNow;
            await _orderRepo.UpdateAsync(order, cancellationToken);
            Log.Information("Updated order {OrderId}: Notes='{Notes}', ReferredBy='{ReferredBy}'", orderId, notes, referredBy);
        }

        public async Task VoidOrderAsync(int orderId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);
            if (order == null)
                throw new InvalidOperationException("Order not found.");

            if (order.StatusEnum == Core.Enums.OrderStatus.Voided)
                throw new InvalidOperationException("Order is already voided.");

            order.Status = Core.Enums.OrderStatus.Voided.ToString();
            order.UpdatedAt = DateTime.UtcNow;
            await _orderRepo.UpdateAsync(order, cancellationToken);
            Log.Information("Voided order {OrderId}", orderId);
        }
    }
}
