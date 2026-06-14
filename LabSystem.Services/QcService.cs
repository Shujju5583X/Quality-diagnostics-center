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
    public class QcService
    {
        private readonly IQcRepository _qcRepo;

        public QcService(IQcRepository qcRepo)
        {
            _qcRepo = qcRepo;
        }

        public async Task<QcRun> RecordQcRunAsync(QcRun run, CancellationToken cancellationToken = default)
        {
            try
            {
                run.CreatedAt = DateTime.UtcNow;
                await _qcRepo.AddAsync(run, cancellationToken);

                // Evaluate Westgard rules
                var violations = await EvaluateWestgardRulesAsync(run, cancellationToken);
                run.Status = violations.Any(v => v.StartsWith("REJECT")) ? "Reject" :
                             violations.Any(v => v.StartsWith("WARNING")) ? "Warning" : "Pass";

                return run;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to record QC run.");
                throw;
            }
        }

        public async Task<List<string>> EvaluateWestgardRulesAsync(QcRun currentRun, CancellationToken cancellationToken = default)
        {
            var violations = new List<string>();

            // Get recent runs for the same test type and lot number (last 20 for evaluation)
            var recentRuns = await _qcRepo.GetByTestTypeAndDateRangeAsync(
                currentRun.TestTypeId,
                DateTime.UtcNow.AddDays(-30),
                DateTime.UtcNow,
                cancellationToken);

            var sameLotRuns = recentRuns
                .Where(r => r.LotNumber == currentRun.LotNumber)
                .OrderBy(r => r.RunDate)
                .ToList();

            if (sameLotRuns.Count == 0 || currentRun.SD == 0)
                return violations;

            var latest = sameLotRuns.Last();
            double zScore = (latest.MeasuredValue - latest.TargetValue) / latest.SD;

            // 1-2s: Warning - single control exceeds ±2 SD
            if (Math.Abs(zScore) > 2)
                violations.Add("WARNING: 1-2s rule violated (single control > 2 SD)");

            // 1-3s: Reject - single control exceeds ±3 SD
            if (Math.Abs(zScore) > 3)
                violations.Add("REJECT: 1-3s rule violated (single control > 3 SD)");

            // 2-2s: Reject - two consecutive controls exceed same ±2 SD
            if (sameLotRuns.Count >= 2)
            {
                var prev = sameLotRuns[sameLotRuns.Count - 2];
                double prevZ = (prev.MeasuredValue - prev.TargetValue) / prev.SD;
                if (Math.Abs(prevZ) > 2 && Math.Abs(zScore) > 2
                    && Math.Sign(prevZ) == Math.Sign(zScore))
                    violations.Add("REJECT: 2-2s rule violated (two consecutive same direction > 2 SD)");
            }

            // R-4s: Reject - range between two controls exceeds 4 SD
            if (sameLotRuns.Count >= 2)
            {
                var prev = sameLotRuns[sameLotRuns.Count - 2];
                double prevZ = (prev.MeasuredValue - prev.TargetValue) / prev.SD;
                double range = Math.Abs(zScore - prevZ);
                if (range > 4)
                    violations.Add("REJECT: R-4s rule violated (range > 4 SD)");
            }

            // 4-1s: Reject - four consecutive controls exceed same ±1 SD
            if (sameLotRuns.Count >= 4)
            {
                var last4 = sameLotRuns.Skip(Math.Max(0, sameLotRuns.Count - 4)).ToList();
                bool allAbove1 = last4.All(r => (r.MeasuredValue - r.TargetValue) / r.SD > 1);
                bool allBelow1 = last4.All(r => (r.MeasuredValue - r.TargetValue) / r.SD < -1);
                if (allAbove1 || allBelow1)
                    violations.Add("REJECT: 4-1s rule violated (four consecutive same direction > 1 SD)");
            }

            // 10x: Reject - ten consecutive controls on same side of mean
            if (sameLotRuns.Count >= 10)
            {
                var last10 = sameLotRuns.Skip(Math.Max(0, sameLotRuns.Count - 10)).ToList();
                bool allAbove = last10.All(r => r.MeasuredValue > r.TargetValue);
                bool allBelow = last10.All(r => r.MeasuredValue < r.TargetValue);
                if (allAbove || allBelow)
                    violations.Add("REJECT: 10x rule violated (ten consecutive same side of mean)");
            }

            return violations;
        }

        public async Task<IEnumerable<QcRun>> GetQcRunsAsync(int testTypeId, DateTime start, DateTime end, CancellationToken cancellationToken = default)
        {
            return await _qcRepo.GetByTestTypeAndDateRangeAsync(testTypeId, start, end, cancellationToken);
        }

        public async Task<IEnumerable<QcLot>> GetQcLotsAsync(int testTypeId, CancellationToken cancellationToken = default)
        {
            return await _qcRepo.GetActiveLotsAsync(testTypeId, cancellationToken);
        }

        public async Task<QcLot> GetLotByNumberAsync(string lotNumber, CancellationToken cancellationToken = default)
        {
            return await _qcRepo.GetLotByNumberAsync(lotNumber, cancellationToken);
        }
    }
}
