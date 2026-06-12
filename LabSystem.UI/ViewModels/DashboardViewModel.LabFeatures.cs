using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LabSystem.Core.Models;
using Serilog;

namespace LabSystem.UI.ViewModels
{
    public partial class DashboardViewModel
    {
        // Properties for Phase 2
        public bool IsSelectedOrderSpecimenRejected
        {
            get
            {
                if (SelectedOrder == null || SelectedOrder.Specimens == null)
                    return false;
                return SelectedOrder.Specimens.Any(s => string.Equals(s.Status, "Rejected", StringComparison.OrdinalIgnoreCase));
            }
        }

        private Doctor _selectedDoctor;
        public ObservableCollection<Doctor> Doctors { get; } = new ObservableCollection<Doctor>();
        
        public Doctor SelectedDoctor
        {
            get => _selectedDoctor;
            set
            {
                _selectedDoctor = value;
                OnPropertyChanged();
            }
        }

        private TestPanel _selectedTestPanel;
        public ObservableCollection<TestPanel> TestPanels { get; } = new ObservableCollection<TestPanel>();

        public TestPanel SelectedTestPanel
        {
            get => _selectedTestPanel;
            set
            {
                _selectedTestPanel = value;
                OnPropertyChanged();
                OnTestPanelSelected(value);
            }
        }

        private Specimen _selectedSpecimen;
        public ObservableCollection<Specimen> Specimens { get; } = new ObservableCollection<Specimen>();

        public Specimen SelectedSpecimen
        {
            get => _selectedSpecimen;
            set
            {
                _selectedSpecimen = value;
                OnPropertyChanged();
            }
        }

        private DateTime _referralStartDate = DateTime.Today.AddDays(-30);
        public DateTime ReferralStartDate
        {
            get => _referralStartDate;
            set
            {
                _referralStartDate = value;
                OnPropertyChanged();
            }
        }

