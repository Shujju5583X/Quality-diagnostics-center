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

        private async Task ExecuteVoidInvoiceAsync()
        {
            if (SelectedInvoice == null)
            {
                MessageBox.Show("Please select an invoice to void.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedInvoice.Status == "Voided")
            {
                MessageBox.Show("This invoice is already voided.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var patientName = SelectedInvoice.Order != null && SelectedInvoice.Order.Patient != null
                ? SelectedInvoice.Order.Patient.FullName : "Unknown";

            var message = "Are you sure you want to void Invoice #" + SelectedInvoice.InvoiceId + " for patient '" + patientName + "'?";
            if (SelectedInvoice.AmountPaid > 0)
            {
                message += "\n\nThis will remove " + SelectedInvoice.AmountPaid.ToString("N2") + " in recorded payments.";
            }

            var dialogResult = MessageBox.Show(message, "Confirm Void Invoice", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (dialogResult != MessageBoxResult.Yes) return;

            try
            {
                int invoiceId = SelectedInvoice.InvoiceId;
                await _billingService.VoidInvoiceAsync(invoiceId);
                Log.Information("Voided invoice {InvoiceId}", invoiceId);
                await LoadInvoicesAsync();
                MessageBox.Show("Invoice voided successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to void invoice.");
                MessageBox.Show("Error voiding invoice: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteVoidPaymentAsync(object obj)
        {
            int paymentId = 0;
            var payment = obj as Payment;
            if (payment != null)
            {
                paymentId = payment.PaymentId;
            }
            else if (obj is int)
            {
                paymentId = (int)obj;
            }

            if (paymentId <= 0)
            {
                MessageBox.Show("Please select a payment to void.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialogResult = MessageBox.Show(
                "Are you sure you want to void this payment?\n\nThe invoice status will be recalculated.",
                "Confirm Void Payment", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (dialogResult != MessageBoxResult.Yes) return;

            try
            {
                await _billingService.VoidPaymentAsync(paymentId);
                Log.Information("Voided payment {PaymentId}", paymentId);
                await LoadInvoicesAsync();
                MessageBox.Show("Payment voided successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to void payment.");
                MessageBox.Show("Error voiding payment: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
