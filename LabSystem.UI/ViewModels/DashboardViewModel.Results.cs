using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LabSystem.Core.Models;
using Serilog;

namespace LabSystem.UI.ViewModels
{
    public partial class DashboardViewModel
    {
        public string ResultErrorMessage
        {
            get => _resultErrorMessage;
            set { _resultErrorMessage = value; OnPropertyChanged(); }
        }

        private async Task ExecuteSaveResultsAsync(object obj)
        {
            if (SelectedOrder == null) return;

            if (SelectedOrder.Status != "Pending")
            {
                MessageBox.Show("Results have already been verified and saved for this order.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Validate all inputs are numeric (bypass for rejected samples)
            foreach (var r in SelectedOrderResults)
            {
                if (r.ValueText == "Sample Rejected")
                {
                    continue;
                }
                if (string.IsNullOrWhiteSpace(r.ValueText) || !double.TryParse(r.ValueText, out _))
                {
                    ResultErrorMessage = $"Please enter a valid numeric value for {r.TestName}.";
                    return;
                }
            }

            try
            {
                // Save each result
                foreach (var r in SelectedOrderResults)
                {
                    double val = r.ValueText == "Sample Rejected" ? -999.0 : double.Parse(r.ValueText);
                    var result = new Result
                    {
                        OrderId = SelectedOrder.OrderId,
                        TypeId = r.TypeId,
                        Value = val,
                        TechnicianId = StaffId
                    };

                    await _resultService.AddResultAsync(result);
                }

                int selectedOrderId = SelectedOrder.OrderId;

                // Update order status to Complete
                await _orderService.UpdateOrderStatusAsync(selectedOrderId, "Complete");
                Log.Information("Verified and completed order {OrderId}", selectedOrderId);


                // Reload
                await LoadDataAsync();
                // Refresh abnormal count since we just saved new results
                await RefreshAbnormalCountAsync();
                CalculateDashboardStatsFromLoadedData();
                
                // Select the order again to refresh results grid as read-only
                SelectedOrder = Orders.FirstOrDefault(o => o.OrderId == selectedOrderId);
                
                MessageBox.Show("Results verified and saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Generate and open the PDF report immediately
                try
                {
                    if (SelectedOrder != null)
                    {
                        var previewWindow = new Views.PdfPreviewWindow(SelectedOrder, _reportService);
                        previewWindow.ShowDialog();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to automatically generate PDF report after saving results.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save results.");
                MessageBox.Show("Error saving results to database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteGenerateReport(object obj)
        {
            if (SelectedOrder == null)
            {
                MessageBox.Show("Please select an order from the list.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedOrder.Status != "Complete")
            {
                MessageBox.Show("Reports can only be generated for Complete orders.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var previewWindow = new Views.PdfPreviewWindow(SelectedOrder, _reportService);
                previewWindow.Owner = Application.Current.MainWindow;
                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to generate report.");
                MessageBox.Show($"Error generating PDF report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
