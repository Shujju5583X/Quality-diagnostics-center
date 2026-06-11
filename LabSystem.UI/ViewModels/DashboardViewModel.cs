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
using LabSystem.Services;

namespace LabSystem.UI.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly IPatientRepository _patientRepo;
        private readonly ITestOrderRepository _orderRepo;
        private readonly IResultRepository _resultRepo;
        private readonly IRepository<TestType> _testTypeRepo;
        private readonly IRepository<Staff> _staffRepo;
        private readonly IRepository<AuditLog> _auditLogRepo;
        private readonly IOrderService _orderService;
        private readonly IResultService _resultService;
        private readonly IPdfReportService _reportService;
        private readonly IBackupService _backupService;
        private readonly IBillingService _billingService;

        private int _staffId;
        private string _currentStaffName;
        private bool _isAdmin;
        private Patient _selectedPatient;
        private TestOrder _selectedOrder;
        
        // Patient tab fields
        private string _newPatientName;
        private DateTime? _newPatientDOB;
        private string _newPatientPhone;
        private string _newPatientEmail;
        private string _newPatientGender = "Male";

        // Order tab fields
        private string _orderNotes;
        private string _orderReferredBy = "SELF";

        // Results tab fields
        private string _resultErrorMessage;

        // Dashboard stats fields
        private int _totalPatients;
        private int _pendingOrders;
        private int _completedOrders;
        private int _abnormalResultsFlagged;

        // Catalog Management fields
        private TestType _selectedCatalogTest;
        private string _catalogTestName;
        private string _catalogTestUnit;
        private double? _catalogTestLow;
        private double? _catalogTestHigh;
        private bool _catalogTestIsActive = true;
        private string _catalogTestCategory;
        private string _catalogTestGroupName;
        private string _catalogTestMethod;
        private string _catalogTestInterpretation;
        private int _catalogTestSortOrder;

        // Audit Log search fields
        private string _auditLogSearchQuery;

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

        public bool IsAdmin
        {
            get => _isAdmin;
            set { _isAdmin = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Patient> Patients { get; } = new ObservableCollection<Patient>();
        public ObservableCollection<TestOrder> Orders { get; } = new ObservableCollection<TestOrder>();
        public ObservableCollection<TestTypeSelection> TestTypes { get; } = new ObservableCollection<TestTypeSelection>();
        
        // Items for entering results for the selected order
        public ObservableCollection<ResultInput> SelectedOrderResults { get; } = new ObservableCollection<ResultInput>();

        // Catalog Management items
        public ObservableCollection<TestType> CatalogTestTypes { get; } = new ObservableCollection<TestType>();

        // Billing Items
        public ObservableCollection<Invoice> Invoices { get; } = new ObservableCollection<Invoice>();

        // Audit Logs items
        public ObservableCollection<AuditLog> AuditLogs { get; } = new ObservableCollection<AuditLog>();

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

        private Invoice _selectedInvoice;
        public Invoice SelectedInvoice
        {
            get => _selectedInvoice;
            set { _selectedInvoice = value; OnPropertyChanged(); }
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

        public string NewPatientGender
        {
            get => _newPatientGender;
            set { _newPatientGender = value; OnPropertyChanged(); }
        }

        // Create Order Bindings
        public string OrderNotes
        {
            get => _orderNotes;
            set { _orderNotes = value; OnPropertyChanged(); }
        }

        public string OrderReferredBy
        {
            get => _orderReferredBy;
            set { _orderReferredBy = value; OnPropertyChanged(); }
        }

        public string ResultErrorMessage
        {
            get => _resultErrorMessage;
            set { _resultErrorMessage = value; OnPropertyChanged(); }
        }

        // Dashboard Stats Bindings
        public int TotalPatients
        {
            get => _totalPatients;
            set { _totalPatients = value; OnPropertyChanged(); }
        }

        public int PendingOrders
        {
            get => _pendingOrders;
            set { _pendingOrders = value; OnPropertyChanged(); }
        }

        public int CompletedOrders
        {
            get => _completedOrders;
            set { _completedOrders = value; OnPropertyChanged(); }
        }

        public int AbnormalResultsFlagged
        {
            get => _abnormalResultsFlagged;
            set { _abnormalResultsFlagged = value; OnPropertyChanged(); }
        }

        // Catalog Management Bindings
        public TestType SelectedCatalogTest
        {
            get => _selectedCatalogTest;
            set
            {
                _selectedCatalogTest = value;
                OnPropertyChanged();
                PopulateCatalogEditFields();
            }
        }

        public string CatalogTestName
        {
            get => _catalogTestName;
            set { _catalogTestName = value; OnPropertyChanged(); }
        }

        public string CatalogTestUnit
        {
            get => _catalogTestUnit;
            set { _catalogTestUnit = value; OnPropertyChanged(); }
        }

        public double? CatalogTestLow
        {
            get => _catalogTestLow;
            set { _catalogTestLow = value; OnPropertyChanged(); }
        }

        public double? CatalogTestHigh
        {
            get => _catalogTestHigh;
            set { _catalogTestHigh = value; OnPropertyChanged(); }
        }

        public bool CatalogTestIsActive
        {
            get => _catalogTestIsActive;
            set { _catalogTestIsActive = value; OnPropertyChanged(); }
        }

        public string CatalogTestCategory
        {
            get => _catalogTestCategory;
            set { _catalogTestCategory = value; OnPropertyChanged(); }
        }

        public string CatalogTestGroupName
        {
            get => _catalogTestGroupName;
            set { _catalogTestGroupName = value; OnPropertyChanged(); }
        }

        public string CatalogTestMethod
        {
            get => _catalogTestMethod;
            set { _catalogTestMethod = value; OnPropertyChanged(); }
        }

        public string CatalogTestInterpretation
        {
            get => _catalogTestInterpretation;
            set { _catalogTestInterpretation = value; OnPropertyChanged(); }
        }

        public int CatalogTestSortOrder
        {
            get => _catalogTestSortOrder;
            set { _catalogTestSortOrder = value; OnPropertyChanged(); }
        }

        // Audit Logs Bindings
        public string AuditLogSearchQuery
        {
            get => _auditLogSearchQuery;
            set { _auditLogSearchQuery = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand AddPatientCommand { get; }
        public ICommand CreateOrderCommand { get; }
        public ICommand SaveResultsCommand { get; }
        public ICommand GenerateReportCommand { get; }
        public ICommand GenerateBillCommand { get; }
        public ICommand BackupCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SaveCatalogTestCommand { get; }
        public ICommand AddCatalogTestCommand { get; }
        public ICommand RefreshAuditLogsCommand { get; }
        public ICommand MarkAsPaidCashCommand { get; }
        public ICommand MarkAsPaidUpiCommand { get; }

        public DashboardViewModel(
            IPatientRepository patientRepo,
            ITestOrderRepository orderRepo,
            IResultRepository resultRepo,
            IRepository<TestType> testTypeRepo,
            IRepository<Staff> staffRepo,
            IRepository<AuditLog> auditLogRepo,
            IOrderService orderService,
            IResultService resultService,
            IPdfReportService reportService,
            IBackupService backupService,
            IBillingService billingService)
        {
            _patientRepo = patientRepo;
            _orderRepo = orderRepo;
            _resultRepo = resultRepo;
            _testTypeRepo = testTypeRepo;
            _staffRepo = staffRepo;
            _auditLogRepo = auditLogRepo;
            _orderService = orderService;
            _resultService = resultService;
            _reportService = reportService;
            _backupService = backupService;
            _billingService = billingService;

            AddPatientCommand = new RelayCommand(ExecuteAddPatient);
            CreateOrderCommand = new RelayCommand(ExecuteCreateOrder);
            SaveResultsCommand = new RelayCommand(ExecuteSaveResults);
            GenerateReportCommand = new RelayCommand(ExecuteGenerateReport);
            GenerateBillCommand = new RelayCommand(ExecuteGenerateBill);
            BackupCommand = new RelayCommand(ExecuteBackup);
            RefreshCommand = new RelayCommand(o => LoadData());
            SaveCatalogTestCommand = new RelayCommand(ExecuteSaveCatalogTest);
            AddCatalogTestCommand = new RelayCommand(ExecuteAddCatalogTest);
            RefreshAuditLogsCommand = new RelayCommand(async o => await LoadAuditLogsAsync());
            MarkAsPaidCashCommand = new RelayCommand(async o => await ExecuteMarkAsPaidAsync("Cash"));
            MarkAsPaidUpiCommand = new RelayCommand(async o => await ExecuteMarkAsPaidAsync("UPI"));

            LoadData();
        }

        private async void LoadStaffName()
        {
            try
            {
                var staff = await _staffRepo.GetByIdAsync(StaffId);
                CurrentStaffName = staff?.FullName ?? "Unknown Staff";
                IsAdmin = staff?.Role == "Admin";
                LoadData();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load staff details.");
                CurrentStaffName = "Admin User";
                IsAdmin = true;
                LoadData();
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
                    if (o.Patient == null)
                    {
                        o.Patient = await _patientRepo.GetByIdAsync(o.PatientId);
                    }
                    Orders.Add(o);
                }

                // Load Catalog Test Types
                CatalogTestTypes.Clear();
                foreach (var t in testTypes.OrderBy(x => x.SortOrder).ThenBy(x => x.Name))
                {
                    CatalogTestTypes.Add(t);
                }

                // Load Invoices
                Invoices.Clear();
                var invoices = await _billingService.GetAllInvoicesAsync();
                foreach (var inv in invoices)
                {
                    Invoices.Add(inv);
                }

                // Load Dashboard Statistics
                await CalculateDashboardStatsAsync();

                // Load Audit Logs if Admin
                if (IsAdmin)
                {
                    await LoadAuditLogsAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load dashboard data.");
                MessageBox.Show("Error loading data from database. See logs.", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CalculateDashboardStatsAsync()
        {
            try
            {
                var patients = await _patientRepo.GetAllAsync();
                TotalPatients = patients.Count();

                var orders = await _orderRepo.GetAllAsync();
                PendingOrders = orders.Count(o => o.Status == "Pending");
                CompletedOrders = orders.Count(o => o.Status == "Complete");

                var results = await _resultRepo.GetAllAsync();
                AbnormalResultsFlagged = results.Count(r => r.IsAbnormal);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to calculate dashboard statistics.");
            }
        }

        private async Task LoadAuditLogsAsync()
        {
            try
            {
                var logs = await _auditLogRepo.GetAllAsync();
                AuditLogs.Clear();
                foreach (var log in logs.OrderByDescending(l => l.LogId))
                {
                    if (log.User == null && log.UserId.HasValue)
                    {
                        log.User = await _staffRepo.GetByIdAsync(log.UserId.Value);
                    }
                    AuditLogs.Add(log);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load audit logs.");
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
                    Gender = NewPatientGender ?? "Male",
                    ContactPhone = NewPatientPhone ?? "",
                    ContactEmail = NewPatientEmail ?? "",
                    CreatedAt = DateTime.UtcNow.ToString("O")
                };

                await _patientRepo.AddAsync(patient);
                Log.Information("Added patient: {PatientName}", NewPatientName);

                await _auditLogRepo.AddAsync(new AuditLog
                {
                    Action = "Created",
                    EntityType = "Patient",
                    EntityId = patient.PatientId,
                    UserId = StaffId,
                    Timestamp = DateTime.UtcNow,
                    Details = $"Registered patient '{NewPatientName}' (ID {patient.PatientId})."
                });

                // Reset fields
                NewPatientName = string.Empty;
                NewPatientDOB = null;
                NewPatientPhone = string.Empty;
                NewPatientEmail = string.Empty;
                NewPatientGender = "Male";

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
                string testIds = string.Join(",", selectedTests.Select(t => t.TypeId));
                
                var order = new TestOrder
                {
                    PatientId = SelectedPatient.PatientId,
                    Status = "Pending",
                    Notes = testIds,
                    ReferredBy = string.IsNullOrWhiteSpace(OrderReferredBy) ? "SELF" : OrderReferredBy
                };

                await _orderService.CreateOrderAsync(order);
                Log.Information("Created test order ID {OrderId} for Patient ID {PatientId}", order.OrderId, SelectedPatient.PatientId);

                // Generate Invoice automatically
                await _billingService.GenerateInvoiceAsync(order.OrderId);

                await _auditLogRepo.AddAsync(new AuditLog
                {
                    Action = "Created",
                    EntityType = "TestOrder",
                    EntityId = order.OrderId,
                    UserId = StaffId,
                    Timestamp = DateTime.UtcNow,
                    Details = $"Created order {order.OrderId} for patient ID {SelectedPatient.PatientId}. Referred by: {order.ReferredBy}."
                });

                // Unselect test check boxes
                foreach (var t in TestTypes)
                {
                    t.IsSelected = false;
                }

                OrderReferredBy = "SELF";

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
                                    var ri = new ResultInput
                                    {
                                        TypeId = testType.TypeId,
                                        TestName = testType.Name,
                                        Unit = testType.Unit,
                                        Low = testType.ReferenceRangeLow,
                                        High = testType.ReferenceRangeHigh,
                                        IsAbnormal = false,
                                        IsReadOnly = false
                                    };
                                    PopulateOptions(ri);
                                    SelectedOrderResults.Add(ri);
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

                        var ri = new ResultInput
                        {
                            TypeId = r.TypeId,
                            TestName = r.TestType?.Name ?? "Unknown Test",
                            Unit = r.TestType?.Unit ?? "",
                            Low = r.TestType?.ReferenceRangeLow,
                            High = r.TestType?.ReferenceRangeHigh,
                            ValueText = r.Value.ToString(),
                            IsAbnormal = r.IsAbnormal,
                            IsReadOnly = true
                        };
                        PopulateOptions(ri);
                        SelectedOrderResults.Add(ri);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load order results.");
            }
        }

        private void PopulateOptions(ResultInput ri)
        {
            ri.Options.Clear();
            if (ri.Unit == "Blood Group")
            {
                ri.Options.Add(new ResultOption { Display = "A Rh Positive", Value = "1" });
                ri.Options.Add(new ResultOption { Display = "A Rh Negative", Value = "2" });
                ri.Options.Add(new ResultOption { Display = "B Rh Positive", Value = "3" });
                ri.Options.Add(new ResultOption { Display = "B Rh Negative", Value = "4" });
                ri.Options.Add(new ResultOption { Display = "O Rh Positive", Value = "5" });
                ri.Options.Add(new ResultOption { Display = "O Rh Negative", Value = "6" });
                ri.Options.Add(new ResultOption { Display = "AB Rh Positive", Value = "7" });
                ri.Options.Add(new ResultOption { Display = "AB Rh Negative", Value = "8" });
            }
            else if (ri.TestName.Contains("Malarial Parasite") || ri.TestName.Contains("PBS Malarial"))
            {
                ri.Options.Add(new ResultOption { Display = "Not Detected", Value = "0" });
                ri.Options.Add(new ResultOption { Display = "Detected", Value = "1" });
            }
            else if (ri.TestName.Contains("Rapid Malaria"))
            {
                ri.Options.Add(new ResultOption { Display = "Negative", Value = "0" });
                ri.Options.Add(new ResultOption { Display = "Positive", Value = "1" });
            }
            else if (ri.Unit == "Qualitative" || ri.TestName.Contains("Urine Sugar") || ri.TestName.Contains("Urine Protein"))
            {
                ri.Options.Add(new ResultOption { Display = "Absent", Value = "0" });
                ri.Options.Add(new ResultOption { Display = "Present", Value = "1" });
            }

            if (ri.HasOptions && !string.IsNullOrEmpty(ri.ValueText))
            {
                ri.SelectedOption = ri.Options.FirstOrDefault(o => o.Value == ri.ValueText || Math.Abs((double.TryParse(o.Value, out var v1) ? v1 : -1) - (double.TryParse(ri.ValueText, out var v2) ? v2 : -2)) < 0.001);
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

                    await _resultService.AddResultAsync(result);
                }

                int selectedOrderId = SelectedOrder.OrderId;

                // Update order status to Complete
                await _orderService.UpdateOrderStatusAsync(selectedOrderId, "Complete");
                Log.Information("Verified and completed order {OrderId}", selectedOrderId);

                await _auditLogRepo.AddAsync(new AuditLog
                {
                    Action = "Updated",
                    EntityType = "TestOrder",
                    EntityId = selectedOrderId,
                    UserId = StaffId,
                    Timestamp = DateTime.UtcNow,
                    Details = $"Verified and saved results for order ID {selectedOrderId}."
                });

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
                        var previewWindow = new Views.PdfPreviewWindow(SelectedOrder, _reportService);
                        previewWindow.ShowDialog();
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

        private void ExecuteGenerateReport(object obj)
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
                var previewWindow = new Views.PdfPreviewWindow(SelectedOrder, _reportService);
                previewWindow.Owner = Application.Current.MainWindow;
                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to generate report.");
                MessageBox.Show($"Error generating PDF report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteGenerateBill(object obj)
        {
            if (SelectedOrder == null)
            {
                MessageBox.Show("Please select an order from the list.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var invoice = await _billingService.GetInvoiceForOrderAsync(SelectedOrder.OrderId);
                if (invoice == null)
                {
                    invoice = await _billingService.GenerateInvoiceAsync(SelectedOrder.OrderId);
                    invoice = await _billingService.GetInvoiceForOrderAsync(SelectedOrder.OrderId);
                }

                LoadData(); // Always refresh to ensure it shows up

                if (invoice != null)
                {
                    var previewWindow = new Views.InvoicePreviewWindow(invoice, _reportService);
                    previewWindow.Owner = Application.Current.MainWindow; // Fix CenterOwner issue
                    previewWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to generate bill.");
                MessageBox.Show($"Error generating bill: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteBackup(object obj)
        {
            try
            {
                await _backupService.BackupNowAsync();
                
                await _auditLogRepo.AddAsync(new AuditLog
                {
                    Action = "Backup",
                    EntityType = "System",
                    UserId = StaffId,
                    Timestamp = DateTime.UtcNow,
                    Details = "Created full SQLite database and ClosedXML Excel backup."
                });

                MessageBox.Show("Database (SQLite) and technician-friendly report (Excel) backed up successfully to the backups directory!", "Backup Completed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Database backup failed.");
                MessageBox.Show("Failed to complete database backup. Check logs for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateCatalogEditFields()
        {
            if (SelectedCatalogTest == null)
            {
                CatalogTestName = string.Empty;
                CatalogTestUnit = string.Empty;
                CatalogTestLow = null;
                CatalogTestHigh = null;
                CatalogTestIsActive = true;
                CatalogTestCategory = string.Empty;
                CatalogTestGroupName = string.Empty;
                CatalogTestMethod = string.Empty;
                CatalogTestInterpretation = string.Empty;
                CatalogTestSortOrder = 0;
                return;
            }

            CatalogTestName = SelectedCatalogTest.Name;
            CatalogTestUnit = SelectedCatalogTest.Unit;
            CatalogTestLow = SelectedCatalogTest.ReferenceRangeLow;
            CatalogTestHigh = SelectedCatalogTest.ReferenceRangeHigh;
            CatalogTestIsActive = SelectedCatalogTest.IsActive;
            CatalogTestCategory = SelectedCatalogTest.Category;
            CatalogTestGroupName = SelectedCatalogTest.GroupName;
            CatalogTestMethod = SelectedCatalogTest.Method;
            CatalogTestInterpretation = SelectedCatalogTest.Interpretation;
            CatalogTestSortOrder = SelectedCatalogTest.SortOrder;
        }

        private async void ExecuteSaveCatalogTest(object obj)
        {
            if (SelectedCatalogTest == null) return;
            if (string.IsNullOrWhiteSpace(CatalogTestName))
            {
                MessageBox.Show("Please enter a test name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Fetch the tracked entity to avoid multiple entity instance conflict in EF
                var entityToUpdate = await _testTypeRepo.GetByIdAsync(SelectedCatalogTest.TypeId);
                if (entityToUpdate == null) return;

                entityToUpdate.Name = CatalogTestName;
                entityToUpdate.Unit = CatalogTestUnit;
                entityToUpdate.ReferenceRangeLow = CatalogTestLow;
                entityToUpdate.ReferenceRangeHigh = CatalogTestHigh;
                entityToUpdate.IsActive = CatalogTestIsActive;
                entityToUpdate.Category = CatalogTestCategory;
                entityToUpdate.GroupName = CatalogTestGroupName;
                entityToUpdate.Method = CatalogTestMethod;
                entityToUpdate.Interpretation = CatalogTestInterpretation;
                entityToUpdate.SortOrder = CatalogTestSortOrder;

                await _testTypeRepo.UpdateAsync(entityToUpdate);

                // Update the UI model
                SelectedCatalogTest.Name = CatalogTestName;
                SelectedCatalogTest.Unit = CatalogTestUnit;
                SelectedCatalogTest.ReferenceRangeLow = CatalogTestLow;
                SelectedCatalogTest.ReferenceRangeHigh = CatalogTestHigh;
                SelectedCatalogTest.IsActive = CatalogTestIsActive;
                SelectedCatalogTest.Category = CatalogTestCategory;
                SelectedCatalogTest.GroupName = CatalogTestGroupName;
                SelectedCatalogTest.Method = CatalogTestMethod;
                SelectedCatalogTest.Interpretation = CatalogTestInterpretation;
                SelectedCatalogTest.SortOrder = CatalogTestSortOrder;

                Log.Information("Admin updated TestType {TypeId}: {TestName}", SelectedCatalogTest.TypeId, CatalogTestName);

                await _auditLogRepo.AddAsync(new AuditLog
                {
                    Action = "Updated",
                    EntityType = "TestType",
                    EntityId = SelectedCatalogTest.TypeId,
                    UserId = StaffId,
                    Timestamp = DateTime.UtcNow,
                    Details = $"Updated test type '{CatalogTestName}' (ID {SelectedCatalogTest.TypeId})."
                });

                MessageBox.Show("Test type saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save test type.");
                MessageBox.Show("Error saving test type.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteAddCatalogTest(object obj)
        {
            if (string.IsNullOrWhiteSpace(CatalogTestName))
            {
                MessageBox.Show("Please enter a name for the new test type.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var newTest = new TestType
                {
                    Name = CatalogTestName,
                    Unit = CatalogTestUnit,
                    ReferenceRangeLow = CatalogTestLow,
                    ReferenceRangeHigh = CatalogTestHigh,
                    IsActive = CatalogTestIsActive,
                    Category = CatalogTestCategory,
                    GroupName = CatalogTestGroupName,
                    Method = CatalogTestMethod,
                    Interpretation = CatalogTestInterpretation,
                    SortOrder = CatalogTestSortOrder
                };

                await _testTypeRepo.AddAsync(newTest);
                Log.Information("Admin added new TestType: {TestName}", CatalogTestName);

                await _auditLogRepo.AddAsync(new AuditLog
                {
                    Action = "Created",
                    EntityType = "TestType",
                    EntityId = newTest.TypeId,
                    UserId = StaffId,
                    Timestamp = DateTime.UtcNow,
                    Details = $"Created new test type '{CatalogTestName}' (ID {newTest.TypeId})."
                });

                MessageBox.Show("New test type added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
                SelectedCatalogTest = newTest;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add test type.");
                MessageBox.Show("Error adding test type.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteMarkAsPaidAsync(string paymentMethod)
        {
            if (SelectedInvoice == null)
            {
                MessageBox.Show("Please select an invoice to mark as paid.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedInvoice.IsPaid)
            {
                MessageBox.Show("This invoice is already paid.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                await _billingService.MarkAsPaidAsync(SelectedInvoice.InvoiceId, paymentMethod);
                Log.Information("Marked invoice {InvoiceId} as paid via {PaymentMethod}", SelectedInvoice.InvoiceId, paymentMethod);

                await _auditLogRepo.AddAsync(new AuditLog
                {
                    Action = "Updated",
                    EntityType = "Invoice",
                    EntityId = SelectedInvoice.InvoiceId,
                    UserId = StaffId,
                    Timestamp = DateTime.UtcNow,
                    Details = $"Marked invoice {SelectedInvoice.InvoiceId} as paid via {paymentMethod}."
                });

                MessageBox.Show($"Invoice marked as paid via {paymentMethod} successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData(); // Reload invoices
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to mark invoice as paid.");
                MessageBox.Show("Error updating invoice status.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

    public class ResultOption
    {
        public string Display { get; set; }
        public string Value { get; set; }
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
            set 
            { 
                _valueText = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(DisplayValue));
                if (HasOptions && (_selectedOption == null || _selectedOption.Value != value))
                {
                    SelectedOption = Options.FirstOrDefault(o => o.Value == value || Math.Abs((double.TryParse(o.Value, out var v1) ? v1 : -1) - (double.TryParse(value, out var v2) ? v2 : -2)) < 0.001);
                }
            }
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

        public ObservableCollection<ResultOption> Options { get; } = new ObservableCollection<ResultOption>();
        public bool HasOptions => Options.Count > 0;

        private ResultOption _selectedOption;
        public ResultOption SelectedOption
        {
            get => _selectedOption;
            set
            {
                _selectedOption = value;
                OnPropertyChanged();
                if (_selectedOption != null && ValueText != _selectedOption.Value)
                {
                    ValueText = _selectedOption.Value;
                }
            }
        }

        public string DisplayValue
        {
            get
            {
                if (HasOptions)
                {
                    var opt = Options.FirstOrDefault(o => o.Value == ValueText || Math.Abs((double.TryParse(o.Value, out var v1) ? v1 : -1) - (double.TryParse(ValueText, out var v2) ? v2 : -2)) < 0.001);
                    if (opt != null) return opt.Display;
                }
                return ValueText;
            }
        }
    }
}
