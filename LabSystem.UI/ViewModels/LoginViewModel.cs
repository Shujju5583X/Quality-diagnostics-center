using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using Serilog;

namespace LabSystem.UI.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly IStaffRepository _staffRepo;
        private Staff _selectedStaff;
        private string _pin;
        private string _errorMessage;
        private bool _isLoginSuccess;

        public ObservableCollection<Staff> StaffMembers { get; } = new ObservableCollection<Staff>();

        public Staff SelectedStaff
        {
            get => _selectedStaff;
            set
            {
                _selectedStaff = value;
                OnPropertyChanged();
                ErrorMessage = string.Empty;
            }
        }

        public string Pin
        {
            get => _pin;
            set
            {
                _pin = value;
                OnPropertyChanged();
                ErrorMessage = string.Empty;
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoginSuccess
        {
            get => _isLoginSuccess;
            private set
            {
                _isLoginSuccess = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoginCommand { get; }

        public Action CloseAction { get; set; }

        public LoginViewModel(IStaffRepository staffRepo)
        {
            _staffRepo = staffRepo ?? throw new ArgumentNullException(nameof(staffRepo));
            LoginCommand = new RelayCommand(async o => await ExecuteLoginAsync(), o => SelectedStaff != null && !string.IsNullOrEmpty(Pin));
        }

        public async Task InitializeAsync()
        {
            try
            {
                var staffList = await _staffRepo.GetAllAsync();
                StaffMembers.Clear();
                foreach (var s in staffList)
                {
                    StaffMembers.Add(s);
                }
                SelectedStaff = StaffMembers.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load staff list for login.");
                ErrorMessage = "Failed to load technicians from database.";
            }
        }

        private async Task ExecuteLoginAsync()
        {
            if (SelectedStaff == null || string.IsNullOrEmpty(Pin)) return;

            var staff = SelectedStaff;

            // Check if locked out
            if (staff.LockoutEnd.HasValue)
            {
                var remaining = staff.LockoutEnd.Value - DateTime.UtcNow;
                if (remaining.TotalSeconds > 0)
                {
                    ErrorMessage = $"Account locked. Try again in {(int)Math.Ceiling(remaining.TotalSeconds)} seconds.";
                    return;
                }
            }

            try
            {
                bool isPinValid = false;
                if (!string.IsNullOrEmpty(staff.PinHash))
                {
                    // Verify PIN using BCrypt
                    isPinValid = BCrypt.Net.BCrypt.Verify(Pin, staff.PinHash);
                }
                else
                {
                    // First-time PIN setup required
                    var pinSetupVM = App.Container.GetInstance<PinSetupViewModel>();
                    pinSetupVM.Initialize(staff);
                    
                    // We must dispatch UI creation to the view layer conceptually, but for simplicity we instantiate it here
                    var pinSetupView = new Views.PinSetupView { DataContext = pinSetupVM };
                    pinSetupVM.CloseAction = () => pinSetupView.DialogResult = true;
                    
                    if (pinSetupView.ShowDialog() == true && pinSetupVM.IsSuccess)
                    {
                        // PIN successfully set, consider them logged in
                        staff.FailedLoginAttempts = 0;
                        staff.LockoutEnd = null;
                        
                        IsLoginSuccess = true;
                        App.AuthenticatedStaffId = staff.StaffId;
                        CloseAction?.Invoke();
                        return;
                    }
                    else
                    {
                        ErrorMessage = "PIN setup was cancelled or failed.";
                        return;
                    }
                }

                if (isPinValid)
                {
                    // Reset lockout and login attempts
                    staff.FailedLoginAttempts = 0;
                    staff.LockoutEnd = null;
                    await _staffRepo.UpdateAsync(staff);

                    IsLoginSuccess = true;
                    App.AuthenticatedStaffId = staff.StaffId;
                    CloseAction?.Invoke();
                }
                else
                {
                    staff.FailedLoginAttempts++;
                    if (staff.FailedLoginAttempts >= 5)
                    {
                        staff.LockoutEnd = DateTime.UtcNow.AddMinutes(1);
                        await _staffRepo.UpdateAsync(staff);
                        ErrorMessage = "Too many failed attempts. Account locked for 1 minute.";
                    }
                    else
                    {
                        await _staffRepo.UpdateAsync(staff);
                        ErrorMessage = $"Invalid PIN. {5 - staff.FailedLoginAttempts} attempts remaining.";
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during staff login.");
                ErrorMessage = "An error occurred during verification.";
            }
        }
    }
}
