using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using Serilog;

namespace LabSystem.UI.ViewModels
{
    public partial class DashboardViewModel
    {
        private readonly ICsvBackupService _csvBackupService;

        public ObservableCollection<Setting> SettingsCollection { get; private set; }

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
                ShowInfo("Operator settings saved successfully!", "Success");
                await LoadDataAsync();
                await LoadSettingsCollectionAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save operator settings.");
                ShowError("Error saving settings to database.");
            }
        }

        private async Task ExecuteDeleteSettingAsync(object obj)
        {
            var setting = obj as Setting;
            if (setting == null) return;

            var dialogResult = MessageBox.Show(
                "Are you sure you want to delete setting '" + setting.Key + "'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (dialogResult != MessageBoxResult.Yes) return;

            try
            {
                await _settingRepo.DeleteAsync(setting.Key.GetHashCode());
                Log.Information("Deleted setting: {Key}", setting.Key);

                // Refresh the settings collection and operator fields
                await LoadSettingsCollectionAsync();
                await LoadDataAsync();

                MessageBox.Show("Setting deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete setting.");
                MessageBox.Show("Error deleting setting: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteAddSettingAsync()
        {
            if (string.IsNullOrWhiteSpace(NewSettingKey))
            {
                MessageBox.Show("Please enter a setting key.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var settingsList = (await _settingRepo.GetAllAsync()).ToList();
                var existing = settingsList.FirstOrDefault(s => s.Key == NewSettingKey.Trim());
                if (existing != null)
                {
                    MessageBox.Show("A setting with key '" + NewSettingKey.Trim() + "' already exists.", "Duplicate Key", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var setting = new Setting
                {
                    Key = NewSettingKey.Trim(),
                    Value = NewSettingValue ?? ""
                };

                await _settingRepo.AddAsync(setting);
                Log.Information("Added setting: {Key}={Value}", setting.Key, setting.Value);

                NewSettingKey = "";
                NewSettingValue = "";
                await LoadSettingsCollectionAsync();

                MessageBox.Show("Setting added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add setting.");
                MessageBox.Show("Error adding setting: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string _newSettingKey;
        public string NewSettingKey
        {
            get { return _newSettingKey; }
            set { _newSettingKey = value; OnPropertyChanged(); }
        }

        private string _newSettingValue;
        public string NewSettingValue
        {
            get { return _newSettingValue; }
            set { _newSettingValue = value; OnPropertyChanged(); }
        }

        private async Task LoadSettingsCollectionAsync()
        {
            try
            {
                var settings = await _settingRepo.GetAllAsync();
                SettingsCollection.Clear();
                foreach (var s in settings.OrderBy(s => s.Key))
                {
                    SettingsCollection.Add(s);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load settings collection.");
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

            try
            {
                await _csvBackupService.ExportToCsvAsync(saveFileDialog.FileName);
                Log.Information("Manual CSV Backup created successfully at {Path}", saveFileDialog.FileName);
                ShowInfo("CSV Backup downloaded successfully!", "Backup Successful");
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create CSV backup.");
                ShowError("Error creating backup: " + ex.Message);
            }
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
                await _csvBackupService.ImportFromCsvAsync(openFileDialog.FileName);
                ShowInfo("Backup restored successfully!", "Success");
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to restore CSV backup.");
                ShowError("Error restoring backup: " + ex.Message);
            }
        }
    }
}
