using System;
using System.Collections.Generic;
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
    public class DashboardViewModel : ViewModelBase
    {
        private readonly IPatientRepository _patientRepo;
        private readonly ITestOrderRepository _orderRepo;
        private readonly IResultRepository _resultRepo;
        private readonly IRepository<TestType> _testTypeRepo;
        private readonly IRepository<Staff> _staffRepo;
        private readonly IOrderService _orderService;
        private readonly IResultService _resultService;
        private readonly IPdfReportService _reportService;
        private readonly IBackupService _backupService;

        private int _staffId;
        private string _currentStaffName;
        private Patient _selectedPatient;
        private TestOrder _selectedOrder;
        
        // Patient tab fields
        private string _newPatientName;
        private DateTime? _newPatientDOB;
        private string _newPatientPhone;
        private string _newPatientEmail;

        // Order tab fields
        private string _orderNotes;

        // Results tab fields
        private string _resultErrorMessage;

        public int StaffId
        {
            get => _staffId;
            set
            {
                _staffId = value;
                LoadStaffName();
                OnPropertyChanged();
            }
        }

        public string CurrentStaffName
        {
            get => _currentStaffName;
            set { _currentStaffName = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Patient> Patients { get; } = new ObservableCollection<Patient>();
        public ObservableCollection<TestOrder> Orders { get; } = new ObservableCollection<TestOrder>();
        public ObservableCollection<TestTypeSelection> TestTypes { get; } = new ObservableCollection<TestTypeSelection>();
        
        // Items for entering results for the selected order
        public ObservableCollection<ResultInput> SelectedOrderResults { get; } = new ObservableCollection<ResultInput>();

        public Patient SelectedPatient
        {
            get => _selectedPatient;
            set { _selectedPatient = value; OnPropertyChanged(); }
        }

        public TestOrder SelectedOrder
        {
            get => _selectedOrder;
            set 
            { 
                _selectedOrder = value; 
                OnPropertyChanged();
                LoadResultsForSelectedOrder();
            }
        }

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

        // Create Order Bindings
        public string OrderNotes
        {
            get => _orderNotes;
            set { _orderNotes = value; OnPropertyChanged(); }
        }

        public string ResultErrorMessage
        {
            get => _resultErrorMessage;
            set { _resultErrorMessage = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand AddPatientCommand { get; }
        public ICommand CreateOrderCommand { get; }
        public ICommand SaveResultsCommand { get; }
        public ICommand GenerateReportCommand { get; }
        public ICommand BackupCommand { get; }
        public ICommand RefreshCommand { get; }

        public DashboardViewModel(
            IPatientRepository patientRepo,
            ITestOrderRepository orderRepo,
            IResultRepository resultRepo,
            IRepository<TestType> testTypeRepo,
            IRepository<Staff> staffRepo,
            IOrderService orderService,
            IResultService resultService,
            IPdfReportService reportService,
            IBackupService backupService)
        {
            _patientRepo = patientRepo;
            _orderRepo = orderRepo;
            _resultRepo = resultRepo;
            _testTypeRepo = testTypeRepo;
            _staffRepo = staffRepo;
            _orderService = orderService;
            _resultService = resultService;
            _reportService = reportService;
            _backupService = backupService;

            AddPatientCommand = new RelayCommand(ExecuteAddPatient);
            CreateOrderCommand = new RelayCommand(ExecuteCreateOrder);
            SaveResultsCommand = new RelayCommand(ExecuteSaveResults);
            GenerateReportCommand = new RelayCommand(ExecuteGenerateReport);
            BackupCommand = new RelayCommand(ExecuteBackup);
            RefreshCommand = new RelayCommand(o => LoadData());

            LoadData();
        }

        private async void LoadStaffName()
        {
            try
            {
                var staff = await _staffRepo.GetByIdAsync(StaffId);
                CurrentStaffName = staff?.FullName ?? "Unknown Staff";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load staff details.");
                CurrentStaffName = "Admin User";
            }
        }

        private async void LoadData()
        {
            try
            {
                // Load Patients
                Patients.Clear();
                var patients = await _patientRepo.GetAllAsync();
                foreach (var p in patients)
                {
                    Patients.Add(p);
                }

                // Load Test Types for checkboxes
                TestTypes.Clear();
                var testTypes = await _testTypeRepo.GetAllAsync();
                foreach (var t in testTypes)
                {
                    if (t.IsActive)
                    {
                        TestTypes.Add(new TestTypeSelection 
                        { 
                            TypeId = t.TypeId, 
                            Name = t.Name, 
                            Unit = t.Unit, 
                            Low = t.ReferenceRangeLow, 
                            High = t.ReferenceRangeHigh,
                            GroupName = t.GroupName,
                            Category = t.Category
                        });
                    }
                }

                // Load Orders
                Orders.Clear();
                var orders = await _orderRepo.GetAllAsync();
                foreach (var o in orders)
                {
                    // EF handles relation, but double-check if patient needs to be populated manually
                    if (o.Patient == null)
                    {
                        o.Patient = await _patientRepo.GetByIdAsync(o.PatientId);
                    }
                    Orders.Add(o);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load dashboard data.");
                MessageBox.Show("Error loading data from database. See logs.", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteAddPatient(object obj)
        {
            if (string.IsNullOrWhiteSpace(NewPatientName))
            {
                MessageBox.Show("Please enter the patient's full name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var patient = new Patient
                {
                    FullName = NewPatientName,
                    DateOfBirth = NewPatientDOB?.ToString("yyyy-MM-dd") ?? "",
                    ContactPhone = NewPatientPhone ?? "",
                    ContactEmail = NewPatientEmail ?? "",
                    CreatedAt = DateTime.UtcNow.ToString("O")
                };

                await _patientRepo.AddAsync(patient);
                Log.Information("Added patient: {PatientName}", NewPatientName);

                // Reset fields
                NewPatientName = string.Empty;
                NewPatientDOB = null;
                NewPatientPhone = string.Empty;
                NewPatientEmail = string.Empty;

                LoadData();
                MessageBox.Show("Patient added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add patient.");
                MessageBox.Show("Error adding patient to database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteCreateOrder(object obj)
        {
            if (SelectedPatient == null)
            {
                MessageBox.Show("Please select a patient from the grid on the left.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedTests = TestTypes.Where(t => t.IsSelected).ToList();
            if (!selectedTests.Any())
            {
                MessageBox.Show("Please select at least one test to order.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Store selected test IDs in Notes as comma-separated string
                string testIds = string.Join(",", selectedTests.Select(t => t.TypeId));
                
                var order = new TestOrder
                {
                    PatientId = SelectedPatient.PatientId,
                    Status = "Pending",
                    Notes = testIds
                };

                await _orderService.CreateOrderAsync(order);
                Log.Information("Created test order ID {OrderId} for Patient ID {PatientId}", order.OrderId, SelectedPatient.PatientId);

                // Unselect test check boxes
                foreach (var t in TestTypes)
                {
                    t.IsSelected = false;
                }

                LoadData();
                MessageBox.Show("Test order created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create order.");
                MessageBox.Show("Error creating test order.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadResultsForSelectedOrder()
        {
            SelectedOrderResults.Clear();
            ResultErrorMessage = string.Empty;

            if (SelectedOrder == null) return;

            try
            {
                if (SelectedOrder.Status == "Pending")
                {
                    // Parse the requested TestType IDs from Notes
                    if (!string.IsNullOrWhiteSpace(SelectedOrder.Notes))
                    {
                        var ids = SelectedOrder.Notes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var idStr in ids)
                        {
                            if (int.TryParse(idStr, out int id))
                            {
                                var testType = await _testTypeRepo.GetByIdAsync(id);
                                if (testType != null)
                                {
                                    SelectedOrderResults.Add(new ResultInput
                                    {
                                        TypeId = testType.TypeId,
                                        TestName = testType.Name,
                                        Unit = testType.Unit,
                                        Low = testType.ReferenceRangeLow,
                                        High = testType.ReferenceRangeHigh,
                                        IsAbnormal = false,
                                        IsReadOnly = false
                                    });
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Order is complete, load the actual saved results
                    var savedResults = await _resultRepo.GetResultsForOrderAsync(SelectedOrder.OrderId);
                    foreach (var r in savedResults)
                    {
                        if (r.TestType == null)
                        {
                            r.TestType = await _testTypeRepo.GetByIdAsync(r.TypeId);
                        }

                        SelectedOrderResults.Add(new ResultInput
                        {
                            TypeId = r.TypeId,
                            TestName = r.TestType?.Name ?? "Unknown Test",
                            Unit = r.TestType?.Unit ?? "",
                            Low = r.TestType?.ReferenceRangeLow,
                            High = r.TestType?.ReferenceRangeHigh,
                            ValueText = r.Value.ToString(),
                            IsAbnormal = r.IsAbnormal,
                            IsReadOnly = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load order results.");
            }
        }

        private async void ExecuteSaveResults(object obj)
        {
            if (SelectedOrder == null) return;

            if (SelectedOrder.Status != "Pending")
            {
                MessageBox.Show("Results have already been verified and saved for this order.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Validate all inputs are numeric
            foreach (var r in SelectedOrderResults)
            {
                if (string.IsNullOrWhiteSpace(r.ValueText) || !double.TryParse(r.ValueText, out _))
                {
                    ResultErrorMessage = $"Please enter a valid numeric value for {r.TestName}.";
                    return;
                }
            }

            try
            {
                // Save each result
                foreach (var r in SelectedOrderResults)
                {
                    double val = double.Parse(r.ValueText);
                    var result = new Result
                    {
                        OrderId = SelectedOrder.OrderId,
                        TypeId = r.TypeId,
                        Value = val,
                        TechnicianId = StaffId
                    };

                    // Calling service logic which calculates abnormal flag and saves to DB
                    await _resultService.AddResultAsync(result);
                }

                int selectedOrderId = SelectedOrder.OrderId;

                // Update order status to Complete
                await _orderService.UpdateOrderStatusAsync(selectedOrderId, "Complete");
                Log.Information("Verified and completed order {OrderId}", selectedOrderId);

                // Reload
                LoadData();
                
                // Select the order again to refresh results grid as read-only
                SelectedOrder = Orders.FirstOrDefault(o => o.OrderId == selectedOrderId);
                
                MessageBox.Show("Results verified and saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Generate and open the PDF report immediately
                try
                {
                    if (SelectedOrder != null)
                    {
                        string path = await _reportService.GenerateReportAsync(SelectedOrder);
                        Log.Information("Automatically generated PDF report for order {OrderId} at {Path}", SelectedOrder.OrderId, path);
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to automatically generate PDF report after saving results.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save results.");
                MessageBox.Show("Error saving results to database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteGenerateReport(object obj)
        {
            if (SelectedOrder == null)
            {
                MessageBox.Show("Please select an order from the list.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedOrder.Status != "Complete")
            {
                MessageBox.Show("Reports can only be generated for Complete orders.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string path = await _reportService.GenerateReportAsync(SelectedOrder);
                Log.Information("Generated PDF report for order {OrderId} at {Path}", SelectedOrder.OrderId, path);
                MessageBox.Show($"PDF Report generated successfully!\nSaved to: {path}", "Report Generated", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to generate report.");
                MessageBox.Show("Error generating PDF report.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteBackup(object obj)
        {
            try
            {
                await _backupService.BackupNowAsync();
                MessageBox.Show("Database (SQLite) and technician-friendly report (Excel) backed up successfully to the backups directory!", "Backup Completed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Database backup failed.");
                MessageBox.Show("Failed to complete database backup. Check logs for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Helper classes for lists and bindings
    public class TestTypeSelection : ViewModelBase
    {
        public int TypeId { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public double? Low { get; set; }
        public double? High { get; set; }
        public string GroupName { get; set; }
        public string Category { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }
    }

    public class ResultInput : ViewModelBase
    {
        public int TypeId { get; set; }
        public string TestName { get; set; }
        public string Unit { get; set; }
        public double? Low { get; set; }
        public double? High { get; set; }

        private string _valueText;
        public string ValueText
        {
            get => _valueText;
            set { _valueText = value; OnPropertyChanged(); }
        }

        private bool _isAbnormal;
        public bool IsAbnormal
        {
            get => _isAbnormal;
            set { _isAbnormal = value; OnPropertyChanged(); }
        }

        private bool _isReadOnly;
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set { _isReadOnly = value; OnPropertyChanged(); }
        }
    }
}
