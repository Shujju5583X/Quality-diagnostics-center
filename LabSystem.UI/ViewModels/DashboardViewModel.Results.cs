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
        private async Task ExecuteSaveResultsAsync(object obj)
        {
            if (SelectedOrder == null) return;

            if (SelectedOrder.StatusEnum != LabSystem.Core.Enums.OrderStatus.Pending)
            {
                MessageBox.Show("Results have already been verified and saved for this order.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Validate all inputs are numeric (bypass for rejected samples or qualitative options)
            foreach (var r in SelectedOrderResults)
            {
                if (r.ValueText == "Sample Rejected")
                {
                    continue;
                }
                if (string.IsNullOrWhiteSpace(r.ValueText))
                {
                    ResultErrorMessage = "Please enter a valid value for " + r.TestName + ".";
                    return;
                }
                double discardVal;
                if (!r.HasOptions && !double.TryParse(r.ValueText, out discardVal))
                {
                    ResultErrorMessage = "Please enter a valid numeric value for " + r.TestName + ".";
                    return;
                }
            }

            try
            {
                // Save each result
                foreach (var r in SelectedOrderResults)
                {
                    double parsedVal;
                    double? val = r.ValueText == "Sample Rejected" ? (double?)null : (double.TryParse(r.ValueText, out parsedVal) ? (double?)parsedVal : null);
                    var result = new Result
                    {
                        OrderId = SelectedOrder.OrderId,
                        TypeId = r.TypeId,
                        Value = val,
                        ValueText = r.ValueText,
                        TechnicianId = DefaultStaffId,
                        RecordedAt = DateTime.UtcNow
                    };

                    await _resultService.AddResultAsync(result);
                }

                int selectedOrderId = SelectedOrder.OrderId;

                // Update order status to Complete
                await _orderService.UpdateOrderStatusAsync(selectedOrderId, "Complete");
                Log.Information("Verified and completed order {OrderId}", selectedOrderId);

                // Reload
                await LoadDataAsync();
                
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

            if (SelectedOrder.StatusEnum != LabSystem.Core.Enums.OrderStatus.Complete)
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
                MessageBox.Show("Error generating PDF report: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteEditResultsAsync(object obj)
        {
            if (SelectedOrder == null || SelectedOrder.StatusEnum != LabSystem.Core.Enums.OrderStatus.Complete) return;

            IsResultEditMode = true;
            foreach (var ri in SelectedOrderResults)
            {
                ri.IsReadOnly = false;
                ri.IsAmendmentMode = true;
            }
            OnPropertyChanged("SelectedOrderResults");
        }

        private async Task ExecuteSaveAmendmentAsync(object obj)
        {
            if (SelectedOrder == null || SelectedOrder.StatusEnum != LabSystem.Core.Enums.OrderStatus.Complete) return;

            var reasonDialog = new Views.AmendmentReasonDialog();
            if (reasonDialog.ShowDialog() != true)
            {
                await CancelEditModeAsync();
                return;
            }

            string reason = reasonDialog.Reason;
            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("Amendment reason is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                foreach (var ri in SelectedOrderResults)
                {
                    double parsedVal;
                    double? newValue = double.TryParse(ri.ValueText, out parsedVal) ? (double?)parsedVal : null;
                    await _resultService.AmendResultAsync(ri.ResultId, newValue, ri.ValueText, reason, App.AuthenticatedStaffId);
                }

                int selectedOrderId = SelectedOrder.OrderId;
                await LoadResultsForSelectedOrderAsync();
                IsResultEditMode = false;

                MessageBox.Show("Results amended successfully.", "Amended", MessageBoxButton.OK, MessageBoxImage.Information);

                SelectedOrder = Orders.FirstOrDefault(o => o.OrderId == selectedOrderId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save amendment.");
                MessageBox.Show("Amendment failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CancelEditModeAsync()
        {
            IsResultEditMode = false;
            if (SelectedOrder != null)
            {
                await LoadResultsForSelectedOrderAsync();
            }
        }
    }
}
