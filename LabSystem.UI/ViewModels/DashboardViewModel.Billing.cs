using System;
using System.Threading.Tasks;
using System.Windows;
using Serilog;

namespace LabSystem.UI.ViewModels
{
    public partial class DashboardViewModel
    {
        private async Task ExecuteGenerateBillAsync(object obj)
        {
            if (SelectedOrder == null)
            {
                MessageBox.Show("Please select an order from the list.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var invoice = await _billingService.GetInvoiceForOrderAsync(SelectedOrder.OrderId);
                if (invoice == null)
                {
                    invoice = await _billingService.GenerateInvoiceAsync(SelectedOrder.OrderId);
                    invoice = await _billingService.GetInvoiceForOrderAsync(SelectedOrder.OrderId);
                }

                await LoadDataAsync(); // Always refresh to ensure it shows up

                if (invoice != null)
                {
                    var previewWindow = new Views.InvoicePreviewWindow(invoice, _reportService);
                    previewWindow.Owner = Application.Current.MainWindow;
                    previewWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to generate bill.");
                MessageBox.Show("Error generating bill: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
