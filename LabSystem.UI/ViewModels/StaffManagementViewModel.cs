using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using Serilog;

namespace LabSystem.UI.ViewModels
{
    public class StaffManagementViewModel : ViewModelBase
    {
        private readonly IStaffService _staffService;
        private readonly IStaffRepository _staffRepo;

        public ObservableCollection<Staff> StaffMembers { get; private set; }

        private string _editStaffName;
        public string EditStaffName
        {
            get { return _editStaffName; }
            set { _editStaffName = value; OnPropertyChanged(); }
        }

        private string _editStaffRole = "Technician";
        public string EditStaffRole
        {
            get { return _editStaffRole; }
            set { _editStaffRole = value; OnPropertyChanged(); }
        }

        private string _editStaffPin;
        public string EditStaffPin
        {
            get { return _editStaffPin; }
            set { _editStaffPin = value; OnPropertyChanged(); }
        }

        private Staff _selectedStaff;
        public Staff SelectedStaff
        {
            get { return _selectedStaff; }
            set
            {
                _selectedStaff = value;
                OnPropertyChanged();
                OnPropertyChanged("IsStaffSelected");
                if (value != null)
                {
                    EditStaffName = value.FullName;
                    EditStaffRole = value.Role ?? "Technician";
                }
                else
                {
                    EditStaffName = "";
                    EditStaffRole = "Technician";
                }
            }
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

        private string _searchQuery = "";
        public string SearchQuery
        {
            get { return _searchQuery; }
            set
            {
                _searchQuery = value;
                OnPropertyChanged();
                var unused = LoadStaffAsync();
            }
        }

        public ICommand LoadStaffCommand { get; private set; }
        public ICommand AddStaffCommand { get; private set; }
        public ICommand ResetPinCommand { get; private set; }
        public ICommand ToggleLockoutCommand { get; private set; }
        public ICommand DeleteStaffCommand { get; private set; }
        public ICommand EditStaffCommand { get; private set; }
        public ICommand UpdateStaffCommand { get; private set; }
        public ICommand ClearSelectionCommand { get; private set; }

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
            DeleteStaffCommand = new AsyncRelayCommand(async o => await ExecuteDeleteStaffAsync(o));
            EditStaffCommand = new RelayCommand(ExecuteEditStaff);
            UpdateStaffCommand = new AsyncRelayCommand(async o => await ExecuteUpdateStaffAsync());
            ClearSelectionCommand = new RelayCommand(o => ExecuteClearSelection());

            var unused = LoadStaffAsync();
        }

        public async Task LoadStaffAsync()
        {
            try
            {
                var all = await _staffService.GetAllStaffAsync();
                StaffMembers.Clear();
                var filtered = all.Where(s =>
                    string.IsNullOrEmpty(SearchQuery) ||
                    s.FullName.IndexOf(SearchQuery, StringComparison.OrdinalIgnoreCase) >= 0);
                foreach (var s in filtered.OrderBy(s => s.FullName))
                    StaffMembers.Add(s);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading staff.");
                StatusMessage = "Error loading staff: " + ex.Message;
            }
        }

        private async Task ExecuteAddStaffAsync()
        {
            if (string.IsNullOrWhiteSpace(EditStaffName))
            {
                MessageBox.Show("Please enter the staff member's full name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditStaffPin) || EditStaffPin.Length < 4)
            {
                MessageBox.Show("PIN must be at least 4 digits.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await _staffService.CreateStaffAsync(EditStaffName.Trim(), EditStaffRole, EditStaffPin);
                Log.Information("Added staff member: {StaffName}", EditStaffName);
                StatusMessage = "Staff '" + EditStaffName + "' added successfully.";
                EditStaffName = "";
                EditStaffPin = "";
                EditStaffRole = "Technician";
                await LoadStaffAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding staff.");
                StatusMessage = "Error: " + ex.Message;
            }
        }

        private void ExecuteEditStaff(object obj)
        {
            var staff = obj as Staff;
            if (staff != null)
            {
                SelectedStaff = staff;
            }
        }

        private async Task ExecuteUpdateStaffAsync()
        {
            if (SelectedStaff == null) return;

            if (string.IsNullOrWhiteSpace(EditStaffName))
            {
                MessageBox.Show("Please enter the staff member's full name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var staff = await _staffRepo.GetByIdAsync(SelectedStaff.StaffId);
                if (staff != null)
                {
                    staff.FullName = EditStaffName.Trim();
                    staff.Role = EditStaffRole;
                    await _staffService.UpdateStaffAsync(staff);
                    Log.Information("Updated staff member: {StaffName} (ID: {StaffId})", staff.FullName, staff.StaffId);
                    StatusMessage = "Staff '" + staff.FullName + "' updated successfully.";
                }
                SelectedStaff = null;
                EditStaffName = "";
                EditStaffPin = "";
                EditStaffRole = "Technician";
                await LoadStaffAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating staff.");
                StatusMessage = "Error: " + ex.Message;
            }
        }

        private async Task ExecuteDeleteStaffAsync(object parameter)
        {
            var staff = parameter as Staff;
            if (staff == null) return;

            if (staff.StaffId == 1)
            {
                MessageBox.Show("Cannot delete the default operator account.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialogResult = MessageBox.Show(
                "Are you sure you want to delete staff member '" + staff.FullName + "'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (dialogResult != MessageBoxResult.Yes) return;

            try
            {
                await _staffService.DeleteStaffAsync(staff.StaffId);
                Log.Information("Deleted staff member ID: {StaffId}", staff.StaffId);
                StatusMessage = "Staff '" + staff.FullName + "' deleted.";

                if (SelectedStaff != null && SelectedStaff.StaffId == staff.StaffId)
                {
                    SelectedStaff = null;
                    EditStaffName = "";
                    EditStaffRole = "Technician";
                }

                await LoadStaffAsync();
                MessageBox.Show("Staff deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting staff.");
                StatusMessage = "Error: " + ex.Message;
                MessageBox.Show("Error deleting staff: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteResetPinAsync()
        {
            if (SelectedStaff == null) return;

            string pin = PendingResetPin ?? ResetPinValue;
            if (string.IsNullOrWhiteSpace(pin) || pin.Length < 4)
            {
                MessageBox.Show("New PIN must be at least 4 digits.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await _staffService.ResetPinAsync(SelectedStaff.StaffId, pin);
                Log.Information("Reset PIN for staff: {StaffName} (ID: {StaffId})", SelectedStaff.FullName, SelectedStaff.StaffId);
                StatusMessage = "PIN reset successfully for '" + SelectedStaff.FullName + "'.";
                PendingResetPin = "";
                ResetPinValue = "";
                await LoadStaffAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error resetting PIN.");
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
                    Log.Information("Toggled lockout for staff: {StaffName} (ID: {StaffId})", staff.FullName, staff.StaffId);
                    StatusMessage = isLocked ? "Staff '" + staff.FullName + "' unlocked." : "Staff '" + staff.FullName + "' locked.";
                    await LoadStaffAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error toggling lockout.");
                    StatusMessage = "Error: " + ex.Message;
                }
            }
        }

        private void ExecuteClearSelection()
        {
            SelectedStaff = null;
            EditStaffName = "";
            EditStaffRole = "Technician";
            EditStaffPin = "";
            StatusMessage = "";
        }
    }
}
