using System;
using System.Linq;
using System.Threading;
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
        private readonly ITestOrderRepository _orderRepo;
        private readonly IQCResultRepository _qcRepo;

        public ResultService(
            IResultRepository resultRepo, 
            IRepository<TestType> testTypeRepo, 
            IRepository<AuditLog> auditRepo,
            ITestOrderRepository orderRepo,
            IQCResultRepository qcRepo)
        {
            _resultRepo = resultRepo;
            _testTypeRepo = testTypeRepo;
            _auditRepo = auditRepo;
            _orderRepo = orderRepo;
            _qcRepo = qcRepo;
        }

        public async Task AddResultAsync(Result result, CancellationToken cancellationToken = default)
        {
            var testType = await _testTypeRepo.GetByIdAsync(result.TypeId, cancellationToken);
            
            // Phase 4: QC Enforcement Check
            if (testType != null)
            {
                var latestQc = await _qcRepo.GetLatestQCAsync(testType.TypeId, cancellationToken);
                if (latestQc != null && latestQc.IsOutOfRange)
                {
                    throw new InvalidOperationException($"Cannot record result for {testType.Name}. The latest Quality Control (QC) run is OUT OF RANGE (> 2SD). Please recalibrate and run a passing QC first.");
                }
            }

            var order = await _orderRepo.GetByIdAsync(result.OrderId, cancellationToken);
            
            bool isRejected = false;
            if (testType != null && order != null)
            {
                var specimen = order.Specimens?.FirstOrDefault(s => string.Equals(s.SampleType, testType.SampleType, StringComparison.OrdinalIgnoreCase));
                if (specimen != null && string.Equals(specimen.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    isRejected = true;
                }
            }

            if (isRejected)
            {
                result.Value = -999.0;
                result.IsAbnormal = false;
            }
            else if (testType != null)
            {
                var patient = order?.Patient;
                result.IsAbnormal = EvaluateIsAbnormal(result.Value, testType, patient);
            }

            result.RecordedAt = DateTime.UtcNow;
            await _resultRepo.AddAsync(result, cancellationToken);

            await _auditRepo.AddAsync(new AuditLog
            {
                Action = "Created",
                EntityType = "Result",
                EntityId = result.ResultId,
                Timestamp = DateTime.UtcNow,
                Details = isRejected 
                    ? $"Result recorded as Sample Rejected for OrderId {result.OrderId}, TypeId {result.TypeId}."
                    : $"Result added for OrderId {result.OrderId}."
            }, cancellationToken);
        }

        public async Task AmendResultAsync(int resultId, double newValue, string reason, int technicianId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("An amendment reason is required to modify a finalized result.");

            var result = await _resultRepo.GetByIdAsync(resultId, cancellationToken);
            if (result == null)
                throw new InvalidOperationException("Result not found.");

            // Do QC check again before amendment
            var testType = await _testTypeRepo.GetByIdAsync(result.TypeId, cancellationToken);
            if (testType != null)
            {
                var latestQc = await _qcRepo.GetLatestQCAsync(testType.TypeId, cancellationToken);
                if (latestQc != null && latestQc.IsOutOfRange)
                {
                    throw new InvalidOperationException($"Cannot amend result for {testType.Name}. The latest Quality Control (QC) run is OUT OF RANGE (> 2SD).");
                }
            }

            double oldValue = result.Value;
            result.Value = newValue;
            result.IsAmended = true;
            result.AmendmentReason = reason;
            result.AmendedAt = DateTime.UtcNow;

            var order = await _orderRepo.GetByIdAsync(result.OrderId, cancellationToken);
            var patient = order?.Patient;
            result.IsAbnormal = EvaluateIsAbnormal(result.Value, testType, patient);

            await _resultRepo.UpdateAsync(result, cancellationToken);

            await _auditRepo.AddAsync(new AuditLog
            {
                Action = "Amended",
                EntityType = "Result",
                EntityId = result.ResultId,
                UserId = technicianId,
                Timestamp = DateTime.UtcNow,
                Details = $"Amended ResultId {result.ResultId} from {oldValue} to {newValue}. Reason: {reason}"
            }, cancellationToken);
        }

        private int CalculateAge(DateTime? dob, DateTime relativeTo)
        {
            if (!dob.HasValue) return 30; // default age
            var birthDate = dob.Value;
            int age = relativeTo.Year - birthDate.Year;
            if (relativeTo.Month < birthDate.Month || (relativeTo.Month == birthDate.Month && relativeTo.Day < birthDate.Day))
            {
                age--;
            }
            return age < 0 ? 0 : age;
        }

        private bool EvaluateIsAbnormal(double value, TestType testType, Patient patient)
        {
            if (testType.ReferenceRanges != null && testType.ReferenceRanges.Count > 0 && patient != null)
            {
                int age = CalculateAge(patient.DateOfBirth, DateTime.UtcNow);
                string gender = patient.Gender ?? "All";

                var matchingRange = testType.ReferenceRanges.FirstOrDefault(r =>
                    (string.Equals(r.Gender, gender, StringComparison.OrdinalIgnoreCase) || string.Equals(r.Gender, "All", StringComparison.OrdinalIgnoreCase))
                    && age >= r.AgeMin && age <= r.AgeMax);

                if (matchingRange != null)
                {
                    if (matchingRange.RangeLow.HasValue && value < matchingRange.RangeLow.Value)
                        return true;
                    if (matchingRange.RangeHigh.HasValue && value > matchingRange.RangeHigh.Value)
                        return true;
                    return false;
                }
            }

            // Fallback to static range
            if (testType.ReferenceRangeLow.HasValue && value < testType.ReferenceRangeLow.Value)
                return true;
            if (testType.ReferenceRangeHigh.HasValue && value > testType.ReferenceRangeHigh.Value)
                return true;

            return false;
        }
    }
}
