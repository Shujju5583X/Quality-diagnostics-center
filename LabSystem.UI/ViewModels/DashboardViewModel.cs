using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.Core.Enums;
using Serilog;
using LabSystem.Services;

namespace LabSystem.UI.ViewModels
{
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly IPatientRepository _patientRepo;
        private readonly ITestOrderRepository _orderRepo;
        private readonly IResultRepository _resultRepo;
        private readonly IRepository<TestType> _testTypeRepo;
        private readonly IOrderService _orderService;
        private readonly IResultService _resultService;
        private readonly IPdfReportService _reportService;
        private readonly IBackupService _backupService;
        private readonly IBillingService _billingService;
        private readonly IRepository<TestPanel> _testPanelRepo;

        // Fixed operator identity — single-person mode, no login required
        public const int DefaultStaffId = 1;

        private Patient _selectedPatient;
        private TestOrder _selectedOrder;
        private Invoice _selectedInvoice;

        // Patient tab fields
        private string _newPatientName;
        private DateTime? _newPatientDOB;
        private string _newPatientPhone;
        private string _newPatientEmail;
        private string _newPatientGender = "Male";

        // Patient search, filter, pagination fields
        private string _patientSearchQuery;
        private DateTime? _patientStartDate;
        private DateTime? _patientEndDate;
        private int _patientCurrentPage = 1;
        private int _patientTotalPages = 1;
        private int _patientTotalCount = 0;
        public const int PageSize = 15;

        // Order tab fields
        private string _orderNotes;
        private string _orderReferredBy = "SELF";

        // Results tab fields
        private string _resultErrorMessage;
        public string ResultErrorMessage
        {
            get => _resultErrorMessage;
            set { _resultErrorMessage = value; OnPropertyChanged(); }
        }

        // Dashboard stats fields
        private int _totalPatients;
        private int _pendingOrders;
        private int _completedOrders;
        private int _abnormalResultsFlagged;

        // Billing fields - Discount/Tax percent
        private decimal _discountPercent;
        private decimal _taxPercent;
        private decimal _paymentAmount;

        // Revenue report fields
        private RevenueReportStats _revenueStats;
        private DateTime _reportStartDate = DateTime.Today.AddDays(-30);
        private DateTime _reportEndDate = DateTime.Today;

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

        public ObservableCollection<Patient> Patients { get; } = new ObservableCollection<Patient>();
        public ObservableCollection<TestOrder> Orders { get; } = new ObservableCollection<TestOrder>();
        public ObservableCollection<TestTypeSelection> TestTypes { get; } = new ObservableCollection<TestTypeSelection>();

        // Items for entering results for the selected order
        public ObservableCollection<ResultInput> SelectedOrderResults { get; } = new ObservableCollection<ResultInput>();

        // Catalog Management items
        public ObservableCollection<TestType> CatalogTestTypes { get; } = new ObservableCollection<TestType>();

        // Billing Items
        public ObservableCollection<Invoice> Invoices { get; } = new ObservableCollection<Invoice>();