        private DateTime _referralEndDate = DateTime.Today;
        public DateTime ReferralEndDate
        {
            get => _referralEndDate;
            set
            {
                _referralEndDate = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<DoctorReferralStats> ReferralStats { get; } = new ObservableCollection<DoctorReferralStats>();

        // Catalog Doctor tab properties
        private Doctor _catalogSelectedDoctor;
        public ObservableCollection<Doctor> CatalogDoctors { get; } = new ObservableCollection<Doctor>();

        public Doctor CatalogSelectedDoctor
        {
            get => _catalogSelectedDoctor;
            set
            {
                _catalogSelectedDoctor = value;
                OnPropertyChanged();
                PopulateCatalogDoctorFields(value);
            }
        }

        private string _docName;
        private string _docSpecialization;
        private string _docClinicName;
        private string _docContactPhone;
        private decimal _docCommissionPercent;

        public string DocName { get => _docName; set { _docName = value; OnPropertyChanged(); } }
        public string DocSpecialization { get => _docSpecialization; set { _docSpecialization = value; OnPropertyChanged(); } }
        public string DocClinicName { get => _docClinicName; set { _docClinicName = value; OnPropertyChanged(); } }
        public string DocContactPhone { get => _docContactPhone; set { _docContactPhone = value; OnPropertyChanged(); } }
        public decimal DocCommissionPercent { get => _docCommissionPercent; set { _docCommissionPercent = value; OnPropertyChanged(); } }

        // Logic methods
        private void OnTestPanelSelected(TestPanel panel)
        {
            if (panel == null) return;
            
            // Unselect all test types
            foreach (var t in TestTypes)
            {
                t.IsSelected = false;
            }

            // Select matching test types in panel
            if (panel.TestTypes != null)
            {
                var panelTestTypeIds = new HashSet<int>(panel.TestTypes.Select(t => t.TypeId));
                foreach (var t in TestTypes)
                {
                    if (panelTestTypeIds.Contains(t.TypeId))
                    {
                        t.IsSelected = true;
                    }
                }
            }
        }

        private void PopulateCatalogDoctorFields(Doctor doc)
        {
            if (doc != null)
            {
                DocName = doc.Name;
                DocSpecialization = doc.Specialization;
                DocClinicName = doc.ClinicName;
                DocContactPhone = doc.ContactPhone;
                DocCommissionPercent = doc.CommissionPercent;
            }
            else
            {
                DocName = string.Empty;
                DocSpecialization = string.Empty;
                DocClinicName = string.Empty;
                DocContactPhone = string.Empty;
                DocCommissionPercent = 0;
            }
        }

        public async Task RefreshReferralStatsAsync()
        {
            try
            {
                ReferralStats.Clear();
                // To cover the full day on end date
                var start = ReferralStartDate.Date;
                var end = ReferralEndDate.Date.AddDays(1).AddTicks(-1);

                var stats = await _billingService.GetDoctorReferralStatsAsync(start, end);
                foreach (var s in stats)
                {
                    ReferralStats.Add(s);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load doctor referral statistics.");
            }
        }

        private async Task ExecuteRefreshReferralStatsAsync(object obj)
        {
            await RefreshReferralStatsAsync();
        }

        private async Task ExecuteMarkSpecimenStatusAsync(object obj, string status)
        {
            if (SelectedSpecimen == null)
            {
                MessageBox.Show("Please select a specimen from the grid.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var specimen = SelectedSpecimen;
            try
            {
                specimen.Status = status;
                specimen.CollectionTime = DateTime.UtcNow;
                specimen.CollectedBy = CurrentStaffName;
                await _specimenRepo.UpdateAsync(specimen);
                await LoadDataAsync();
                
                await _auditLogRepo.AddAsync(new AuditLog
                {
                    Action = "Updated",
                    EntityType = "Specimen",
                    EntityId = specimen.SpecimenId,
                    UserId = StaffId,
                    Timestamp = DateTime.UtcNow,
                    Details = $"Specimen barcode {specimen.Barcode} marked as {status}."
                });

                MessageBox.Show($"Specimen marked as {status}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to update specimen status.");
                MessageBox.Show("Error updating specimen status.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteRejectSpecimenAsync(object obj)
        {
            if (SelectedSpecimen == null)
            {
                MessageBox.Show("Please select a specimen from the grid.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var reason = RejectionDialog.Show("Reject Specimen", "Enter reason for rejection (e.g., Clotted, Hemolyzed):");
            if (string.IsNullOrWhiteSpace(reason)) return; // cancelled or empty

            var specimen = SelectedSpecimen;
            try
            {
                specimen.Status = "Rejected";
                specimen.RejectionReason = reason;
                specimen.CollectionTime = DateTime.UtcNow;
                specimen.CollectedBy = CurrentStaffName;
                await _specimenRepo.UpdateAsync(specimen);
                await LoadDataAsync();

                await _auditLogRepo.AddAsync(new AuditLog
                {
                    Action = "Updated",
                    EntityType = "Specimen",
                    EntityId = specimen.SpecimenId,
                    UserId = StaffId,
                    Timestamp = DateTime.UtcNow,
                    Details = $"Specimen barcode {specimen.Barcode} REJECTED. Reason: {reason}."
                });

                MessageBox.Show("Specimen marked as Rejected.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to reject specimen.");
                MessageBox.Show("Error rejecting specimen.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteSaveCatalogDoctorAsync(object obj)
        {
            if (CatalogSelectedDoctor == null)
            {
                MessageBox.Show("Please select a doctor to edit.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(DocName))
            {
                MessageBox.Show("Doctor name cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                CatalogSelectedDoctor.Name = DocName;
                CatalogSelectedDoctor.Specialization = DocSpecialization;
                CatalogSelectedDoctor.ClinicName = DocClinicName;
                CatalogSelectedDoctor.ContactPhone = DocContactPhone;
                CatalogSelectedDoctor.CommissionPercent = DocCommissionPercent;

                await _doctorRepo.UpdateAsync(CatalogSelectedDoctor);
                
                await _auditLogRepo.AddAsync(new AuditLog
                {
                    Action = "Updated",
                    EntityType = "Doctor",
                    EntityId = CatalogSelectedDoctor.DoctorId,
                    UserId = StaffId,
                    Timestamp = DateTime.UtcNow,
                    Details = $"Updated doctor details for {CatalogSelectedDoctor.Name}."
                });

                await LoadDataAsync();
                MessageBox.Show("Doctor details updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to update doctor.");
                MessageBox.Show("Error updating doctor details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteAddCatalogDoctorAsync(object obj)
        {
            if (string.IsNullOrWhiteSpace(DocName))
            {
                MessageBox.Show("Doctor Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                var doc = new Doctor
                {
                    Name = DocName,
                    Specialization = DocSpecialization,
                    ClinicName = DocClinicName,
                    ContactPhone = DocContactPhone,
                    CommissionPercent = DocCommissionPercent
                };

                await _doctorRepo.AddAsync(doc);

                await _auditLogRepo.AddAsync(new AuditLog
                {
                    Action = "Created",
                    EntityType = "Doctor",
                    EntityId = doc.DoctorId,
                    UserId = StaffId,
                    Timestamp = DateTime.UtcNow,
                    Details = $"Added new referring doctor: {doc.Name}."
                });

                DocName = string.Empty;
                DocSpecialization = string.Empty;
                DocClinicName = string.Empty;
                DocContactPhone = string.Empty;
                DocCommissionPercent = 0;

                await LoadDataAsync();
                MessageBox.Show("Doctor added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add doctor.");
                MessageBox.Show("Error adding doctor.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EvaluatePatientReferenceRange(ResultInput ri, TestType testType, Patient patient)
        {
            if (testType != null && testType.ReferenceRanges != null && testType.ReferenceRanges.Count > 0 && patient != null)
            {
                int age = 30; // default
                if (patient.DateOfBirth.HasValue)
                {
                    var dob = patient.DateOfBirth.Value;
                    age = DateTime.Today.Year - dob.Year;
                    if (dob > DateTime.Today.AddYears(-age)) age--;
                    if (age < 0) age = 0;
                }
                string gender = patient.Gender ?? "All";

                var matchingRange = testType.ReferenceRanges.FirstOrDefault(r =>
                    (string.Equals(r.Gender, gender, StringComparison.OrdinalIgnoreCase) || string.Equals(r.Gender, "All", StringComparison.OrdinalIgnoreCase))
                    && age >= r.AgeMin && age <= r.AgeMax);

                if (matchingRange != null)
                {
                    ri.Low = matchingRange.RangeLow;
                    ri.High = matchingRange.RangeHigh;
                    return;
                }
            }

            // Fallback
            ri.Low = testType?.ReferenceRangeLow;
            ri.High = testType?.ReferenceRangeHigh;
        }
    }

    public static class RejectionDialog
    {
        public static string Show(string title, string prompt)
        {
            var window = new Window
            {
                Title = title,
                Width = 400,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow
            };

            var stack = new StackPanel { Margin = new Thickness(15) };
            
            var lbl = new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 10), FontWeight = FontWeights.Bold };
            stack.Children.Add(lbl);

            var txt = new TextBox { Height = 25, Margin = new Thickness(0, 0, 0, 15) };
            stack.Children.Add(txt);

            var btnStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnOk = new Button { Content = "OK", Width = 75, IsDefault = true, Margin = new Thickness(0, 0, 10, 0) };
            var btnCancel = new Button { Content = "Cancel", Width = 75, IsCancel = true };

            btnOk.Click += (s, e) => { window.DialogResult = true; window.Close(); };
            btnCancel.Click += (s, e) => { window.DialogResult = false; window.Close(); };

            btnStack.Children.Add(btnOk);
            btnStack.Children.Add(btnCancel);
            stack.Children.Add(btnStack);

            window.Content = stack;
            if (window.ShowDialog() == true)
            {
                return txt.Text;
            }
            return null;
        }
    }
}
