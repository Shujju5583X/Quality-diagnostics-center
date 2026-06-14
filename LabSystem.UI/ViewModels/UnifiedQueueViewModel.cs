using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LabSystem.Core.Enums;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.Core.Services;
using LabSystem.Data;
using Serilog;

namespace LabSystem.UI.ViewModels
{
    public class UnifiedQueueViewModel : ViewModelBase
    {
        private readonly LabDbContext _context;
        private readonly IWorkflowService _workflowService;
        private readonly IResultService _resultService;
        private readonly IRepository<TestType> _testTypeRepo;

        public ObservableCollection<UnifiedQueueItem> QueueItems { get; } = new ObservableCollection<UnifiedQueueItem>();
        public ObservableCollection<ResultInput> SelectedOrderResults { get; } = new ObservableCollection<ResultInput>();

        private UnifiedQueueItem _selectedItem;
        public UnifiedQueueItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsCompletedOrder));
                OnSelectedItemChanged();
            }
        }

        public bool IsCompletedOrder => SelectedItem?.WorkflowState == LabSystem.Core.Enums.UnifiedWorkflowState.Completed_Paid || SelectedItem?.OrderStatus == "Complete";
        public bool IsPendingOrder => !IsCompletedOrder;

        public ICommand QuickFinalizeCommand { get; }
        public ICommand AmendResultCommand { get; }

        public UnifiedQueueViewModel(LabDbContext context, IWorkflowService workflowService, IResultService resultService = null, IRepository<TestType> testTypeRepo = null)
        {
            _context = context;
            _workflowService = workflowService;
            _resultService = resultService;
            _testTypeRepo = testTypeRepo;

            QuickFinalizeCommand = new RelayCommand(async o => await ExecuteQuickFinalizeAsync(), o => SelectedItem != null);
            AmendResultCommand = new RelayCommand(async o => await ExecuteAmendResultAsync(o), o => SelectedItem != null && IsCompletedOrder);
        }

        public async Task LoadQueueAsync()
        {
            try
            {
                var items = _context.GetUnifiedQueue().ToList();
                QueueItems.Clear();
                foreach (var item in items)
                {
                    QueueItems.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load unified queue: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnSelectedItemChanged()
        {
            SelectedOrderResults.Clear();
            if (SelectedItem == null) return;

            // Load results for completed orders (for amendment)
            if (IsCompletedOrder)
            {
                _ = LoadResultsForCompletedOrderAsync();
            }
        }

        private async Task LoadResultsForCompletedOrderAsync()
        {
            if (SelectedItem == null) return;

            try
            {
                // Get the full order with results from the database
                var order = await _context.TestOrders.FindAsync(SelectedItem.OrderId);
                if (order == null) return;

                // Get results for this order
                var results = _context.Results
                    .Where(r => r.OrderId == SelectedItem.OrderId)
                    .ToList();

                SelectedOrderResults.Clear();
                foreach (var r in results)
                {
                    var testType = _testTypeRepo != null ? await _testTypeRepo.GetByIdAsync(r.TypeId) : null;
                    var ri = new ResultInput
                    {
                        TypeId = r.TypeId,
                        InputType = testType?.InputType ?? ResultInputType.Numeric,
                        TestName = testType?.Name ?? "Unknown Test",
                        Unit = testType?.Unit ?? "",
                        ValueText = r.Value == null ? "Sample Rejected" : r.Value.ToString(),
                        IsAbnormal = r.IsAbnormal,
                        IsReadOnly = true,
                        IsAmendmentMode = false
                    };
                    SelectedOrderResults.Add(ri);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load results for completed order.");
            }
        }

        private async Task ExecuteAmendResultAsync(object parameter)
        {
            if (parameter is not ResultInput ri) return;

            var reasonDialog = new Views.AmendmentReasonDialog();
            if (reasonDialog.ShowDialog() != true) return;

            string reason = reasonDialog.Reason;
            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("Amendment reason is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                double? newValue = double.TryParse(ri.ValueText, out var v) ? v : (double?)null;
                await _resultService.AmendResultAsync(ri.TypeId, newValue, ri.ValueText, reason, App.AuthenticatedStaffId);

                ri.IsAmendmentMode = false;

                var testType = _testTypeRepo != null ? await _testTypeRepo.GetByIdAsync(ri.TypeId) : null;
                ri.IsAbnormal = ReferenceRangeEvaluator.IsAbnormal(newValue, testType, null);

                MessageBox.Show("Result amended successfully.", "Amended", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to amend result.");
                MessageBox.Show($"Amendment failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteQuickFinalizeAsync()
        {
            if (SelectedItem == null) return;

            try
            {
                var results = SelectedOrderResults.Select(r => new Result
                {
                    OrderId = SelectedItem.OrderId,
                    TypeId = r.TypeId,
                    Value = double.TryParse(r.ValueText, out var v) ? v : (double?)null,
                    ValueText = r.ValueText,
                    RecordedAt = DateTime.UtcNow
                }).ToList();

                // Use the authenticated technician ID
                await _workflowService.QuickFinalizeAsync(SelectedItem.OrderId, results, App.AuthenticatedStaffId, "Cash");

                MessageBox.Show("Order finalized successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadQueueAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to execute quick finalize: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
