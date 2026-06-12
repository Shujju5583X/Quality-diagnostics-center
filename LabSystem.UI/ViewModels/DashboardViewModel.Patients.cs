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

        public DateTime? NewPatientDOB
        {
            get => _newPatientDOB;
            set { _newPatientDOB = value; OnPropertyChanged(); }
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
                    DateOfBirth = NewPatientDOB,
                    Gender = NewPatientGender ?? "Male",
                    ContactPhone = NewPatientPhone ?? "",
                    ContactEmail = NewPatientEmail ?? "",
                    CreatedAt = DateTime.UtcNow
                };

                await _patientRepo.AddAsync(patient);
                Log.Information("Added patient: {PatientName} with UHID {Uhid}", NewPatientName, uhid);

                await _auditLogRepo.AddAsync(new AuditLog
                {
                    Action = "Created",
                    EntityType = "Patient",
                    EntityId = patient.PatientId,
                    UserId = StaffId,
                    Timestamp = DateTime.UtcNow,
                    Details = $"Registered patient '{NewPatientName}' (UHID: {uhid})."
                });

                // Reset fields
                NewPatientName = string.Empty;
                NewPatientDOB = null;
                NewPatientPhone = string.Empty;
                NewPatientEmail = string.Empty;
                NewPatientGender = "Male";

                await LoadDataAsync();
                MessageBox.Show($"Patient registered successfully!\nUHID: {uhid}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add patient.");
                MessageBox.Show("Error adding patient to database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<string> GenerateNextUhidAsync()
        {
            var currentYear = DateTime.Now.Year;
            var startOfYear = new DateTime(currentYear, 1, 1);
            var endOfYear = new DateTime(currentYear, 12, 31);
            
            // Count registered this year
            int countThisYear = await _patientRepo.GetPatientsCountAsync("", startOfYear, endOfYear);
            int seq = countThisYear + 1;
            string uhid = $"QDC-{currentYear}-{seq:D5}";

            // Guarantee uniqueness
            var all = await _patientRepo.GetAllAsync();
            while (all.Any(p => p.Uhid == uhid))
            {
                seq++;
                uhid = $"QDC-{currentYear}-{seq:D5}";
            }

            return uhid;
        }
    }
}
