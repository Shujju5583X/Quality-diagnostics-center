using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LabSystem.Core.Models;
using Serilog;
using LiveCharts;
using LiveCharts.Wpf;

namespace LabSystem.UI.ViewModels
{
    public partial class DashboardViewModel
    {
        // Quality Control Fields
        private ObservableCollection<QCResult> _qcResults = new ObservableCollection<QCResult>();
        public ObservableCollection<QCResult> QCResults
        {
            get => _qcResults;
            set { _qcResults = value; OnPropertyChanged(); }
        }

        private QCResult _selectedQcResult;
        public QCResult SelectedQcResult
        {
            get => _selectedQcResult;
            set { _selectedQcResult = value; OnPropertyChanged(); }
        }

        private TestType _selectedQcTestType;
        public TestType SelectedQcTestType
        {
            get => _selectedQcTestType;
            set { _selectedQcTestType = value; OnPropertyChanged(); }
        }

        private string _qcControlLevel = "Level 1";
        public string QcControlLevel
        {
            get => _qcControlLevel;
            set { _qcControlLevel = value; OnPropertyChanged(); }
        }

        private double? _qcExpectedMean;
        public double? QcExpectedMean
        {
            get => _qcExpectedMean;
            set { _qcExpectedMean = value; OnPropertyChanged(); }
        }

        private double? _qcStandardDeviation;
        public double? QcStandardDeviation
        {
            get => _qcStandardDeviation;
            set { _qcStandardDeviation = value; OnPropertyChanged(); }
        }

        private double? _qcMeasuredValue;
        public double? QcMeasuredValue
        {
            get => _qcMeasuredValue;
            set { _qcMeasuredValue = value; OnPropertyChanged(); }
        }

        private string _qcRemarks;
        public string QcRemarks
        {
            get => _qcRemarks;
            set { _qcRemarks = value; OnPropertyChanged(); }
        }

        public ICommand SaveQcCommand => new RelayCommand(async o => await ExecuteSaveQcAsync());
        public ICommand RefreshQcCommand => new RelayCommand(async o => await ExecuteRefreshQcAsync());

        // Patient History Fields
        private Patient _selectedHistoryPatient;
        public Patient SelectedHistoryPatient
        {
            get => _selectedHistoryPatient;
            set { _selectedHistoryPatient = value; OnPropertyChanged(); _ = LoadPatientHistoryAsync(); }
        }

        private TestType _selectedHistoryTestType;
        public TestType SelectedHistoryTestType
        {
            get => _selectedHistoryTestType;
            set { _selectedHistoryTestType = value; OnPropertyChanged(); _ = LoadPatientHistoryAsync(); }
        }

        private SeriesCollection _patientHistorySeries;
        public SeriesCollection PatientHistorySeries
        {
            get => _patientHistorySeries;
            set { _patientHistorySeries = value; OnPropertyChanged(); }
        }

        private ObservableCollection<string> _patientHistoryLabels = new ObservableCollection<string>();
        public ObservableCollection<string> PatientHistoryLabels
        {
            get => _patientHistoryLabels;
            set { _patientHistoryLabels = value; OnPropertyChanged(); }
        }

        private async Task ExecuteSaveQcAsync()
        {
            if (SelectedQcTestType == null || !QcExpectedMean.HasValue || !QcStandardDeviation.HasValue || !QcMeasuredValue.HasValue)
            {
                MessageBox.Show("Please fill in all QC details.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var qc = new QCResult
                {
                    TestTypeId = SelectedQcTestType.TypeId,
                    ControlLevel = QcControlLevel,
                    ExpectedMean = QcExpectedMean.Value,
                    StandardDeviation = QcStandardDeviation.Value,
                    MeasuredValue = QcMeasuredValue.Value,
                    RecordedAt = DateTime.UtcNow,
                    TechnicianId = this.StaffId,
                    Remarks = QcRemarks
                };

                await _qcRepo.AddAsync(qc);

                MessageBox.Show(qc.IsOutOfRange ? "QC result saved but is OUT OF RANGE (>2 SD). Testing blocked." : "QC result saved successfully.", "QC Saved", MessageBoxButton.OK, qc.IsOutOfRange ? MessageBoxImage.Warning : MessageBoxImage.Information);

                // Reset form
                QcMeasuredValue = null;
                QcRemarks = null;

                await ExecuteRefreshQcAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save QC Result.");
                MessageBox.Show("Failed to save QC Result.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteRefreshQcAsync()
        {
            try
            {
                var qcs = await _qcRepo.GetAllAsync();
                QCResults.Clear();
                foreach(var q in qcs.OrderByDescending(x => x.RecordedAt))
                {
                    // Ensure TestType is populated if not eager loaded
                    if (q.TestType == null) {
                        q.TestType = await _testTypeRepo.GetByIdAsync(q.TestTypeId);
                    }
                    QCResults.Add(q);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to refresh QC Results.");
            }
        }

        private async Task LoadPatientHistoryAsync()
        {
            if (SelectedHistoryPatient == null || SelectedHistoryTestType == null)
            {
                PatientHistorySeries = new SeriesCollection();
                PatientHistoryLabels.Clear();
                return;
            }

            try
            {
                var history = await _resultRepo.GetPatientHistoryAsync(SelectedHistoryPatient.PatientId, SelectedHistoryTestType.TypeId);
                
                if (history == null || !history.Any())
                {
                    PatientHistorySeries = new SeriesCollection();
                    PatientHistoryLabels.Clear();
                    return;
                }

                history = history.OrderBy(h => h.RecordedAt).ToList();

                var values = new ChartValues<double>();
                PatientHistoryLabels.Clear();

                foreach (var h in history)
                {
                    values.Add(h.Value);
                    PatientHistoryLabels.Add(h.RecordedAt.ToLocalTime().ToString("dd-MMM yy"));
                }

                PatientHistorySeries = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = SelectedHistoryTestType.Name,
                        Values = values,
                        PointGeometrySize = 10,
                        DataLabels = true
                    }
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load patient history.");
            }
        }

        // Logic to support Amendment from Dashboard
        private int _amendResultId;
        public int AmendResultId
        {
            get => _amendResultId;
            set { _amendResultId = value; OnPropertyChanged(); }
        }

        private double _amendNewValue;
        public double AmendNewValue
        {
            get => _amendNewValue;
            set { _amendNewValue = value; OnPropertyChanged(); }
        }

        private string _amendmentReason;
        public string AmendmentReason
        {
            get => _amendmentReason;
            set { _amendmentReason = value; OnPropertyChanged(); }
        }

        public ICommand AmendResultCommand => new RelayCommand(async o => await ExecuteAmendResultAsync());

        private async Task ExecuteAmendResultAsync()
        {
            if (AmendResultId <= 0 || string.IsNullOrWhiteSpace(AmendmentReason))
            {
                MessageBox.Show("Please select a result and provide an amendment reason.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await _resultService.AmendResultAsync(AmendResultId, AmendNewValue, AmendmentReason, this.StaffId);
                MessageBox.Show("Result amended successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                AmendmentReason = string.Empty;
                AmendResultId = 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to amend result.");
                MessageBox.Show(ex.Message, "Error Amending Result", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
