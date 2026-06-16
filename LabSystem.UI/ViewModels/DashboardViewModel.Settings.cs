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
                FileName = "lab_backup_" + DateTime.Today.ToString("yyyy-MM-dd") + ".csv",
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
                    sb.AppendLine(EscapeCsv(s.Key) + "," + EscapeCsv(s.Value));
                }
                sb.AppendLine();

                // 2. Patients Section
                sb.AppendLine("=== PATIENTS ===");
                sb.AppendLine("PatientId,Uhid,FullName,Gender,Age,ContactPhone,ContactEmail,CreatedAt");
                var patients = await _patientRepo.GetAllAsync();
                foreach (var p in patients)
                {
                    sb.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7}", p.PatientId, EscapeCsv(p.Uhid), EscapeCsv(p.FullName), EscapeCsv(p.Gender), p.Age, EscapeCsv(p.ContactPhone), EscapeCsv(p.ContactEmail), p.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
                }
                sb.AppendLine();

                // 3. Doctors Section
                sb.AppendLine("=== DOCTORS ===");
                sb.AppendLine("DoctorId,FullName,ContactPhone,Commission,CreatedAt");
                var doctors = await _doctorRepo.GetAllAsync();
                foreach (var d in doctors)
                {
                    sb.AppendLine(string.Format("{0},{1},{2},{3},{4}", d.DoctorId, EscapeCsv(d.FullName), EscapeCsv(d.ContactPhone), d.Commission, d.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
                }
                sb.AppendLine();

                // 4. Departments Section
                sb.AppendLine("=== DEPARTMENTS ===");
                sb.AppendLine("DepartmentId,Name");
                var depts = await _departmentRepo.GetAllAsync();
                foreach (var dept in depts)
                {
                    sb.AppendLine(dept.DepartmentId + "," + EscapeCsv(dept.Name));
                }
                sb.AppendLine();

                // 5. Test Catalog Section
                sb.AppendLine("=== TEST CATALOG ===");
                sb.AppendLine("TypeId,Name,Unit,Price,SampleType,Category,IsActive");
                var testTypes = await _testTypeRepo.GetAllAsync();
                foreach (var t in testTypes)
                {
                    sb.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6}", t.TypeId, EscapeCsv(t.Name), EscapeCsv(t.Unit), t.Price, EscapeCsv(t.SampleType), EscapeCsv(t.Category), t.IsActive ? 1 : 0));
                }
                sb.AppendLine();

                // 6. Orders Section
                sb.AppendLine("=== ORDERS ===");
                sb.AppendLine("OrderId,PatientId,DoctorId,ReferredBy,Status,OrderedAt");
                var orders = await _orderRepo.GetAllAsync();
                foreach (var o in orders)
                {
                    sb.AppendLine(string.Format("{0},{1},{2},{3},{4},{5}", o.OrderId, o.PatientId, o.DoctorId, EscapeCsv(o.ReferredBy), EscapeCsv(o.Status), o.OrderedAt.ToString("yyyy-MM-dd HH:mm:ss")));
                }
                sb.AppendLine();

                // 7. Results Section
                sb.AppendLine("=== RESULTS ===");
                sb.AppendLine("ResultId,OrderId,TypeId,Value,ValueText,IsAbnormal,IsAmended,RecordedAt");
                var results = await _resultRepo.GetAllAsync();
                foreach (var r in results)
                {
                    string valStr = r.Value.HasValue ? r.Value.Value.ToString() : "NULL";
                    sb.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7}", r.ResultId, r.OrderId, r.TypeId, valStr, EscapeCsv(r.ValueText), r.IsAbnormal ? 1 : 0, r.IsAmended ? 1 : 0, r.RecordedAt.ToString("yyyy-MM-dd HH:mm:ss")));
                }
                sb.AppendLine();

                // 8. Invoices Section
                sb.AppendLine("=== INVOICES ===");
                sb.AppendLine("InvoiceId,OrderId,TotalAmount,DiscountAmount,TaxAmount,AmountPaid,PaymentMethod,IsPaid,CreatedAt");
                var invoices = await _billingService.GetAllInvoicesAsync();
                foreach (var i in invoices)
                {
                    sb.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", i.InvoiceId, i.OrderId, i.TotalAmount, i.DiscountAmount, i.TaxAmount, i.AmountPaid, EscapeCsv(i.PaymentMethod), i.IsPaid ? 1 : 0, i.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
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
                MessageBox.Show("Error creating backup: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private async Task ExecuteRestoreBackupAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                DefaultExt = ".csv",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select CSV Backup to Restore"
            };

            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            var result = MessageBox.Show("This will overwrite current data. Are you sure?", "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                string filePath = openFileDialog.FileName;
                string content = File.ReadAllText(filePath);
                
                using (var connection = new System.Data.SQLite.SQLiteConnection(LabSystem.Data.SecureConfigurationManager.GetLabDbConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            string[] tables = new[] { "Results", "Invoices", "TestOrders", "TestTypes", "Departments", "DoctorCommissions", "Doctors", "Patients", "Settings" };
                            foreach (var table in tables)
                            {
                                using (var cmd = new System.Data.SQLite.SQLiteCommand("DELETE FROM " + table, connection, transaction))
                                {
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }

                            string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                            string currentSection = null;
                            bool skipNext = false;

                            foreach(var line in lines)
                            {
                                if (string.IsNullOrWhiteSpace(line)) continue;
                                if (line.StartsWith("==="))
                                {
                                    currentSection = line.Trim();
                                    skipNext = true; 
                                    continue;
                                }
                                if (skipNext)
                                {
                                    skipNext = false;
                                    continue;
                                }
                                
                                var cols = ParseCsvLine(line);
                                if (currentSection == "=== SETTINGS ===")
                                {
                                    using (var cmd = new System.Data.SQLite.SQLiteCommand("INSERT INTO Settings ([Key], [Value]) VALUES (@p0, @p1)", connection, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@p0", cols[0]);
                                        cmd.Parameters.AddWithValue("@p1", cols[1]);
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                                else if (currentSection == "=== PATIENTS ===")
                                {
                                    using (var cmd = new System.Data.SQLite.SQLiteCommand("INSERT INTO Patients (PatientId, Uhid, FullName, Gender, Age, ContactPhone, ContactEmail, CreatedAt) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7)", connection, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@p0", cols[0]);
                                        cmd.Parameters.AddWithValue("@p1", cols[1]);
                                        cmd.Parameters.AddWithValue("@p2", cols[2]);
                                        cmd.Parameters.AddWithValue("@p3", cols[3]);
                                        cmd.Parameters.AddWithValue("@p4", string.IsNullOrEmpty(cols[4]) ? DBNull.Value : (object)cols[4]);
                                        cmd.Parameters.AddWithValue("@p5", cols[5]);
                                        cmd.Parameters.AddWithValue("@p6", cols[6]);
                                        cmd.Parameters.AddWithValue("@p7", cols[7]);
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                                else if (currentSection == "=== DOCTORS ===")
                                {
                                    using (var cmd = new System.Data.SQLite.SQLiteCommand("INSERT INTO Doctors (DoctorId, FullName, ContactPhone, Commission, CreatedAt) VALUES (@p0, @p1, @p2, @p3, @p4)", connection, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@p0", cols[0]);
                                        cmd.Parameters.AddWithValue("@p1", cols[1]);
                                        cmd.Parameters.AddWithValue("@p2", cols[2]);
                                        cmd.Parameters.AddWithValue("@p3", string.IsNullOrEmpty(cols[3]) ? DBNull.Value : (object)cols[3]);
                                        cmd.Parameters.AddWithValue("@p4", cols[4]);
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                                else if (currentSection == "=== DEPARTMENTS ===")
                                {
                                    using (var cmd = new System.Data.SQLite.SQLiteCommand("INSERT INTO Departments (DepartmentId, Name) VALUES (@p0, @p1)", connection, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@p0", cols[0]);
                                        cmd.Parameters.AddWithValue("@p1", cols[1]);
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                                else if (currentSection == "=== TEST CATALOG ===")
                                {
                                    using (var cmd = new System.Data.SQLite.SQLiteCommand("INSERT INTO TestTypes (TypeId, Name, Unit, Price, SampleType, Category, IsActive) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6)", connection, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@p0", cols[0]);
                                        cmd.Parameters.AddWithValue("@p1", cols[1]);
                                        cmd.Parameters.AddWithValue("@p2", cols[2]);
                                        cmd.Parameters.AddWithValue("@p3", string.IsNullOrEmpty(cols[3]) ? DBNull.Value : (object)cols[3]);
                                        cmd.Parameters.AddWithValue("@p4", cols[4]);
                                        cmd.Parameters.AddWithValue("@p5", cols[5]);
                                        cmd.Parameters.AddWithValue("@p6", cols[6]);
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                                else if (currentSection == "=== ORDERS ===")
                                {
                                    using (var cmd = new System.Data.SQLite.SQLiteCommand("INSERT INTO TestOrders (OrderId, PatientId, DoctorId, ReferredBy, Status, OrderedAt) VALUES (@p0, @p1, @p2, @p3, @p4, @p5)", connection, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@p0", cols[0]);
                                        cmd.Parameters.AddWithValue("@p1", cols[1]);
                                        cmd.Parameters.AddWithValue("@p2", string.IsNullOrEmpty(cols[2]) ? DBNull.Value : (object)cols[2]);
                                        cmd.Parameters.AddWithValue("@p3", cols[3]);
                                        cmd.Parameters.AddWithValue("@p4", cols[4]);
                                        cmd.Parameters.AddWithValue("@p5", cols[5]);
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                                else if (currentSection == "=== RESULTS ===")
                                {
                                    using (var cmd = new System.Data.SQLite.SQLiteCommand("INSERT INTO Results (ResultId, OrderId, TypeId, Value, ValueText, IsAbnormal, IsAmended, RecordedAt, TechnicianId) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8)", connection, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@p0", cols[0]);
                                        cmd.Parameters.AddWithValue("@p1", cols[1]);
                                        cmd.Parameters.AddWithValue("@p2", cols[2]);
                                        cmd.Parameters.AddWithValue("@p3", cols[3] == "NULL" ? DBNull.Value : (object)cols[3]);
                                        cmd.Parameters.AddWithValue("@p4", cols[4]);
                                        cmd.Parameters.AddWithValue("@p5", cols[5]);
                                        cmd.Parameters.AddWithValue("@p6", cols[6]);
                                        cmd.Parameters.AddWithValue("@p7", cols[7]);
                                        cmd.Parameters.AddWithValue("@p8", 1); // Default TechnicianId
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                                else if (currentSection == "=== INVOICES ===")
                                {
                                    using (var cmd = new System.Data.SQLite.SQLiteCommand("INSERT INTO Invoices (InvoiceId, OrderId, TotalAmount, DiscountAmount, TaxAmount, AmountPaid, PaymentMethod, IsPaid, CreatedAt) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8)", connection, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@p0", cols[0]);
                                        cmd.Parameters.AddWithValue("@p1", cols[1]);
                                        cmd.Parameters.AddWithValue("@p2", string.IsNullOrEmpty(cols[2]) ? DBNull.Value : (object)cols[2]);
                                        cmd.Parameters.AddWithValue("@p3", string.IsNullOrEmpty(cols[3]) ? DBNull.Value : (object)cols[3]);
                                        cmd.Parameters.AddWithValue("@p4", string.IsNullOrEmpty(cols[4]) ? DBNull.Value : (object)cols[4]);
                                        cmd.Parameters.AddWithValue("@p5", string.IsNullOrEmpty(cols[5]) ? DBNull.Value : (object)cols[5]);
                                        cmd.Parameters.AddWithValue("@p6", cols[6]);
                                        cmd.Parameters.AddWithValue("@p7", cols[7]);
                                        cmd.Parameters.AddWithValue("@p8", cols[8]);
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                            transaction.Commit();
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
                
                MessageBox.Show("Backup restored successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to restore CSV backup.");
                MessageBox.Show("Error restoring backup: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private System.Collections.Generic.List<string> ParseCsvLine(string line)
        {
            var result = new System.Collections.Generic.List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        current.Append('\"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result;
        }
    }
}