        // Patient History
        public ObservableCollection<PatientHistoryEntry> PatientHistory { get; } = new ObservableCollection<PatientHistoryEntry>();
        private string _patientHistoryName;
        public string PatientHistoryName
        {
            get => _patientHistoryName;
            set { _patientHistoryName = value; OnPropertyChanged(); }
        }
        private int _patientHistoryCount;
        public int PatientHistoryCount
        {
            get => _patientHistoryCount;
            set { _patientHistoryCount = value; OnPropertyChanged(); }
        }
        public ICommand LoadPatientHistoryCommand { get; private set; }

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
                _ = LoadResultsForSelectedOrderSafeAsync();
            }
        }

        public Invoice SelectedInvoice
        {
            get => _selectedInvoice;
            set
            {
                _selectedInvoice = value;
                OnPropertyChanged();
                // Update PaymentAmount to grand total when invoice changes
                if (_selectedInvoice != null)
                {
                    PaymentAmount = _selectedInvoice.GrandTotal;
                    DiscountPercent = _selectedInvoice.DiscountPercent;
                    TaxPercent = _selectedInvoice.TaxPercent;
                }
            }
        }

        public UnifiedQueueViewModel UnifiedQueueVM { get; }
        public QcViewModel QcVM { get; }
        public AppointmentsViewModel AppointmentsVM { get; }
        public StaffManagementViewModel StaffManagementVM { get; }

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

        // Billing properties - Discount/Tax percent
        public decimal DiscountPercent
        {
            get => _discountPercent;
            set { _discountPercent = value; OnPropertyChanged(); }
        }

        public decimal TaxPercent
        {
            get => _taxPercent;
            set { _taxPercent = value; OnPropertyChanged(); }
        }

        public decimal PaymentAmount
        {
            get => _paymentAmount;
            set { _paymentAmount = value; OnPropertyChanged(); }
        }

        // Revenue report properties
        public RevenueReportStats RevenueStats
        {
            get => _revenueStats;
            set { _revenueStats = value; OnPropertyChanged(); }
        }

        public DateTime ReportStartDate
        {
            get => _reportStartDate;
            set { _reportStartDate = value; OnPropertyChanged(); }
        }

        public DateTime ReportEndDate
        {
            get => _reportEndDate;
            set { _reportEndDate = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand AddPatientCommand { get; }
        public ICommand CreateOrderCommand { get; }
        public ICommand BackupCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SaveCatalogTestCommand { get; }
        public ICommand AddCatalogTestCommand { get; }
        public ICommand PreviousPatientPageCommand { get; }
        public ICommand NextPatientPageCommand { get; }
        public ICommand AddPaymentCashCommand { get; }
        public ICommand AddPaymentUpiCommand { get; }
        public ICommand ApplyDiscountTaxCommand { get; }
        public ICommand GenerateRevenueReportCommand { get; }

        public DashboardViewModel(
            IPatientRepository patientRepo,
            ITestOrderRepository orderRepo,
            IOrderService orderService,
            IPdfReportService reportService,
            IResultRepository resultRepo,
            IRepository<TestType> testTypeRepo,
            IResultService resultService,
            IBillingService billingService,
            IRepository<TestPanel> testPanelRepo,
            IBackupService backupService,
            UnifiedQueueViewModel unifiedQueueVM,
            QcViewModel qcVM,
            AppointmentsViewModel appointmentsVM,
            StaffManagementViewModel staffManagementVM)
        {
            _patientRepo = patientRepo;
            _orderRepo = orderRepo;
            _orderService = orderService;
            _reportService = reportService;

            _resultRepo = resultRepo;
            _testTypeRepo = testTypeRepo;
            _resultService = resultService;

            _billingService = billingService;
            _testPanelRepo = testPanelRepo;

            _backupService = backupService;
            UnifiedQueueVM = unifiedQueueVM;
            QcVM = qcVM;
            AppointmentsVM = appointmentsVM;
            StaffManagementVM = staffManagementVM;

            AddPatientCommand = new AsyncRelayCommand(async o => await ExecuteAddPatientAsync(o));
            CreateOrderCommand = new AsyncRelayCommand(async o => await ExecuteCreateOrderAsync(o));
            BackupCommand = new AsyncRelayCommand(async o => await _backupService.BackupNowAsync());
            RefreshCommand = new AsyncRelayCommand(async o => await LoadDataAsync());
            SaveCatalogTestCommand = new AsyncRelayCommand(async o => await ExecuteSaveCatalogTestAsync(o));
            AddCatalogTestCommand = new AsyncRelayCommand(async o => await ExecuteAddCatalogTestAsync(o));
            AddPaymentCashCommand = new AsyncRelayCommand(async o => await ExecuteAddPaymentAsync("Cash"));
            AddPaymentUpiCommand = new AsyncRelayCommand(async o => await ExecuteAddPaymentAsync("UPI"));
            ApplyDiscountTaxCommand = new AsyncRelayCommand(async o => await ExecuteApplyDiscountTaxAsync());
            GenerateRevenueReportCommand = new AsyncRelayCommand(async o => await ExecuteGenerateRevenueReportAsync());

            PreviousPatientPageCommand = new AsyncRelayCommand(async o =>
            {
                if (PatientCurrentPage > 1)
                {
                    PatientCurrentPage--;
                    await LoadPatientsAsync();
                }
            });

            NextPatientPageCommand = new AsyncRelayCommand(async o =>
            {
                if (PatientCurrentPage < PatientTotalPages)
                {
                    PatientCurrentPage++;
                    await LoadPatientsAsync();
                }
            });

            InitializeAmendResultCommand();
            LoadPatientHistoryCommand = new AsyncRelayCommand(async o => await ExecuteLoadPatientHistoryAsync());

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize dashboard.");
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "startup_crash.log"),
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} DASHBOARD INIT ERROR: {ex}\r\n");
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Load Patients (paginated & filtered)
                await LoadPatientsAsync();

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

                // Load Test Panels
                TestPanels.Clear();
                var panels = await _testPanelRepo.GetAllAsync();
                foreach (var p in panels)
                {
                    TestPanels.Add(p);
                }

                // Load Orders
                Orders.Clear();
                var orders = await _orderRepo.GetAllAsync();
                foreach (var o in orders)
                {
                    Orders.Add(o);
                }

                // Load Catalog Test Types
                CatalogTestTypes.Clear();
                foreach (var t in testTypes.OrderBy(x => x.SortOrder).ThenBy(x => x.Name))
                {
                    CatalogTestTypes.Add(t);
                }

                await UnifiedQueueVM.LoadQueueAsync();

                // Load Invoices
                await LoadInvoicesAsync();

                // Load ReferredBy autocomplete history
                await LoadReferredByHistoryAsync();

                // Calculate Dashboard Statistics
                await RefreshDashboardStatsAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load dashboard data.");
                MessageBox.Show("Error loading data from database. See logs.", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadInvoicesAsync()
        {
            try
            {
                Invoices.Clear();
                var invoices = await _billingService.GetAllInvoicesAsync();
                foreach (var inv in invoices.OrderByDescending(i => i.InvoiceId))
                {
                    Invoices.Add(inv);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load invoices.");
            }
        }

        private async Task ExecuteAddPaymentAsync(string paymentMethod)
        {
            try
            {
                if (SelectedInvoice == null || SelectedInvoice.IsPaid)
                {
                    MessageBox.Show("Please select an unpaid invoice.", "No Invoice Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (PaymentAmount <= 0)
                {
                    MessageBox.Show("Please enter a valid payment amount.", "Invalid Amount", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (PaymentAmount > SelectedInvoice.GrandTotal)
                {
                    MessageBox.Show($"Amount exceeds grand total of ₹{SelectedInvoice.GrandTotal:N2}.", "Invalid Amount", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await _billingService.AddPaymentAsync(SelectedInvoice.InvoiceId, PaymentAmount, paymentMethod);
                MessageBox.Show($"Payment of ₹{PaymentAmount:N2} recorded via {paymentMethod}.", "Payment Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                PaymentAmount = 0;
                await LoadInvoicesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to process payment.");
                MessageBox.Show($"Payment failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteApplyDiscountTaxAsync()
        {
            try
            {
                if (SelectedInvoice == null || SelectedInvoice.IsPaid)
                {
                    MessageBox.Show("Please select an unpaid invoice.", "No Invoice Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await _billingService.UpdateInvoiceFinancialsAsync(SelectedInvoice.InvoiceId, DiscountPercent, TaxPercent);
                MessageBox.Show("Discount/tax applied.", "Updated", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadInvoicesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to apply discount/tax.");
                MessageBox.Show($"Failed to apply discount/tax: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteGenerateRevenueReportAsync()
        {
            try
            {
                RevenueStats = await _billingService.GetRevenueReportAsync(ReportStartDate, ReportEndDate);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to generate revenue report.");
                MessageBox.Show($"Failed to generate revenue report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                            TestName = result.TestType?.Name ?? "Unknown",
                            Value = result.Value,
                            ValueText = result.ValueText,
                            Unit = result.TestType?.Unit ?? "",
                            IsAbnormal = result.IsAbnormal,
                            ReferenceLow = result.TestType?.ReferenceRangeLow,
                            ReferenceHigh = result.TestType?.ReferenceRangeHigh
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
                MessageBox.Show($"Failed to load patient history: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RefreshDashboardStatsAsync()
        {
            try
            {
                TotalPatients = PatientTotalCount;
                PendingOrders = Orders.Count(o => o.StatusEnum == OrderStatus.Pending);
                CompletedOrders = Orders.Count(o => o.StatusEnum == OrderStatus.Complete);

                AbnormalResultsFlagged = await _resultRepo.CountAbnormalAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to refresh dashboard statistics.");
            }
        }

        private async Task LoadResultsForSelectedOrderSafeAsync()
        {
            try
            {
                await LoadResultsForSelectedOrderAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load results for selected order.");
            }
        }
    }
}
