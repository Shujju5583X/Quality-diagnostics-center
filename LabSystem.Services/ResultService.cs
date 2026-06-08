using System;
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

        public void AddResult(Result result)
        {
            var testType = _testTypeRepo.GetById(result.TypeId);
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
            _resultRepo.Add(result);

            _auditRepo.Add(new AuditLog
            {
                Action = "Created",
                EntityType = "Result",
                Timestamp = DateTime.UtcNow.ToString("O"),
                Details = $"Result added for OrderId {result.OrderId}."
            });
        }
    }
}
