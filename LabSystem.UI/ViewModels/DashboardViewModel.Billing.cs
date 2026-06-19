using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Serilog;

namespace LabSystem.UI.ViewModels
{
    public partial class DashboardViewModel
    {
        private async Task LoadInvoicesAsync()
        {
            try
            {
                Invoices.Clear();
                var invoices = await _billingService.GetAllInvoicesAsync();
                foreach (var inv in invoices.OrderByDescending(i => i.InvoiceId))
                {
                    Invoices.Add(inv);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load invoices.");
            }
        }

        private async Task ExecuteAddPaymentAsync(string paymentMethod)
        {
            try
            {
                if (SelectedInvoice == null || SelectedInvoice.IsPaid)
                {
                    MessageBox.Show("Please select an unpaid invoice.", "No Invoice Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal amountToPay = PaymentAmount;
                if (amountToPay <= 0)
                {
                    amountToPay = SelectedInvoice.DueAmount;
                }

                if (amountToPay <= 0)
                {
                    MessageBox.Show("Please enter a valid payment amount.", "Invalid Amount", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (amountToPay > SelectedInvoice.DueAmount)
                {
                    MessageBox.Show("Amount exceeds due balance of \u20B9" + SelectedInvoice.DueAmount.ToString("N2") + ".", "Invalid Amount", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var invoiceId = SelectedInvoice.InvoiceId;
                await _billingService.AddPaymentAsync(invoiceId, amountToPay, paymentMethod);
                MessageBox.Show("Payment of \u20B9" + amountToPay.ToString("N2") + " recorded via " + paymentMethod + ".", "Payment Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                PaymentAmount = 0;
                await LoadInvoicesAsync();
                SelectedInvoice = Invoices.FirstOrDefault(i => i.InvoiceId == invoiceId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to process payment.");
                MessageBox.Show("Payment failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteApplyDiscountTaxAsync()
        {
            try
            {
                if (SelectedInvoice == null || SelectedInvoice.IsPaid)
                {
                    MessageBox.Show("Please select an unpaid invoice.", "No Invoice Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var invoiceId = SelectedInvoice.InvoiceId;
                await _billingService.UpdateInvoiceFinancialsAsync(invoiceId, DiscountAmount, TaxAmount);
                MessageBox.Show("Discount/tax applied.", "Updated", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadInvoicesAsync();
                SelectedInvoice = Invoices.FirstOrDefault(i => i.InvoiceId == invoiceId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to apply discount/tax.");
                MessageBox.Show("Failed to apply discount/tax: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteGenerateRevenueReportAsync()
        {
            try
            {
                RevenueStats = await _billingService.GetRevenueReportAsync(ReportStartDate, ReportEndDate);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to generate revenue report.");
                MessageBox.Show("Failed to generate revenue report: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
