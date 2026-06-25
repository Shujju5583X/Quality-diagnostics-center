using System.Windows.Input;
using LabSystem.Core.Interfaces;
using Serilog;
using LabSystem.Core;
using System.Threading.Tasks;

namespace LabSystem.UI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ViewModelBase _currentViewModel;
        public ViewModelBase CurrentViewModel
        {
            get { return _currentViewModel; }
            set { _currentViewModel = value; OnPropertyChanged(); }
        }

        public ICommand BackupCommand { get; private set; }

        public MainViewModel(IBackupService backupService)
        {
            Log.Information("MainViewModel constructor start.");

            // Start on the login screen
            Log.Information("Resolving LoginViewModel...");
            var loginVm = App.Container.GetInstance<LoginViewModel>();
            // ponytail: fire-and-forget — staff list populates the dropdown asynchronously,
            // user can't log in until it loads anyway (LoginCommand checks SelectedStaff != null)
            var _ = loginVm.InitializeAsync();
            loginVm.LoginSuccessAction = () =>
            {
                Log.Information("Login successful. Switching to Dashboard...");
                var dashboardVm = App.Container.GetInstance<DashboardViewModel>();
                CurrentViewModel = dashboardVm;
            };
            CurrentViewModel = loginVm;

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
        }
    }
}