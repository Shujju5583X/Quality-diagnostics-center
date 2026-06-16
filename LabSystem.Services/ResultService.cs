using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.Core.Services;

namespace LabSystem.Services
{
    public class ResultService : IResultService
    {
        private readonly IResultRepository _resultRepo;
        private readonly IRepository<TestType> _testTypeRepo;
        private readonly ITestOrderRepository _orderRepo;

        public ResultService(
            IResultRepository resultRepo, 
            IRepository<TestType> testTypeRepo,
            ITestOrderRepository orderRepo)
        {
            _resultRepo = resultRepo;
            _testTypeRepo = testTypeRepo;
            _orderRepo = orderRepo;
        }

        public async Task AddResultAsync(Result result, CancellationToken cancellationToken = default(CancellationToken))
        {
            var testType = await _testTypeRepo.GetByIdAsync(result.TypeId, cancellationToken);
            
            var order = await _orderRepo.GetByIdAsync(result.OrderId, cancellationToken);

            if (testType != null)
            {
                var patient = order != null ? order.Patient : null;
                result.IsAbnormal = EvaluateIsAbnormal(result.Value, testType, patient);
            }

            result.RecordedAt = DateTime.UtcNow;
            result.CreatedAt = DateTime.UtcNow;
            result.UpdatedAt = DateTime.UtcNow;
            try
            {
                await _resultRepo.AddAsync(result, cancellationToken);
            }
            catch (Exception ex)
            {
                string exStr = ex.ToString();
                if (exStr.Contains("FOREIGN KEY") || exStr.Contains("constraint") || exStr.Contains("Constraint"))
                {
                    throw new InvalidOperationException("Default staff record (ID=1) not found. Please ensure the database has been seeded correctly.", ex);
                }
                throw;
            }
        }

        public async Task AmendResultAsync(int resultId, double? newValue, string valueText, string reason, int technicianId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("An amendment reason is required to modify a finalized result.");

            var result = await _resultRepo.GetByIdAsync(resultId, cancellationToken);
            if (result == null)
                throw new InvalidOperationException("Result not found.");

            var testType = await _testTypeRepo.GetByIdAsync(result.TypeId, cancellationToken);

            double? oldValue = result.Value;
            result.Value = newValue;
            result.ValueText = valueText;
            result.IsAmended = true;
            result.AmendmentReason = reason;
            result.AmendedAt = DateTime.UtcNow;
            result.UpdatedAt = DateTime.UtcNow;

            var order = await _orderRepo.GetByIdAsync(result.OrderId, cancellationToken);
            var patient = order != null ? order.Patient : null;
            result.IsAbnormal = EvaluateIsAbnormal(result.Value, testType, patient);

            await _resultRepo.UpdateAsync(result, cancellationToken);
        }

        private bool EvaluateIsAbnormal(double? value, TestType testType, Patient patient)
        {
            return ReferenceRangeEvaluator.IsAbnormal(value, testType, patient);
        }
    }
}
