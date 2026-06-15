using System;
using System.Threading.Tasks;
using System.Windows.Input;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.UI.ViewModels
{
    public class PinSetupViewModel : ViewModelBase
    {
        private readonly IStaffService _staffService;
        private Staff _staff;
        private string _newPin;
        private string _confirmPin;
        private string _errorMessage;

        public Staff Staff
        {
            get => _staff;
            set { _staff = value; OnPropertyChanged(); }
        }

        public string NewPin
        {
            get => _newPin;
            set { _newPin = value; OnPropertyChanged(); ErrorMessage = string.Empty; }
        }

        public string ConfirmPin
        {
            get => _confirmPin;
            set { _confirmPin = value; OnPropertyChanged(); ErrorMessage = string.Empty; }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public ICommand SavePinCommand { get; }
        public Action CloseAction { get; set; }
        public bool IsSuccess { get; private set; }

        public PinSetupViewModel(IStaffService staffService)
        {
            _staffService = staffService;
            SavePinCommand = new RelayCommand(async o => await ExecuteSavePinAsync());
        }

        public void Initialize(Staff staff)
        {
            Staff = staff;
        }

        private async Task ExecuteSavePinAsync()
        {
            if (string.IsNullOrEmpty(NewPin) || NewPin.Length < 4)
            {
                ErrorMessage = "PIN must be at least 4 digits.";
                return;
            }

            if (NewPin != ConfirmPin)
            {
                ErrorMessage = "PINs do not match.";
                return;
            }

            try
            {
                await _staffService.ResetPinAsync(Staff.StaffId, NewPin);
                Staff.PinHash = BCrypt.Net.BCrypt.HashPassword(NewPin); // update local model just in case
                IsSuccess = true;
                CloseAction?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error saving PIN: {ex.Message}";
            }
        }
    }
}
