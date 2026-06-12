using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LabSystem.Core.Models;
using Serilog;

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



    }
}
