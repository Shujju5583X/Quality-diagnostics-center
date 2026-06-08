using System;
using System.Windows.Input;
using LabSystem.Core.Interfaces;
using Serilog;

namespace LabSystem.UI.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private string _pin;
        private string _errorMessage;

        public event Action<int> LoginSuccessful;

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

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;
            LoginCommand = new RelayCommand(ExecuteLogin);
        }

        private void ExecuteLogin(object obj)
        {
            Log.Information("ExecuteLogin called. PIN value: '{Pin}' (Length: {Length})", Pin ?? "NULL", Pin?.Length ?? 0);
            
            // Simple mock for staffId = 1
            if (_authService.VerifyPin(1, Pin))
            {
                LoginSuccessful?.Invoke(1);
            }
            else
            {
                ErrorMessage = "Invalid PIN.";
            }
        }
    }
}
