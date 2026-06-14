using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.Services;
using Serilog;

namespace LabSystem.UI.ViewModels
{
    public class QcViewModel : ViewModelBase
    {
        private readonly QcService _qcService;
        private readonly IRepository<TestType> _testTypeRepo;

        public ObservableCollection<QcRun> QcRuns { get; } = new ObservableCollection<QcRun>();
        public ObservableCollection<QcLot> QcLots { get; } = new ObservableCollection<QcLot>();
        public ObservableCollection<TestType> TestTypes { get; } = new ObservableCollection<TestType>();
        public ObservableCollection<string> Violations { get; } = new ObservableCollection<string>();

        private TestType _selectedTestType;
        public TestType SelectedTestType
        {
            get => _selectedTestType;
            set { _selectedTestType = value; OnPropertyChanged(); _ = LoadQcLotsAsync(); }
        }

        private QcLot _selectedLot;
        public QcLot SelectedLot
        {
            get => _selectedLot;
            set { _selectedLot = value; OnPropertyChanged(); }
        }

        private DateTime _startDate = DateTime.Today.AddMonths(-1);
        public DateTime StartDate
        {
            get => _startDate;
            set { _startDate = value; OnPropertyChanged(); }
        }

        private DateTime _endDate = DateTime.Today;
        public DateTime EndDate
        {
            get => _endDate;
            set { _endDate = value; OnPropertyChanged(); }
        }

        private string _controlName;
        public string ControlName
        {
            get => _controlName;
            set { _controlName = value; OnPropertyChanged(); }
        }

        private string _measuredValue;
        public string MeasuredValue
        {
            get => _measuredValue;
            set { _measuredValue = value; OnPropertyChanged(); }
        }

        private string _lotNumber;
        public string LotNumber
        {
            get => _lotNumber;
            set { _lotNumber = value; OnPropertyChanged(); }
        }

        private string _targetValue;
        public string TargetValue
        {
            get => _targetValue;
            set { _targetValue = value; OnPropertyChanged(); }
        }

        private string _sdValue;
        public string SdValue
        {
            get => _sdValue;
            set { _sdValue = value; OnPropertyChanged(); }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public ICommand LoadQcRunsCommand { get; }
        public ICommand RecordQcRunCommand { get; }

        public QcViewModel(QcService qcService, IRepository<TestType> testTypeRepo)
        {
            _qcService = qcService;
            _testTypeRepo = testTypeRepo;

            LoadQcRunsCommand = new AsyncRelayCommand(async o => await ExecuteLoadQcRunsAsync());
            RecordQcRunCommand = new AsyncRelayCommand(async o => await ExecuteRecordQcRunAsync());

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                var testTypes = await _testTypeRepo.GetAllAsync();
                TestTypes.Clear();
                foreach (var tt in testTypes.Where(t => t.IsActive).OrderBy(t => t.Name))
                {
                    TestTypes.Add(tt);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load test types for QC.");
            }
        }

        private async Task LoadQcLotsAsync()
        {
            if (SelectedTestType == null) return;

            try
            {
                QcLots.Clear();
                var lots = await _qcService.GetQcLotsAsync(SelectedTestType.TypeId);
                foreach (var lot in lots)
                {
                    QcLots.Add(lot);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load QC lots.");
            }
        }

        private async Task ExecuteLoadQcRunsAsync()
        {
            if (SelectedTestType == null)
            {
                MessageBox.Show("Please select a test type.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                QcRuns.Clear();
                Violations.Clear();
                var runs = await _qcService.GetQcRunsAsync(SelectedTestType.TypeId, StartDate, EndDate);
                foreach (var run in runs)
                {
                    // Evaluate status for each run
                    var violations = await _qcService.EvaluateWestgardRulesAsync(run);
                    run.Status = violations.Any(v => v.StartsWith("REJECT")) ? "Reject" :
                                 violations.Any(v => v.StartsWith("WARNING")) ? "Warning" : "Pass";
                    QcRuns.Add(run);
                }
                StatusMessage = $"Loaded {QcRuns.Count} QC runs.";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load QC runs.");
                MessageBox.Show($"Failed to load QC runs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteRecordQcRunAsync()
        {
            if (SelectedTestType == null)
            {
                MessageBox.Show("Please select a test type.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(ControlName))
            {
                MessageBox.Show("Please enter a control name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(MeasuredValue, out double measured))
            {
                MessageBox.Show("Please enter a valid measured value.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double target = 0;
            double sd = 0;
            string lotNum = LotNumber;

            // If a lot is selected, use its values
            if (SelectedLot != null)
            {
                target = SelectedLot.TargetValue;
                sd = SelectedLot.SD;
                lotNum = SelectedLot.LotNumber;
            }
            else
            {
                // Manual entry
                if (!double.TryParse(TargetValue, out target))
                {
                    MessageBox.Show("Please enter a valid target value or select a lot.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!double.TryParse(SdValue, out sd) || sd == 0)
                {
                    MessageBox.Show("Please enter a valid SD value (non-zero).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(lotNum))
                {
                    MessageBox.Show("Please enter a lot number.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            try
            {
                var run = new QcRun
                {
                    TestTypeId = SelectedTestType.TypeId,
                    ControlName = ControlName,
                    RunDate = DateTime.UtcNow,
                    MeasuredValue = measured,
                    LotNumber = lotNum,
                    TargetValue = target,
                    SD = sd
                };

                var result = await _qcService.RecordQcRunAsync(run);
                Violations.Clear();

                var violations = await _qcService.EvaluateWestgardRulesAsync(result);
                foreach (var v in violations)
                {
                    Violations.Add(v);
                }

                StatusMessage = $"QC run recorded. Status: {result.Status}";
                MessageBox.Show($"QC run recorded successfully.\nStatus: {result.Status}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Reload runs
                await ExecuteLoadQcRunsAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to record QC run.");
                MessageBox.Show($"Failed to record QC run: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
