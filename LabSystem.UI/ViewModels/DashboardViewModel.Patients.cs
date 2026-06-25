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

        private System.Collections.Generic.List<string> _titles;
        public System.Collections.Generic.List<string> Titles
        {
            get
            {
                if (_titles == null)
                {
                    _titles = new System.Collections.Generic.List<string> { "Select Title", "Mr", "Mrs", "Ms", "Master", "Baby", "Baby of" };
                }
                return _titles;
            }
        }

        public string NewPatientTitle
        {
            get { return _newPatientTitle; }
            set
            {
                _newPatientTitle = value;
                OnPropertyChanged();
                OnPropertyChanged("IsDetailedAgeVisible");
                OnPropertyChanged("IsSimpleAgeVisible");

                // Auto-gender selection
                if (_newPatientTitle == "Mr" || _newPatientTitle == "Master")
                {
                    NewPatientGender = "Male";
                }
                else if (_newPatientTitle == "Mrs" || _newPatientTitle == "Ms")
                {
                    NewPatientGender = "Female";
                }
            }
        }

        public string NewPatientAgeYears
        {
            get { return _newPatientAgeYears; }
            set { _newPatientAgeYears = value; OnPropertyChanged(); }
        }

        public string NewPatientAgeMonths
        {
            get { return _newPatientAgeMonths; }
            set { _newPatientAgeMonths = value; OnPropertyChanged(); }
        }

        public string NewPatientAgeDays
        {
            get { return _newPatientAgeDays; }
            set { _newPatientAgeDays = value; OnPropertyChanged(); }
        }

        public bool IsDetailedAgeVisible
        {
            get
            {
                return _newPatientTitle == "Master" || _newPatientTitle == "Baby" || _newPatientTitle == "Baby of";
            }
        }

        public bool IsSimpleAgeVisible
        {
            get
            {
                return !IsDetailedAgeVisible;
            }
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
                NewPatientTitle = patient.Title ?? "Select Title";
                NewPatientAgeYears = patient.AgeYears.HasValue ? patient.AgeYears.Value.ToString() : "";
                NewPatientAgeMonths = patient.AgeMonths.HasValue ? patient.AgeMonths.Value.ToString() : "";
                NewPatientAgeDays = patient.AgeDays.HasValue ? patient.AgeDays.Value.ToString() : "";
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

            if (string.IsNullOrEmpty(NewPatientTitle) || NewPatientTitle == "Select Title")
            {
                MessageBox.Show("Please select a patient title.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            int? years = null;
            int? months = null;
            int? days = null;

            if (NewPatientTitle == "Master" || NewPatientTitle == "Baby" || NewPatientTitle == "Baby of")
            {
                if (string.IsNullOrWhiteSpace(NewPatientAgeYears) && 
                    string.IsNullOrWhiteSpace(NewPatientAgeMonths) && 
                    string.IsNullOrWhiteSpace(NewPatientAgeDays))
                {
                    MessageBox.Show("Please enter at least one age value (Years, Months, or Days).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(NewPatientAgeYears))
                {
                    int y;
                    if (!int.TryParse(NewPatientAgeYears, out y) || y < 0)
                    {
                        MessageBox.Show("Please enter a valid non-negative integer for Years.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    years = y;
                }

                if (!string.IsNullOrWhiteSpace(NewPatientAgeMonths))
                {
                    int m;
                    if (!int.TryParse(NewPatientAgeMonths, out m) || m < 0)
                    {
                        MessageBox.Show("Please enter a valid non-negative integer for Months.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    months = m;
                }

                if (!string.IsNullOrWhiteSpace(NewPatientAgeDays))
                {
                    int d;
                    if (!int.TryParse(NewPatientAgeDays, out d) || d < 0)
                    {
                        MessageBox.Show("Please enter a valid non-negative integer for Days.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    days = d;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(NewPatientAgeYears))
                {
                    MessageBox.Show("Please enter the patient's age in years.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int y;
                if (!int.TryParse(NewPatientAgeYears, out y) || y < 0)
                {
                    MessageBox.Show("Please enter a valid non-negative integer for Age.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                years = y;
            }

            var dob = DateTime.Today.AddYears(-(years ?? 0)).AddMonths(-(months ?? 0)).AddDays(-(days ?? 0));

            try
            {
                if (_editingPatient != null)
                {
                    // Update existing patient
                    _editingPatient.FullName = NewPatientName.Trim();
                    _editingPatient.Title = NewPatientTitle;
                    _editingPatient.AgeYears = years;
                    _editingPatient.AgeMonths = months;
                    _editingPatient.AgeDays = days;
                    _editingPatient.Gender = NewPatientGender ?? "Male";
                    _editingPatient.ContactPhone = NewPatientPhone ?? "";
                    _editingPatient.ContactEmail = NewPatientEmail ?? "";
                    _editingPatient.DateOfBirth = dob;

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
                        Title = NewPatientTitle,
                        AgeYears = years,
                        AgeMonths = months,
                        AgeDays = days,
                        Gender = NewPatientGender ?? "Male",
                        ContactPhone = NewPatientPhone ?? "",
                        ContactEmail = NewPatientEmail ?? "",
                        CreatedAt = DateTime.UtcNow,
                        DateOfBirth = dob
                    };

                    await _patientRepo.AddAsync(patient);
                    Log.Information("Added patient: {PatientName} with UHID {Uhid}", NewPatientName, uhid);
                    MessageBox.Show("Patient registered successfully!\nUHID: " + uhid, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Reset fields
                NewPatientName = string.Empty;
                NewPatientTitle = "Select Title";
                NewPatientAgeYears = string.Empty;
                NewPatientAgeMonths = string.Empty;
                NewPatientAgeDays = string.Empty;
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

        private async Task ExecuteDeletePatientAsync()
        {
            if (SelectedPatient == null)
            {
                MessageBox.Show("Please select a patient to delete.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var patient = SelectedPatient;
            var hasOrders = Orders.Any(o => o.PatientId == patient.PatientId);

            string message = "Are you sure you want to delete patient '" + patient.FullName + "' (UHID: " + patient.Uhid + ")?";
            if (hasOrders)
            {
                message += "\n\nThis patient has existing orders. Deleting will also remove all associated orders, results, and invoices.";
            }

            var dialogResult = MessageBox.Show(message, "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (dialogResult != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                // Cascade delete: remove orders and their children first
                if (hasOrders)
                {
                    var patientOrders = Orders.Where(o => o.PatientId == patient.PatientId).ToList();
                    foreach (var order in patientOrders)
                    {
                        // Delete results for this order
                        var results = await _resultRepo.GetResultsForOrderAsync(order.OrderId);
                        foreach (var result in results)
                        {
                            await _resultRepo.DeleteAsync(result.ResultId);
                        }

                        // Delete invoice for this order
                        var invoice = await _billingService.GetInvoiceForOrderAsync(order.OrderId);
                        if (invoice != null)
                        {
                            await _invoiceRepo.DeleteAsync(invoice.InvoiceId);
                        }

                        // Delete the order
                        await _orderRepo.DeleteAsync(order.OrderId);
                    }
                }

                await _patientRepo.DeleteAsync(patient.PatientId);
                Log.Information("Deleted patient: {PatientName} (UHID: {Uhid}, ID: {PatientId})", patient.FullName, patient.Uhid, patient.PatientId);

                SelectedPatient = null;
                NewPatientName = string.Empty;
                NewPatientTitle = "Select Title";
                NewPatientAgeYears = string.Empty;
                NewPatientAgeMonths = string.Empty;
                NewPatientAgeDays = string.Empty;
                NewPatientPhone = string.Empty;
                NewPatientEmail = string.Empty;
                NewPatientGender = "Male";

                OnPropertyChanged("PatientFormTitle");
                OnPropertyChanged("PatientFormButtonText");

                await LoadDataAsync();
                MessageBox.Show("Patient deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete patient.");
                MessageBox.Show("Error deleting patient: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
