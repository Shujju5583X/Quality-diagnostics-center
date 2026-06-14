using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.Core.Enums;

namespace LabSystem.Services
{
    public class OrderService : IOrderService
    {
        private readonly ITestOrderRepository _orderRepo;
        private readonly ITestTypeRepository _testTypeRepo;
        private readonly IRepository<Specimen> _specimenRepo;

        public OrderService(
            ITestOrderRepository orderRepo, 
            ITestTypeRepository testTypeRepo,
            IRepository<Specimen> specimenRepo)
        {
            _orderRepo = orderRepo;
            _testTypeRepo = testTypeRepo;
            _specimenRepo = specimenRepo;
        }

        public async Task CreateOrderAsync(TestOrder order, List<int> testTypeIds, CancellationToken cancellationToken = default)
        {
            order.OrderedAt = DateTime.UtcNow;
            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
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
                    StatusEnum = SpecimenStatus.Collected
                };
                await _specimenRepo.AddAsync(specimen, cancellationToken);
                index++;
            }
        }

        public async Task UpdateOrderStatusAsync(int orderId, string status, CancellationToken cancellationToken = default)
        {
            var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);
            if (order != null)
            {
                order.Status = status;
                order.UpdatedAt = DateTime.UtcNow;
                await _orderRepo.UpdateAsync(order, cancellationToken);
            }
        }
    }
}
