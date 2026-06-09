using System;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Services
{
    public class ResultService : IResultService
    {
        private readonly IResultRepository _resultRepo;
        private readonly IRepository<TestType> _testTypeRepo;
        private readonly IRepository<AuditLog> _auditRepo;

        public ResultService(IResultRepository resultRepo, IRepository<TestType> testTypeRepo, IRepository<AuditLog> auditRepo)
        {
            _resultRepo = resultRepo;
            _testTypeRepo = testTypeRepo;
            _auditRepo = auditRepo;
        }

        public async Task AddResultAsync(Result result)
        {
            var testType = await _testTypeRepo.GetByIdAsync(result.TypeId);
            if (testType != null)
            {
                if (testType.ReferenceRangeLow.HasValue && result.Value < testType.ReferenceRangeLow.Value)
                    result.IsAbnormal = true;
                else if (testType.ReferenceRangeHigh.HasValue && result.Value > testType.ReferenceRangeHigh.Value)
                    result.IsAbnormal = true;
                else
                    result.IsAbnormal = false;
            }

            result.RecordedAt = DateTime.UtcNow.ToString("O");
            await _resultRepo.AddAsync(result);

            await _auditRepo.AddAsync(new AuditLog
            {
                Action = "Created",
                EntityType = "Result",
                Timestamp = DateTime.UtcNow,
                Details = $"Result added for OrderId {result.OrderId}."
            });
        }
    }
}
