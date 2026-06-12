using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ITestTypeRepository _testTypeRepo;
        private readonly IRepository<Specimen> _specimenRepo;

        public OrderService(
            ITestOrderRepository orderRepo, 
            IRepository<AuditLog> auditRepo,
            ITestTypeRepository testTypeRepo,
            IRepository<Specimen> specimenRepo)
        {
            _orderRepo = orderRepo;
            _auditRepo = auditRepo;
            _testTypeRepo = testTypeRepo;
            _specimenRepo = specimenRepo;
        }

        public async Task CreateOrderAsync(TestOrder order, List<int> testTypeIds, CancellationToken cancellationToken = default)
        {
            order.OrderedAt = DateTime.UtcNow;
            await _orderRepo.AddOrderWithTestTypesAsync(order, testTypeIds, cancellationToken);

            var sampleTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var testTypeId in testTypeIds)
            {
                var testType = await _testTypeRepo.GetByIdAsync(testTypeId, cancellationToken);
                if (testType != null && !string.IsNullOrWhiteSpace(testType.SampleType))
                {
                    sampleTypes.Add(testType.SampleType.Trim());
                }
            }

            int index = 1;
            int currentYear = DateTime.UtcNow.Year;
            foreach (var sampleType in sampleTypes)
            {
                var barcode = $"QDC-SP-{currentYear}-{order.OrderId:D5}-{index}";
                var specimen = new Specimen
                {
                    OrderId = order.OrderId,
                    Barcode = barcode,
                    SampleType = sampleType,
                    CollectionTime = DateTime.UtcNow,
                    CollectedBy = "System",
                    Status = "Collected"
                };
                await _specimenRepo.AddAsync(specimen, cancellationToken);
                index++;
            }
            
            await _auditRepo.AddAsync(new AuditLog
            {
                Action = "Created",
                EntityType = "TestOrder",
                EntityId = order.OrderId,
                Timestamp = DateTime.UtcNow,
                Details = $"New test order created with {sampleTypes.Count} specimen(s)."
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
