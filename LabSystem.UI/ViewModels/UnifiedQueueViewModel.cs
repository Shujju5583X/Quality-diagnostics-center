using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.Data;

namespace LabSystem.UI.ViewModels
{
    public class UnifiedQueueViewModel : ViewModelBase
    {
        private readonly LabDbContext _context;
        private readonly IWorkflowService _workflowService;

        public ObservableCollection<UnifiedQueueItem> QueueItems { get; } = new ObservableCollection<UnifiedQueueItem>();

        private UnifiedQueueItem _selectedItem;
        public UnifiedQueueItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged();
                OnSelectedItemChanged();
            }
        }

        // Sub-view collections for the detail pane
        public ObservableCollection<ResultInput> SelectedOrderResults { get; } = new ObservableCollection<ResultInput>();
        
        // Command
        public ICommand QuickFinalizeCommand { get; }

        public UnifiedQueueViewModel(LabDbContext context, IWorkflowService workflowService)
        {
            _context = context;
            _workflowService = workflowService;

            QuickFinalizeCommand = new RelayCommand(async o => await ExecuteQuickFinalizeAsync(), o => SelectedItem != null);
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

            // Normally we'd load test types for this order and setup the ResultInput models.
            // Simplified for brevity, as we are mainly concerned with the architecture.
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
