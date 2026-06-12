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

        public string DocName { get => _docName; set { _docName = value; OnPropertyChanged(); } }
        public string DocSpecialization { get => _docSpecialization; set { _docSpecialization = value; OnPropertyChanged(); } }
        public string DocClinicName { get => _docClinicName; set { _docClinicName = value; OnPropertyChanged(); } }
        public string DocContactPhone { get => _docContactPhone; set { _docContactPhone = value; OnPropertyChanged(); } }

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
            }
            else
            {
                DocName = string.Empty;
                DocSpecialization = string.Empty;
                DocClinicName = string.Empty;
                DocContactPhone = string.Empty;
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

                await _doctorRepo.UpdateAsync(CatalogSelectedDoctor);

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
                    ContactPhone = DocContactPhone
                };

                await _doctorRepo.AddAsync(doc);

                DocName = string.Empty;
                DocSpecialization = string.Empty;
                DocClinicName = string.Empty;
                DocContactPhone = string.Empty;

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

}
