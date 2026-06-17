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
            get { return _newPatientName; }
            set { _newPatientName = value; OnPropertyChanged(); }
        }

        public int NewPatientAge
        {
            get { return _newPatientAge; }
            set { _newPatientAge = value; OnPropertyChanged(); }
        }

        public string NewPatientPhone
        {
            get { return _newPatientPhone; }
            set { _newPatientPhone = value; OnPropertyChanged(); }
        }

        public string NewPatientEmail
        {
            get { return _newPatientEmail; }
            set { _newPatientEmail = value; OnPropertyChanged(); }
        }

        public string NewPatientGender
        {
            get { return _newPatientGender; }
            set { _newPatientGender = value; OnPropertyChanged(); }
        }

        // Search & Pagination Bindings for Patients
        public string PatientSearchQuery
        {
            get { return _patientSearchQuery; }
            set
            {
                _patientSearchQuery = value;
                OnPropertyChanged();
                PatientCurrentPage = 1;
                var unused = LoadPatientsAsync();
            }
        }

        public DateTime? PatientStartDate
        {
            get { return _patientStartDate; }
            set
            {
                _patientStartDate = value;
                OnPropertyChanged();
                PatientCurrentPage = 1;
                var unused = LoadPatientsAsync();
            }
        }

        public DateTime? PatientEndDate
        {
            get { return _patientEndDate; }
            set
            {
                _patientEndDate = value;
                OnPropertyChanged();
                PatientCurrentPage = 1;
                var unused = LoadPatientsAsync();
            }
        }

        public int PatientCurrentPage
        {
            get { return _patientCurrentPage; }
            set
            {
                _patientCurrentPage = value;
                OnPropertyChanged();
                OnPropertyChanged("PatientPageInfo");
            }
        }

        public int PatientTotalPages
        {
            get { return _patientTotalPages; }
            set
            {
                _patientTotalPages = value;
                OnPropertyChanged();
                OnPropertyChanged("PatientPageInfo");
            }
        }

        public int PatientTotalCount
        {
            get { return _patientTotalCount; }
            set
            {
                _patientTotalCount = value;
                OnPropertyChanged();
                OnPropertyChanged("PatientPageInfo");
            }
        }

        public string PatientPageInfo
        {
            get { return "Page " + PatientCurrentPage + " of " + PatientTotalPages + " (Total: " + PatientTotalCount + ")"; }
        }

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
        public string PatientFormTitle
        {
            get { return _editingPatient == null ? "Register New Patient" : "Edit Patient"; }
        }
        public string PatientFormButtonText
        {
            get { return _editingPatient == null ? "REGISTER PATIENT" : "UPDATE PATIENT"; }
        }

        private ICommand _editPatientCommand;
        public ICommand EditPatientCommand
        {
            get
            {
                if (_editPatientCommand == null)
                {
                    _editPatientCommand = new RelayCommand(ExecuteEditPatient);
                }
                return _editPatientCommand;
            }
        }

        private void ExecuteEditPatient(object obj)
        {
            var patient = obj as Patient;
            if (patient != null)
            {
                _editingPatient = patient;
                NewPatientName = patient.FullName;
                NewPatientAge = patient.Age;
                NewPatientGender = patient.Gender;
                NewPatientPhone = patient.ContactPhone;
                NewPatientEmail = patient.ContactEmail;
                
                OnPropertyChanged("PatientFormTitle");
                OnPropertyChanged("PatientFormButtonText");
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
                    OnPropertyChanged("PatientFormTitle");
                    OnPropertyChanged("PatientFormButtonText");
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
                    MessageBox.Show("Patient registered successfully!\nUHID: " + uhid, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
                int parsedSeq;
                if (parts.Length == 3 && int.TryParse(parts[2], out parsedSeq))
                {
                    seq = parsedSeq + 1;
                }
            }
            return string.Format("QDC-{0}-{1:D5}", currentYear, seq);
        }

        private async Task ExecuteLoadPatientHistoryAsync()
        {
            if (SelectedPatient == null)
            {
                MessageBox.Show("Please select a patient first.", "No Patient Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                PatientHistory.Clear();
                PatientHistoryName = SelectedPatient.FullName;

                var orders = await _patientRepo.GetPatientOrdersAsync(SelectedPatient.PatientId);
                foreach (var order in orders)
                {
                    var results = await _resultRepo.GetResultsForOrderAsync(order.OrderId);
                    foreach (var result in results)
                    {
                        if (result.TestType == null)
                        {
                            result.TestType = await _testTypeRepo.GetByIdAsync(result.TypeId);
                        }

                        PatientHistory.Add(new PatientHistoryEntry
                        {
                            OrderDate = order.OrderedAt,
                            TestName = result.TestType != null ? result.TestType.Name ?? "Unknown" : "Unknown",
                            Value = result.Value,
                            ValueText = result.ValueText,
                            Unit = result.TestType != null ? result.TestType.Unit ?? "" : "",
                            IsAbnormal = result.IsAbnormal,
                            ReferenceLow = result.TestType != null ? result.TestType.ReferenceRangeLow : null,
                            ReferenceHigh = result.TestType != null ? result.TestType.ReferenceRangeHigh : null
                        });
                    }
                }

                PatientHistoryCount = PatientHistory.Count;

                var historyWindow = new Views.PatientHistoryWindow();
                historyWindow.DataContext = this;
                historyWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load patient history.");
                MessageBox.Show("Failed to load patient history: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
