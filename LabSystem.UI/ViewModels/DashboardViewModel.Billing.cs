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

                await _auditLogRepo.AddAsync(new AuditLog
                {
                    Action = "Updated",
                    EntityType = "Invoice",
                    EntityId = SelectedInvoice.InvoiceId,
                    UserId = StaffId,
                    Timestamp = DateTime.UtcNow,
                    Details = $"Added {paymentMethod} payment of Rs.{amount} to invoice {SelectedInvoice.InvoiceId}."
                });

                MessageBox.Show($"Payment of Rs.{amount} via {paymentMethod} added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadDataAsync(); // Reload invoices
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add payment to invoice.");
                MessageBox.Show("Error updating invoice.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteUpdateFinancialsAsync(object obj)
        {
            if (SelectedInvoice == null)
            {
                MessageBox.Show("Please select an invoice.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string discountStr = RejectionDialog.Show("Update Financials", "Enter Discount Amount:");
            if (discountStr == null) return;
            string taxStr = RejectionDialog.Show("Update Financials", "Enter Tax Amount:");
            if (taxStr == null) return;

            decimal discount = 0, tax = 0;
            decimal.TryParse(discountStr, out discount);
            decimal.TryParse(taxStr, out tax);

            try
            {
                await _billingService.UpdateInvoiceFinancialsAsync(SelectedInvoice.InvoiceId, discount, tax);
                await LoadDataAsync();
                MessageBox.Show("Invoice financials updated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to update invoice financials.");
                MessageBox.Show("Error updating invoice.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteExportRevenueReportAsync(object obj)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"RevenueReport_{DateTime.Today:yyyyMMdd}",
                DefaultExt = ".xlsx",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _billingService.ExportRevenueReportToExcelAsync(ReferralStartDate, ReferralEndDate, dialog.FileName);
                    MessageBox.Show("Revenue report exported successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to export revenue report.");
                    MessageBox.Show("Error exporting revenue report.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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

                await _auditLogRepo.AddAsync(new AuditLog
                {
                    Action = "Backup",
                    EntityType = "System",
                    UserId = StaffId,
                    Timestamp = DateTime.UtcNow,
                    Details = "Created full SQLite database and ClosedXML Excel backup."
                });

                MessageBox.Show("Database (SQLite) and technician-friendly report (Excel) backed up successfully to the backups directory!", "Backup Completed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Database backup failed.");
                MessageBox.Show("Failed to complete database backup. Check logs for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
