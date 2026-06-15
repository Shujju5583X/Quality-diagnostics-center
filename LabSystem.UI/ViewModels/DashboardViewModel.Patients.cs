using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LabSystem.Core.Models;
using Serilog;

namespace LabSystem.UI.ViewModels
{
    public partial class DashboardViewModel
    {
        // New Patient Bindings
        public string NewPatientName
        {
            get => _newPatientName;
            set { _newPatientName = value; OnPropertyChanged(); }
        }

        public int NewPatientAge
        {
            get => _newPatientAge;
            set { _newPatientAge = value; OnPropertyChanged(); }
        }

        public string NewPatientPhone
        {
            get => _newPatientPhone;
            set { _newPatientPhone = value; OnPropertyChanged(); }
        }

        public string NewPatientEmail
        {
            get => _newPatientEmail;
            set { _newPatientEmail = value; OnPropertyChanged(); }
        }

        public string NewPatientGender
        {
            get => _newPatientGender;
            set { _newPatientGender = value; OnPropertyChanged(); }
        }

        // Search & Pagination Bindings for Patients
        public string PatientSearchQuery
        {
            get => _patientSearchQuery;
            set
            {
                _patientSearchQuery = value;
                OnPropertyChanged();
                PatientCurrentPage = 1;
                _ = LoadPatientsAsync();
            }
        }

        public DateTime? PatientStartDate
        {
            get => _patientStartDate;
            set
            {
                _patientStartDate = value;
                OnPropertyChanged();
                PatientCurrentPage = 1;
                _ = LoadPatientsAsync();
            }
        }

        public DateTime? PatientEndDate
        {
            get => _patientEndDate;
            set
            {
                _patientEndDate = value;
                OnPropertyChanged();
                PatientCurrentPage = 1;
                _ = LoadPatientsAsync();
            }
        }

