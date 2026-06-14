using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.UI.ViewModels
{
    public class StaffManagementViewModel : ViewModelBase
    {
        private readonly IStaffService _staffService;
        private readonly IStaffRepository _staffRepo;

        public ObservableCollection<Staff> StaffMembers { get; } = new ObservableCollection<Staff>();

        private string _newStaffName;
        public string NewStaffName
        {
            get => _newStaffName;
            set { _newStaffName = value; OnPropertyChanged(); }
        }

        private string _newStaffRole = "Technician";
        public string NewStaffRole
        {
            get => _newStaffRole;
            set { _newStaffRole = value; OnPropertyChanged(); }
        }

        private string _newStaffPin;
        public string NewStaffPin
        {
            get => _newStaffPin;
            set { _newStaffPin = value; OnPropertyChanged(); }
        }

        private Staff _selectedStaff;
        public Staff SelectedStaff
        {
            get => _selectedStaff;
            set { _selectedStaff = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsStaffSelected)); }
        }

        public bool IsStaffSelected => SelectedStaff != null;

        private string _resetPinValue;
        public string ResetPinValue
        {
            get => _resetPinValue;
            set { _resetPinValue = value; OnPropertyChanged(); }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public ICommand LoadStaffCommand { get; }
        public ICommand AddStaffCommand { get; }
        public ICommand ResetPinCommand { get; }
        public ICommand ToggleLockoutCommand { get; }

        public string PendingStaffPin { get; set; }
        public string PendingResetPin { get; set; }

        public StaffManagementViewModel(IStaffService staffService, IStaffRepository staffRepo)
        {
            _staffService = staffService;
            _staffRepo = staffRepo;

            LoadStaffCommand = new AsyncRelayCommand(async o => await LoadStaffAsync());
            AddStaffCommand = new AsyncRelayCommand(async o => await ExecuteAddStaffAsync());
            ResetPinCommand = new AsyncRelayCommand(async o => await ExecuteResetPinAsync());
            ToggleLockoutCommand = new AsyncRelayCommand(async o => await ExecuteToggleLockoutAsync(o));

            _ = LoadStaffAsync();
        }

        public async Task LoadStaffAsync()
        {
            try
            {
                var all = await _staffService.GetAllStaffAsync();
                StaffMembers.Clear();
                foreach (var s in all.OrderBy(s => s.FullName))
                    StaffMembers.Add(s);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading staff: {ex.Message}";
            }
        }

        private async Task ExecuteAddStaffAsync()
        {
            try
            {
                await _staffService.CreateStaffAsync(NewStaffName, NewStaffRole, PendingStaffPin ?? NewStaffPin);
                StatusMessage = $"Staff '{NewStaffName}' added successfully.";
                NewStaffName = "";
                PendingStaffPin = "";
                NewStaffPin = "";
                NewStaffRole = "Technician";
                await LoadStaffAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async Task ExecuteResetPinAsync()
        {
            if (SelectedStaff == null) return;
            try
            {
                await _staffService.ResetPinAsync(SelectedStaff.StaffId, PendingResetPin ?? ResetPinValue);
                StatusMessage = "PIN reset successfully.";
                PendingResetPin = "";
                ResetPinValue = "";
                await LoadStaffAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async Task ExecuteToggleLockoutAsync(object parameter)
        {
            if (parameter is Staff staff)
            {
                try
                {
                    var isLocked = staff.LockoutEnd > DateTime.UtcNow;
                    await _staffService.ToggleLockoutAsync(staff.StaffId, !isLocked);
                    StatusMessage = isLocked ? "Staff unlocked." : "Staff locked.";
                    await LoadStaffAsync();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error: {ex.Message}";
                }
            }
        }
    }
}