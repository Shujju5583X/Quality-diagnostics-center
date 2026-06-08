using System;
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

        public void CreateOrder(TestOrder order)
        {
            order.OrderedAt = DateTime.UtcNow.ToString("O");
            _orderRepo.Add(order);
            
            _auditRepo.Add(new AuditLog
            {
                Action = "Created",
                EntityType = "TestOrder",
                Timestamp = DateTime.UtcNow.ToString("O"),
                Details = "New test order created."
            });
        }

        public void UpdateOrderStatus(int orderId, string status)
        {
            var order = _orderRepo.GetById(orderId);
            if (order != null)
            {
                order.Status = status;
                _orderRepo.Update(order);
                
                _auditRepo.Add(new AuditLog
                {
                    Action = "Updated",
                    EntityType = "TestOrder",
                    EntityId = orderId,
                    Timestamp = DateTime.UtcNow.ToString("O"),
                    Details = $"Order status updated to {status}."
                });
            }
        }
    }
}