        public int PatientCurrentPage
        {
            get => _patientCurrentPage;
            set
            {
                _patientCurrentPage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PatientPageInfo));
            }
        }

        public int PatientTotalPages
        {
            get => _patientTotalPages;
            set
            {
                _patientTotalPages = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PatientPageInfo));
            }
        }

        public int PatientTotalCount
        {
            get => _patientTotalCount;
            set
            {
                _patientTotalCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PatientPageInfo));
            }
        }

        public string PatientPageInfo => $"Page {PatientCurrentPage} of {PatientTotalPages} (Total: {PatientTotalCount})";

        public async Task LoadPatientsAsync()
        {
            try
            {
                int totalCount = await _patientRepo.GetPatientsCountAsync(PatientSearchQuery, PatientStartDate, PatientEndDate);
                PatientTotalCount = totalCount;
                PatientTotalPages = (int)Math.Ceiling((double)totalCount / PageSize);
                if (PatientTotalPages == 0) PatientTotalPages = 1;

                if (PatientCurrentPage > PatientTotalPages) PatientCurrentPage = PatientTotalPages;
                if (PatientCurrentPage < 1) PatientCurrentPage = 1;

                var patients = await _patientRepo.SearchPatientsAsync(PatientSearchQuery, PatientStartDate, PatientEndDate, PatientCurrentPage, PageSize);
                
                Patients.Clear();
                foreach (var p in patients)
                {
                    Patients.Add(p);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to search patients in database.");
            }
        }

        private Patient _editingPatient;
        public string PatientFormTitle => _editingPatient == null ? "Register New Patient" : "Edit Patient";
        public string PatientFormButtonText => _editingPatient == null ? "REGISTER PATIENT" : "UPDATE PATIENT";

        private ICommand _editPatientCommand;
        public ICommand EditPatientCommand => _editPatientCommand ?? (_editPatientCommand = new RelayCommand(ExecuteEditPatient));

        private void ExecuteEditPatient(object obj)
        {
            if (obj is Patient patient)
            {
                _editingPatient = patient;
                NewPatientName = patient.FullName;
                NewPatientAge = patient.Age;
                NewPatientGender = patient.Gender;
                NewPatientPhone = patient.ContactPhone;
                NewPatientEmail = patient.ContactEmail;
                
                OnPropertyChanged(nameof(PatientFormTitle));
                OnPropertyChanged(nameof(PatientFormButtonText));
            }
        }

        private async Task ExecuteAddPatientAsync(object obj)
        {
            if (string.IsNullOrWhiteSpace(NewPatientName))
            {
                MessageBox.Show("Please enter the patient's full name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Phone number formatting / validation
            if (!string.IsNullOrWhiteSpace(NewPatientPhone))
            {
                var cleanPhone = new string(NewPatientPhone.Where(char.IsDigit).ToArray());
                if (cleanPhone.Length < 10 || cleanPhone.Length > 12)
                {
                    MessageBox.Show("Please enter a valid 10-12 digit phone number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Email validation
            if (!string.IsNullOrWhiteSpace(NewPatientEmail))
            {
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                if (!emailRegex.IsMatch(NewPatientEmail))
                {
                    MessageBox.Show("Please enter a valid email address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            try
            {
                if (_editingPatient != null)
                {
                    // Update existing patient
                    _editingPatient.FullName = NewPatientName.Trim();
                    _editingPatient.Age = NewPatientAge;
                    _editingPatient.Gender = NewPatientGender ?? "Male";
                    _editingPatient.ContactPhone = NewPatientPhone ?? "";
                    _editingPatient.ContactEmail = NewPatientEmail ?? "";

                    await _patientRepo.UpdateAsync(_editingPatient);
                    Log.Information("Updated patient: {PatientName} with UHID {Uhid}", NewPatientName, _editingPatient.Uhid);
                    MessageBox.Show("Patient information updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    _editingPatient = null;
                    OnPropertyChanged(nameof(PatientFormTitle));
                    OnPropertyChanged(nameof(PatientFormButtonText));
                }
                else
                {
                    // Duplicate check
                    var patients = await _patientRepo.GetAllAsync();
                    var exists = patients.Any(p => 
                        p.FullName.Equals(NewPatientName, StringComparison.OrdinalIgnoreCase) && 
                        p.ContactPhone == (NewPatientPhone ?? ""));

                    if (exists)
                    {
                        var dialogResult = MessageBox.Show("A patient with this name and phone number is already registered. Register anyway?", "Duplicate Patient", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (dialogResult == MessageBoxResult.No)
                        {
                            return;
                        }
                    }

                    // Generate UHID
                    var uhid = await GenerateNextUhidAsync();

                    var patient = new Patient
                    {
                        Uhid = uhid,
                        FullName = NewPatientName,
                        Age = NewPatientAge,
                        Gender = NewPatientGender ?? "Male",
                        ContactPhone = NewPatientPhone ?? "",
                        ContactEmail = NewPatientEmail ?? "",
                        CreatedAt = DateTime.UtcNow
                    };

                    await _patientRepo.AddAsync(patient);
                    Log.Information("Added patient: {PatientName} with UHID {Uhid}", NewPatientName, uhid);
                    MessageBox.Show($"Patient registered successfully!\nUHID: {uhid}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Reset fields
                NewPatientName = string.Empty;
                NewPatientAge = 0;
                NewPatientPhone = string.Empty;
                NewPatientEmail = string.Empty;
                NewPatientGender = "Male";

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save patient.");
                MessageBox.Show("Error saving patient to database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<string> GenerateNextUhidAsync()
        {
            var currentYear = DateTime.Now.Year;
            string maxUhid = await _patientRepo.GetMaxUhidForYearAsync(currentYear);
            int seq = 1;
            if (maxUhid != null)
            {
                var parts = maxUhid.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int parsedSeq))
                {
                    seq = parsedSeq + 1;
                }
            }
            return $"QDC-{currentYear}-{seq:D5}";
        }
    }
}
