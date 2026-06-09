using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.Services;
using Serilog;

namespace LabSystem.UI.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly IRepository<Staff> _staffRepo;
        private string _pin;
        private string _errorMessage;
        private Staff _selectedStaff;

        public event Action<int> LoginSuccessful;

        public ObservableCollection<Staff> StaffList { get; } = new ObservableCollection<Staff>();

        public Staff SelectedStaff
        {
            get => _selectedStaff;
            set { _selectedStaff = value; OnPropertyChanged(); }
        }

        public string Pin
        {
            get => _pin;
            set { _pin = value; OnPropertyChanged(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel(IAuthService authService, IRepository<Staff> staffRepo)
        {
            _authService = authService;
            _staffRepo = staffRepo;
            LoginCommand = new RelayCommand(ExecuteLogin);
            LoadStaff();
        }

        private async void LoadStaff()
        {
            try
            {
                var staff = await _staffRepo.GetAllAsync();
                StaffList.Clear();
                foreach (var s in staff)
                {
                    StaffList.Add(s);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load staff list on login view model.");
            }
        }

        private async void ExecuteLogin(object obj)
        {
            if (SelectedStaff == null)
            {
                ErrorMessage = "Please select a staff member.";
                return;
            }

            if (string.IsNullOrEmpty(Pin))
            {
                ErrorMessage = "Please enter your PIN.";
                return;
            }

            Log.Information("ExecuteLogin called for StaffId: {StaffId}", SelectedStaff.StaffId);
            ErrorMessage = string.Empty;

            try
            {
                bool isSuccess = await _authService.VerifyPinAsync(SelectedStaff.StaffId, Pin);
                if (isSuccess)
                {
                    LoginSuccessful?.Invoke(SelectedStaff.StaffId);
                }
                else
                {
                    ErrorMessage = "Invalid PIN.";
                }
            }
            catch (LockoutException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Login failed unexpectedly.");
                ErrorMessage = "An unexpected error occurred.";
            }
        }
    }
}
