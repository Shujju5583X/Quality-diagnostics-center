using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using LabSystem.Core.Models;
using Serilog;

namespace LabSystem.UI.ViewModels
{
    public partial class DashboardViewModel
    {
        private async Task ExecuteSaveSettingsAsync()
        {
            try
            {
                var keys = new[] { "operator_name", "operator_address", "operator_phone" };
                var values = new[] { OperatorName ?? "", OperatorAddress ?? "", OperatorPhone ?? "" };

                var settingsList = (await _settingRepo.GetAllAsync()).ToList();
                for (int i = 0; i < keys.Length; i++)
                {
                    var key = keys[i];
                    var val = values[i];
                    var setting = settingsList.FirstOrDefault(s => s.Key == key);
                    if (setting == null)
                    {
                        setting = new Setting { Key = key, Value = val };
                        await _settingRepo.AddAsync(setting);
                    }
                    else
                    {
                        setting.Value = val;
                        await _settingRepo.UpdateAsync(setting);
                    }
                }

                Log.Information("Saved operator settings: Name={Name}", OperatorName);
                MessageBox.Show("Operator settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save operator settings.");
                MessageBox.Show("Error saving settings to database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteBackupCSVAsync()
        {
            var saveFileDialog = new SaveFileDialog
            {
                FileName = $"lab_backup_{DateTime.Today:yyyy-MM-dd}.csv",
                DefaultExt = ".csv",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Save CSV Backup"
            };

            if (saveFileDialog.ShowDialog() != true)
            {
                return;
            }

            string filePath = saveFileDialog.FileName;

            try
            {
                var sb = new StringBuilder();

                // 1. Settings Section
                sb.AppendLine("=== SETTINGS ===");
                sb.AppendLine("Key,Value");
                var settings = await _settingRepo.GetAllAsync();
                foreach (var s in settings)
                {
                    sb.AppendLine($"{EscapeCsv(s.Key)},{EscapeCsv(s.Value)}");
                }
                sb.AppendLine();

                // 2. Patients Section
                sb.AppendLine("=== PATIENTS ===");
                sb.AppendLine("PatientId,Uhid,FullName,Gender,Age,ContactPhone,ContactEmail,CreatedAt");
                var patients = await _patientRepo.GetAllAsync();
                foreach (var p in patients)
                {
                    sb.AppendLine($"{p.PatientId},{EscapeCsv(p.Uhid)},{EscapeCsv(p.FullName)},{EscapeCsv(p.Gender)},{p.Age},{EscapeCsv(p.ContactPhone)},{EscapeCsv(p.ContactEmail)},{p.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                }
                sb.AppendLine();

                // 3. Doctors Section
                sb.AppendLine("=== DOCTORS ===");
                sb.AppendLine("DoctorId,FullName,ContactPhone,Commission,CreatedAt");
                var doctors = await _doctorRepo.GetAllAsync();
                foreach (var d in doctors)
                {
                    sb.AppendLine($"{d.DoctorId},{EscapeCsv(d.FullName)},{EscapeCsv(d.ContactPhone)},{d.Commission},{d.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                }
                sb.AppendLine();

                // 4. Departments Section
                sb.AppendLine("=== DEPARTMENTS ===");
                sb.AppendLine("DepartmentId,Name");
                var depts = await _departmentRepo.GetAllAsync();
                foreach (var dept in depts)
                {
                    sb.AppendLine($"{dept.DepartmentId},{EscapeCsv(dept.Name)}");
                }
                sb.AppendLine();

                // 5. Test Catalog Section
                sb.AppendLine("=== TEST CATALOG ===");
                sb.AppendLine("TypeId,Name,Unit,Price,SampleType,Category,IsActive");
                var testTypes = await _testTypeRepo.GetAllAsync();
                foreach (var t in testTypes)
                {
                    sb.AppendLine($"{t.TypeId},{EscapeCsv(t.Name)},{EscapeCsv(t.Unit)},{t.Price},{EscapeCsv(t.SampleType)},{EscapeCsv(t.Category)},{(t.IsActive ? 1 : 0)}");
                }
                sb.AppendLine();

                // 6. Orders Section
                sb.AppendLine("=== ORDERS ===");
                sb.AppendLine("OrderId,PatientId,DoctorId,ReferredBy,Status,OrderedAt");
                var orders = await _orderRepo.GetAllAsync();
                foreach (var o in orders)
                {
                    sb.AppendLine($"{o.OrderId},{o.PatientId},{o.DoctorId},{EscapeCsv(o.ReferredBy)},{EscapeCsv(o.Status)},{o.OrderedAt:yyyy-MM-dd HH:mm:ss}");
                }
                sb.AppendLine();

                // 7. Results Section
                sb.AppendLine("=== RESULTS ===");
                sb.AppendLine("ResultId,OrderId,TypeId,Value,ValueText,IsAbnormal,IsAmended,RecordedAt");
                var results = await _resultRepo.GetAllAsync();
                foreach (var r in results)
                {
                    string valStr = r.Value.HasValue ? r.Value.Value.ToString() : "NULL";
                    sb.AppendLine($"{r.ResultId},{r.OrderId},{r.TypeId},{valStr},{EscapeCsv(r.ValueText)},{(r.IsAbnormal ? 1 : 0)},{(r.IsAmended ? 1 : 0)},{r.RecordedAt:yyyy-MM-dd HH:mm:ss}");
                }
                sb.AppendLine();

                // 8. Invoices Section
                sb.AppendLine("=== INVOICES ===");
                sb.AppendLine("InvoiceId,OrderId,TotalAmount,DiscountAmount,TaxAmount,AmountPaid,PaymentMethod,IsPaid,CreatedAt");
                var invoices = await _billingService.GetAllInvoicesAsync();
                foreach (var i in invoices)
                {
                    sb.AppendLine($"{i.InvoiceId},{i.OrderId},{i.TotalAmount},{i.DiscountAmount},{i.TaxAmount},{i.AmountPaid},{EscapeCsv(i.PaymentMethod)},{(i.IsPaid ? 1 : 0)},{i.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

                // Update settings table with last backup time
                string nowStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var settingsListBackup = (await _settingRepo.GetAllAsync()).ToList();
                var setting = settingsListBackup.FirstOrDefault(s => s.Key == "last_backup");
                if (setting == null)
                {
                    setting = new Setting { Key = "last_backup", Value = nowStr };
                    await _settingRepo.AddAsync(setting);
                }
                else
                {
                    setting.Value = nowStr;
                    await _settingRepo.UpdateAsync(setting);
                }

                Log.Information("Manual CSV Backup created successfully at {Path}", filePath);
                MessageBox.Show("CSV Backup downloaded successfully!", "Backup Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create CSV backup.");
                MessageBox.Show($"Error creating backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string EscapeCsv(string value)
        {
            if (value == null) return string.Empty;
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\r") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }
    }
}
