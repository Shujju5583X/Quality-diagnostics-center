using System;
using System.Threading.Tasks;
using System.Windows;
using LabSystem.Core.Models;
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
                MessageBox.Show($"Error generating bill: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteAddPaymentAsync(string paymentMethod)
        {
            if (SelectedInvoice == null)
            {
                MessageBox.Show("Please select an invoice.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedInvoice.IsPaid)
            {
                MessageBox.Show("This invoice is already fully paid.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string amountStr = RejectionDialog.Show("Add Payment", $"Enter amount to pay via {paymentMethod}:");
            if (string.IsNullOrWhiteSpace(amountStr)) return;
            
            if (!decimal.TryParse(amountStr, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Invalid amount entered.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                await _billingService.AddPaymentAsync(SelectedInvoice.InvoiceId, amount, paymentMethod);
                Log.Information("Added payment {Amount} to invoice {InvoiceId} via {PaymentMethod}", amount, SelectedInvoice.InvoiceId, paymentMethod);

                MessageBox.Show($"Payment of Rs.{amount} via {paymentMethod} added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadDataAsync(); // Reload invoices
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add payment to invoice.");
                MessageBox.Show("Error updating invoice.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private async Task ExecuteBackupAsync(object obj)
        {
            try
            {
                await _backupService.BackupNowAsync();
                
                // Reclaim memory after CloseXML + SQLite heavy operation
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                MessageBox.Show("Database (SQLite) and technician-friendly report (Excel) backed up successfully to the backups directory!", "Backup Completed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Database backup failed.");
                MessageBox.Show("Failed to complete database backup. Check logs for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public static class RejectionDialog
    {
        public static string Show(string title, string prompt)
        {
            var window = new Window
            {
                Title = title,
                Width = 400,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow
            };

            var stack = new System.Windows.Controls.StackPanel { Margin = new Thickness(15) };
            
            var lbl = new System.Windows.Controls.TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 10), FontWeight = FontWeights.Bold };
            stack.Children.Add(lbl);

            var txt = new System.Windows.Controls.TextBox { Height = 25, Margin = new Thickness(0, 0, 0, 15) };
            stack.Children.Add(txt);

            var btnStack = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnOk = new System.Windows.Controls.Button { Content = "OK", Width = 75, IsDefault = true, Margin = new Thickness(0, 0, 10, 0) };
            var btnCancel = new System.Windows.Controls.Button { Content = "Cancel", Width = 75, IsCancel = true };

            btnOk.Click += (s, e) => { window.DialogResult = true; window.Close(); };
            btnCancel.Click += (s, e) => { window.DialogResult = false; window.Close(); };

            btnStack.Children.Add(btnOk);
            btnStack.Children.Add(btnCancel);
            stack.Children.Add(btnStack);

            window.Content = stack;
            if (window.ShowDialog() == true)
            {
                return txt.Text;
            }
            return null;
        }
    }
}
