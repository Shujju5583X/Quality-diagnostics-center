using System;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Services
{
    public class OrderService : IOrderService
    {
        private readonly ITestOrderRepository _orderRepo;
        private readonly IRepository<AuditLog> _auditRepo;

        public OrderService(ITestOrderRepository orderRepo, IRepository<AuditLog> auditRepo)
        {
            _orderRepo = orderRepo;
            _auditRepo = auditRepo;
        }

        public async Task CreateOrderAsync(TestOrder order, CancellationToken cancellationToken = default)
        {
            order.OrderedAt = DateTime.UtcNow.ToString("O");
            await _orderRepo.AddAsync(order, cancellationToken);
            
            await _auditRepo.AddAsync(new AuditLog
            {
                Action = "Created",
                EntityType = "TestOrder",
                Timestamp = DateTime.UtcNow,
                Details = "New test order created."
            }, cancellationToken);
        }

        public async Task UpdateOrderStatusAsync(int orderId, string status, CancellationToken cancellationToken = default)
        {
            var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);
            if (order != null)
            {
                order.Status = status;
                await _orderRepo.UpdateAsync(order, cancellationToken);
                
                await _auditRepo.AddAsync(new AuditLog
                {
                    Action = "Updated",
                    EntityType = "TestOrder",
                    EntityId = orderId,
                    Timestamp = DateTime.UtcNow,
                    Details = $"Order status updated to {status}."
                }, cancellationToken);
            }
        }
    }
}
