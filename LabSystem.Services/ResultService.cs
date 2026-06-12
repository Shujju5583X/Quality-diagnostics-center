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

        public async Task AddResultAsync(Result result, CancellationToken cancellationToken = default)
        {
            var testType = await _testTypeRepo.GetByIdAsync(result.TypeId, cancellationToken);
            


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
            result.CreatedAt = DateTime.UtcNow;
            result.UpdatedAt = DateTime.UtcNow;
            await _resultRepo.AddAsync(result, cancellationToken);
        }

        public async Task AmendResultAsync(int resultId, double newValue, string reason, int technicianId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("An amendment reason is required to modify a finalized result.");

            var result = await _resultRepo.GetByIdAsync(resultId, cancellationToken);
            if (result == null)
                throw new InvalidOperationException("Result not found.");

            var testType = await _testTypeRepo.GetByIdAsync(result.TypeId, cancellationToken);

            double oldValue = result.Value;
            result.Value = newValue;
            result.IsAmended = true;
            result.AmendmentReason = reason;
            result.AmendedAt = DateTime.UtcNow;
            result.UpdatedAt = DateTime.UtcNow;

            var order = await _orderRepo.GetByIdAsync(result.OrderId, cancellationToken);
            var patient = order?.Patient;
            result.IsAbnormal = EvaluateIsAbnormal(result.Value, testType, patient);

            await _resultRepo.UpdateAsync(result, cancellationToken);
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
