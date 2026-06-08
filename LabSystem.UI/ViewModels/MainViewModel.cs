using System.Windows.Input;
using LabSystem.Core.Interfaces;

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
            // Set up Login View
            var loginVm = App.Container.GetInstance<LoginViewModel>();
            loginVm.LoginSuccessful += staffId =>
            {
                var dashboardVm = App.Container.GetInstance<DashboardViewModel>();
                dashboardVm.StaffId = staffId;
                CurrentViewModel = dashboardVm;
            };

            CurrentViewModel = loginVm;

            BackupCommand = new RelayCommand(o => {
                backupService.BackupNow();
                System.Windows.MessageBox.Show("Backup completed successfully.");
            });
        }
    }
}
