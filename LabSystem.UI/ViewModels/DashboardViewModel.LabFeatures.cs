using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LabSystem.Core.Models;
using LabSystem.Core.Services;
using Serilog;

namespace LabSystem.UI.ViewModels
{
    public partial class DashboardViewModel
    {
        // Specimen rejection check for the selected order
        public bool IsSelectedOrderSpecimenRejected
        {
            get
            {
                if (SelectedOrder == null || SelectedOrder.Specimens == null)
                    return false;
                return SelectedOrder.Specimens.Any(s => string.Equals(s.Status, "Rejected", StringComparison.OrdinalIgnoreCase));
            }
        }

        // Test Panel selection
        private TestPanel _selectedTestPanel;
        public ObservableCollection<TestPanel> TestPanels { get; } = new ObservableCollection<TestPanel>();

        public TestPanel SelectedTestPanel
        {
            get => _selectedTestPanel;
            set
            {
                _selectedTestPanel = value;
                OnPropertyChanged();
                OnTestPanelSelected(value);
            }
        }

        // ReferredBy autocomplete — populated from distinct historical values in the DB
        public ObservableCollection<string> ReferredByHistory { get; } = new ObservableCollection<string>();

        private async Task LoadReferredByHistoryAsync()
        {
            try
            {
                var orders = await _orderRepo.GetAllAsync();
                var distinctReferrals = orders
                    .Where(o => !string.IsNullOrWhiteSpace(o.ReferredBy))
                    .Select(o => o.ReferredBy.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s)
                    .ToList();

                ReferredByHistory.Clear();
                // Always include SELF at top
                if (!distinctReferrals.Any(s => s.Equals("SELF", StringComparison.OrdinalIgnoreCase)))
                    ReferredByHistory.Add("SELF");
                foreach (var r in distinctReferrals)
                    ReferredByHistory.Add(r);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load ReferredBy history.");
            }
        }

        private void OnTestPanelSelected(TestPanel panel)
        {
            if (panel == null) return;

            // Unselect all test types
            foreach (var t in TestTypes)
            {
                t.IsSelected = false;
            }

            // Select matching test types in panel
            if (panel.TestTypes != null)
            {
                var panelTestTypeIds = new HashSet<int>(panel.TestTypes.Select(t => t.TypeId));
                foreach (var t in TestTypes)
                {
                    if (panelTestTypeIds.Contains(t.TypeId))
                    {
                        t.IsSelected = true;
                    }
                }
            }
        }

        private void EvaluatePatientReferenceRange(ResultInput ri, TestType testType, Patient patient)
        {
            var matchingRange = ReferenceRangeEvaluator.FindMatchingRange(testType, patient);
            if (matchingRange != null)
            {
                ri.Low = matchingRange.RangeLow;
                ri.High = matchingRange.RangeHigh;
            }
            else
            {
                ri.Low = testType?.ReferenceRangeLow;
                ri.High = testType?.ReferenceRangeHigh;
            }
        }
    }
}
