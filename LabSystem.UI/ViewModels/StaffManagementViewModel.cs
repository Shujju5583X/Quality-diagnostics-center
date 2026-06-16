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

        public ObservableCollection<Staff> StaffMembers { get; private set; }

        private string _newStaffName;
        public string NewStaffName
        {
            get { return _newStaffName; }
            set { _newStaffName = value; OnPropertyChanged(); }
        }

        private string _newStaffRole = "Technician";
        public string NewStaffRole
        {
            get { return _newStaffRole; }
            set { _newStaffRole = value; OnPropertyChanged(); }
        }

        private string _newStaffPin;
        public string NewStaffPin
        {
            get { return _newStaffPin; }
            set { _newStaffPin = value; OnPropertyChanged(); }
        }

        private Staff _selectedStaff;
        public Staff SelectedStaff
        {
            get { return _selectedStaff; }
            set { _selectedStaff = value; OnPropertyChanged(); OnPropertyChanged("IsStaffSelected"); }
        }

        public bool IsStaffSelected
        {
            get { return SelectedStaff != null; }
        }

        private string _resetPinValue;
        public string ResetPinValue
        {
            get { return _resetPinValue; }
            set { _resetPinValue = value; OnPropertyChanged(); }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get { return _statusMessage; }
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public ICommand LoadStaffCommand { get; private set; }
        public ICommand AddStaffCommand { get; private set; }
        public ICommand ResetPinCommand { get; private set; }
        public ICommand ToggleLockoutCommand { get; private set; }

        public string PendingStaffPin { get; set; }
        public string PendingResetPin { get; set; }

        public StaffManagementViewModel(IStaffService staffService, IStaffRepository staffRepo)
        {
            _staffService = staffService;
            _staffRepo = staffRepo;
            StaffMembers = new ObservableCollection<Staff>();

            LoadStaffCommand = new AsyncRelayCommand(async o => await LoadStaffAsync());
            AddStaffCommand = new AsyncRelayCommand(async o => await ExecuteAddStaffAsync());
            ResetPinCommand = new AsyncRelayCommand(async o => await ExecuteResetPinAsync());
            ToggleLockoutCommand = new AsyncRelayCommand(async o => await ExecuteToggleLockoutAsync(o));

            var unused = LoadStaffAsync();
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
                StatusMessage = "Error loading staff: " + ex.Message;
            }
        }

        private async Task ExecuteAddStaffAsync()
        {
            try
            {
                await _staffService.CreateStaffAsync(NewStaffName, NewStaffRole, PendingStaffPin ?? NewStaffPin);
                StatusMessage = "Staff '" + NewStaffName + "' added successfully.";
                NewStaffName = "";
                PendingStaffPin = "";
                NewStaffPin = "";
                NewStaffRole = "Technician";
                await LoadStaffAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
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
                StatusMessage = "Error: " + ex.Message;
            }
        }

        private async Task ExecuteToggleLockoutAsync(object parameter)
        {
            var staff = parameter as Staff;
            if (staff != null)
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
                    StatusMessage = "Error: " + ex.Message;
                }
            }
        }
    }
}
