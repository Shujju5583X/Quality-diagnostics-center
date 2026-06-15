using System.IO;
using System.Windows.Input;
using LabSystem.Core.Interfaces;
using Serilog;

namespace LabSystem.UI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ViewModelBase _currentViewModel;
        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set { _currentViewModel = value; OnPropertyChanged(); }
        }

        public ICommand BackupCommand { get; }

        public MainViewModel(IBackupService backupService)
        {
            Log.Information("MainViewModel constructor start.");
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "startup_crash.log"),
                $"{System.DateTime.Now:yyyy-MM-dd HH:mm:ss} MainViewModel ctor start\r\n");

            // Single-operator mode: go directly to Dashboard — no login required
            Log.Information("Resolving DashboardViewModel from container...");
            var dashboardVm = App.Container.GetInstance<DashboardViewModel>();
            Log.Information("DashboardViewModel resolved. Setting as CurrentViewModel...");
            CurrentViewModel = dashboardVm;

            BackupCommand = new RelayCommand(async o =>
            {
                await backupService.BackupNowAsync();
                System.Windows.MessageBox.Show(
                    "Database (SQLite) and technician-friendly report (Excel) backed up successfully to the backups directory!",
                    "Backup Completed",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            });

            Log.Information("MainViewModel constructor complete.");
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "startup_crash.log"),
                $"{System.DateTime.Now:yyyy-MM-dd HH:mm:ss} MainViewModel ctor complete\r\n");
        }
    }
}
