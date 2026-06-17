using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.Core.Enums;
using Serilog;

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
        private readonly IRepository<Doctor> _doctorRepo;
        private readonly IRepository<Department> _departmentRepo;
        private readonly IRepository<Setting> _settingRepo;
        private readonly IUnitOfWork _unitOfWork;

        // Fixed operator identity — single-person mode, no login required
        public const int DefaultStaffId = 1;

        private CancellationTokenSource _cts = new CancellationTokenSource();

        private Patient _selectedPatient;
        private TestOrder _selectedOrder;
        private Invoice _selectedInvoice;

        // Patient tab fields
        private string _newPatientName;
        private int _newPatientAge;
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
            get { return _resultErrorMessage; }
            set { _resultErrorMessage = value; OnPropertyChanged(); }
        }

        // Dashboard stats fields
        private int _totalPatients;
        private int _pendingOrders;
        private int _completedOrders;
        private int _abnormalResultsFlagged;
        private int _todayPatients;
        private int _todayOrders;
        private decimal _todayRevenue;

        // Billing fields - Discount/Tax amount (ruling out percent)
        private decimal _discountAmount;
        private decimal _taxAmount;
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

        // Sidebar pin fields
        private bool _isSidebarPinned;
        public bool IsSidebarPinned
        {
            get { return _isSidebarPinned; }
            set
            {
                _isSidebarPinned = value;
                OnPropertyChanged();
                try
                {
                    var path = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "sidebar_pinned.txt");
                    System.IO.File.WriteAllText(path, value.ToString());
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to save sidebar pin state.");
                }
            }
        }

        private int _mainTabIndex;
        public int MainTabIndex
        {
            get { return _mainTabIndex; }
            set { _mainTabIndex = value; OnPropertyChanged(); }
        }

        private int _workQueueTabIndex;
        public int WorkQueueTabIndex
        {
            get { return _workQueueTabIndex; }
            set { _workQueueTabIndex = value; OnPropertyChanged(); }
        }

        private bool _isResultEditMode;
        public bool IsResultEditMode
        {
            get { return _isResultEditMode; }
            set
            {
                _isResultEditMode = value;
                OnPropertyChanged();
            }
        }

        public IEnumerable<TestOrder> PendingOrdersFiltered
        {
            get { return Orders.Where(o => o.StatusEnum == OrderStatus.Pending); }
        }

        // Settings Operator info fields
        private string _operatorName;
        public string OperatorName
        {
            get { return _operatorName; }
            set { _operatorName = value; OnPropertyChanged(); }
        }

        private string _operatorAddress;
        public string OperatorAddress
        {
            get { return _operatorAddress; }
            set { _operatorAddress = value; OnPropertyChanged(); }
        }

        private string _operatorPhone;
        public string OperatorPhone
        {
            get { return _operatorPhone; }
            set { _operatorPhone = value; OnPropertyChanged(); }
        }

        private string _lastBackupTime;
        public string LastBackupTime
        {
            get { return _lastBackupTime; }
            set { _lastBackupTime = value; OnPropertyChanged(); }
        }

        // Doctors CRUD fields
        private string _newDoctorName;
        public string NewDoctorName
        {
            get { return _newDoctorName; }
            set { _newDoctorName = value; OnPropertyChanged(); }
        }

        private string _newDoctorPhone;
        public string NewDoctorPhone
        {
            get { return _newDoctorPhone; }
            set { _newDoctorPhone = value; OnPropertyChanged(); }
        }

        private decimal _newDoctorCommission;
        public decimal NewDoctorCommission
        {
            get { return _newDoctorCommission; }
            set { _newDoctorCommission = value; OnPropertyChanged(); }
        }

        private Doctor _selectedDoctor;
        public Doctor SelectedDoctor
        {
            get { return _selectedDoctor; }
            set
            {
                _selectedDoctor = value;
                OnPropertyChanged();
                if (value != null)
                {
                    NewDoctorName = value.FullName;
                    NewDoctorPhone = value.ContactPhone;
                    NewDoctorCommission = value.Commission;
                }
                else
                {
                    NewDoctorName = string.Empty;
                    NewDoctorPhone = string.Empty;
                    NewDoctorCommission = 0;
                }
            }
        }

        // Departments CRUD fields
        private string _newDepartmentName;
        public string NewDepartmentName
        {
            get { return _newDepartmentName; }
            set { _newDepartmentName = value; OnPropertyChanged(); }
        }

        private Department _selectedDepartment;
        public Department SelectedDepartment
        {
            get { return _selectedDepartment; }
            set
            {
                _selectedDepartment = value;
                OnPropertyChanged();
                var unused = LoadCatalogTestsForDepartmentAsync();
            }
        }

        public ObservableCollection<Patient> Patients { get; private set; }
        public ObservableCollection<TestOrder> Orders { get; private set; }
        public ObservableCollection<TestTypeSelection> TestTypes { get; private set; }

        // Items for entering results for the selected order
        public ObservableCollection<ResultInput> SelectedOrderResults { get; private set; }

        // Catalog Management items
        public ObservableCollection<TestType> CatalogTestTypes { get; private set; }

        // Billing Items
        public ObservableCollection<Invoice> Invoices { get; private set; }

        // New collections
        public ObservableCollection<Doctor> Doctors { get; private set; }
        public ObservableCollection<Department> Departments { get; private set; }

        // Patient History
        public ObservableCollection<PatientHistoryEntry> PatientHistory { get; private set; }
        private string _patientHistoryName;
        public string PatientHistoryName
        {
            get { return _patientHistoryName; }
            set { _patientHistoryName = value; OnPropertyChanged(); }
        }
        private int _patientHistoryCount;
        public int PatientHistoryCount
        {
            get { return _patientHistoryCount; }
            set { _patientHistoryCount = value; OnPropertyChanged(); }
        }
        public ICommand LoadPatientHistoryCommand { get; private set; }

        public Patient SelectedPatient
        {
            get { return _selectedPatient; }
            set { _selectedPatient = value; OnPropertyChanged(); }
        }

        public TestOrder SelectedOrder
        {
            get { return _selectedOrder; }
            set
            {
                _selectedOrder = value;
                OnPropertyChanged();
                var unused = LoadResultsForSelectedOrderSafeAsync();
            }
        }

        public Invoice SelectedInvoice
        {
            get { return _selectedInvoice; }
            set
            {
                _selectedInvoice = value;
                OnPropertyChanged();
                // Update PaymentAmount to grand total when invoice changes
                if (_selectedInvoice != null)
                {
                    PaymentAmount = _selectedInvoice.GrandTotal;
                    DiscountAmount = _selectedInvoice.DiscountAmount;
                    TaxAmount = _selectedInvoice.TaxAmount;
                    SelectedOrder = _selectedInvoice.Order ?? Orders.FirstOrDefault(o => o.OrderId == _selectedInvoice.OrderId);
                }
            }
        }


        public int TotalPatients
        {
            get { return _totalPatients; }
            set { _totalPatients = value; OnPropertyChanged(); }
        }

        public int PendingOrders
        {
            get { return _pendingOrders; }
            set { _pendingOrders = value; OnPropertyChanged(); }
        }

        public int CompletedOrders
        {
            get { return _completedOrders; }
            set { _completedOrders = value; OnPropertyChanged(); }
        }

        public int AbnormalResultsFlagged
        {
            get { return _abnormalResultsFlagged; }
            set { _abnormalResultsFlagged = value; OnPropertyChanged(); }
        }

        public int TodayPatients
        {
            get { return _todayPatients; }
            set { _todayPatients = value; OnPropertyChanged(); }
        }

        public int TodayOrders
        {
            get { return _todayOrders; }
            set { _todayOrders = value; OnPropertyChanged(); }
        }

        public decimal TodayRevenue
        {
            get { return _todayRevenue; }
            set { _todayRevenue = value; OnPropertyChanged(); }
        }

        // Billing properties - Discount/Tax amount (flat rupee amount)
        public decimal DiscountAmount
        {
            get { return _discountAmount; }
            set { _discountAmount = value; OnPropertyChanged(); }
        }

        public decimal TaxAmount
        {
            get { return _taxAmount; }
            set { _taxAmount = value; OnPropertyChanged(); }
        }

        public decimal PaymentAmount
        {
            get { return _paymentAmount; }
            set { _paymentAmount = value; OnPropertyChanged(); }
        }

        // Revenue report properties
        public RevenueReportStats RevenueStats
        {
            get { return _revenueStats; }
            set { _revenueStats = value; OnPropertyChanged(); }
        }

        public DateTime ReportStartDate
        {
            get { return _reportStartDate; }
            set { _reportStartDate = value; OnPropertyChanged(); }
        }

        public DateTime ReportEndDate
        {
            get { return _reportEndDate; }
            set { _reportEndDate = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand AddPatientCommand { get; private set; }
        public ICommand CreateOrderCommand { get; private set; }
        public ICommand BackupCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand SaveCatalogTestCommand { get; private set; }
        public ICommand AddCatalogTestCommand { get; private set; }
        public ICommand PreviousPatientPageCommand { get; private set; }
        public ICommand NextPatientPageCommand { get; private set; }
        public ICommand AddPaymentCashCommand { get; private set; }
        public ICommand AddPaymentUpiCommand { get; private set; }
        public ICommand ApplyDiscountTaxCommand { get; private set; }
        public ICommand GenerateRevenueReportCommand { get; private set; }
        public ICommand RestoreBackupCommand { get; private set; }
        public ICommand SaveResultsCommand { get; private set; }
        public ICommand GenerateReportCommand { get; private set; }
        public ICommand GenerateBillCommand { get; private set; }

        // New commands
        public ICommand SaveSettingsCommand { get; private set; }
        public ICommand SaveDoctorCommand { get; private set; }
        public ICommand DeleteDoctorCommand { get; private set; }
        public ICommand AddDepartmentCommand { get; private set; }
        public ICommand DeleteDepartmentCommand { get; private set; }
        public ICommand RenameDepartmentCommand { get; private set; }
        public ICommand DeleteCatalogTestCommand { get; private set; }
        public ICommand NavigateToReportCommand { get; private set; }
        public ICommand EditResultsCommand { get; private set; }
        public ICommand SaveAmendmentCommand { get; private set; }
        public ICommand CancelEditCommand { get; private set; }

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
            IRepository<Doctor> doctorRepo,
            IRepository<Department> departmentRepo,
            IRepository<Setting> settingRepo,
            IUnitOfWork unitOfWork)
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

            _doctorRepo = doctorRepo;
            _departmentRepo = departmentRepo;
            _settingRepo = settingRepo;
            _unitOfWork = unitOfWork;

            Patients = new ObservableCollection<Patient>();
            Orders = new ObservableCollection<TestOrder>();
            TestTypes = new ObservableCollection<TestTypeSelection>();
            SelectedOrderResults = new ObservableCollection<ResultInput>();
            CatalogTestTypes = new ObservableCollection<TestType>();
            Invoices = new ObservableCollection<Invoice>();
            Doctors = new ObservableCollection<Doctor>();
            Departments = new ObservableCollection<Department>();
            PatientHistory = new ObservableCollection<PatientHistoryEntry>();
            TestPanels = new ObservableCollection<TestPanel>();
            ReferredByHistory = new ObservableCollection<string>();

            // Load sidebar pin state
            try
            {
                var path = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "sidebar_pinned.txt");
                if (System.IO.File.Exists(path))
                {
                    bool.TryParse(System.IO.File.ReadAllText(path), out _isSidebarPinned);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load sidebar pin state.");
            }

            SaveResultsCommand = new AsyncRelayCommand(async o => await ExecuteSaveResultsAsync(o));
            GenerateReportCommand = new RelayCommand(ExecuteGenerateReport);
            GenerateBillCommand = new AsyncRelayCommand(async o => await ExecuteGenerateBillAsync(o));

            AddPatientCommand = new AsyncRelayCommand(async o => await ExecuteAddPatientAsync(o));
            CreateOrderCommand = new AsyncRelayCommand(async o => await ExecuteCreateOrderAsync(o));
            BackupCommand = new AsyncRelayCommand(async o => await ExecuteBackupCSVAsync());
            RefreshCommand = new AsyncRelayCommand(async o => await LoadDataAsync());
            SaveCatalogTestCommand = new AsyncRelayCommand(async o => await ExecuteSaveCatalogTestAsync(o));
            AddCatalogTestCommand = new AsyncRelayCommand(async o => await ExecuteAddCatalogTestAsync(o));
            AddPaymentCashCommand = new AsyncRelayCommand(async o => await ExecuteAddPaymentAsync("Cash"));
            AddPaymentUpiCommand = new AsyncRelayCommand(async o => await ExecuteAddPaymentAsync("UPI"));
            ApplyDiscountTaxCommand = new AsyncRelayCommand(async o => await ExecuteApplyDiscountTaxAsync());
            GenerateRevenueReportCommand = new AsyncRelayCommand(async o => await ExecuteGenerateRevenueReportAsync());
            RestoreBackupCommand = new AsyncRelayCommand(async o => await ExecuteRestoreBackupAsync());

            SaveSettingsCommand = new AsyncRelayCommand(async o => await ExecuteSaveSettingsAsync());
            SaveDoctorCommand = new AsyncRelayCommand(async o => await ExecuteSaveDoctorAsync());
            DeleteDoctorCommand = new AsyncRelayCommand(async o => await ExecuteDeleteDoctorAsync());
            AddDepartmentCommand = new AsyncRelayCommand(async o => await ExecuteAddDepartmentAsync());
            DeleteDepartmentCommand = new AsyncRelayCommand(async o => await ExecuteDeleteDepartmentAsync());
            RenameDepartmentCommand = new AsyncRelayCommand(async o => await ExecuteRenameDepartmentAsync());
            DeleteCatalogTestCommand = new AsyncRelayCommand(async o => await ExecuteDeleteCatalogTestAsync());
            
            NavigateToReportCommand = new RelayCommand(ExecuteNavigateToReport);

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

            EditResultsCommand = new AsyncRelayCommand(async o => await ExecuteEditResultsAsync(o));
            SaveAmendmentCommand = new AsyncRelayCommand(async o => await ExecuteSaveAmendmentAsync(o));
            CancelEditCommand = new AsyncRelayCommand(async o => await CancelEditModeAsync());
            InitializeAmendResultCommand();
            LoadPatientHistoryCommand = new AsyncRelayCommand(async o => await ExecuteLoadPatientHistoryAsync());
            InitializeRework();

            var unused2 = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                await LoadDataAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize dashboard.");
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "startup_crash.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " DASHBOARD INIT ERROR: " + ex + "\r\n");
            }
        }

        private async Task LoadDataAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // Load Settings
                var settingsList = await _settingRepo.GetAllAsync();
                var opNameSetting = settingsList.FirstOrDefault(s => s.Key == "operator_name");
                OperatorName = opNameSetting != null ? opNameSetting.Value ?? "" : "";
                var opAddrSetting = settingsList.FirstOrDefault(s => s.Key == "operator_address");
                OperatorAddress = opAddrSetting != null ? opAddrSetting.Value ?? "" : "";
                var opPhoneSetting = settingsList.FirstOrDefault(s => s.Key == "operator_phone");
                OperatorPhone = opPhoneSetting != null ? opPhoneSetting.Value ?? "" : "";
                var lastBackupSetting = settingsList.FirstOrDefault(s => s.Key == "last_backup");
                var lastBackup = lastBackupSetting != null ? lastBackupSetting.Value : null;
                LastBackupTime = string.IsNullOrEmpty(lastBackup) ? "No backup has been created yet." : lastBackup;

                // Load Doctors
                Doctors.Clear();
                var doctorsList = await _doctorRepo.GetAllAsync();
                foreach (var doc in doctorsList.OrderBy(d => d.FullName))
                {
                    Doctors.Add(doc);
                }

                // Load Departments
                Departments.Clear();
                var departmentsList = await _departmentRepo.GetAllAsync();
                foreach (var dept in departmentsList.OrderBy(d => d.Name))
                {
                    Departments.Add(dept);
                }

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
                            Category = t.Category,
                            Price = t.Price,
                            RefRangeMale = GetRangeString(t, "Male"),
                            RefRangeFemale = GetRangeString(t, "Female"),
                            OnSelectionChanged = OnTestSelectionChanged
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
                OnPropertyChanged("PendingOrdersFiltered");

                // Load Catalog Test Types
                CatalogTestTypes.Clear();
                foreach (var t in testTypes.OrderBy(x => x.SortOrder).ThenBy(x => x.Name))
                {
                    CatalogTestTypes.Add(t);
                }

                // Load Invoices
                await LoadInvoicesAsync();

                // Load ReferredBy autocomplete history
                await LoadReferredByHistoryAsync();

                // Calculate Dashboard Statistics
                await RefreshDashboardStatsAsync();

                LoadDataRework(doctorsList, departmentsList, panels);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load dashboard data.");
                MessageBox.Show("Error loading data from database. See logs.", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                // Today's aggregates
                DateTime todayStart = DateTime.Today;
                TodayPatients = Patients.Count(p => p.CreatedAt >= todayStart);
                TodayOrders = Orders.Count(o => o.OrderedAt >= todayStart);

                var todayInvoices = Invoices.Where(i => i.CreatedAt >= todayStart);
                TodayRevenue = todayInvoices.Sum(i => (decimal)i.AmountPaid);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to refresh dashboard statistics.");
            }
        }

        private void ExecuteNavigateToReport(object obj)
        {
            var order = obj as TestOrder;
            if (order != null)
            {
                // Tab Index 4 is the REPORT GENERATION tab
                MainTabIndex = 4;
                ReportSearchQuery = order.OrderId.ToString();
                SelectedReportOrder = CompleteOrders.FirstOrDefault(o => o.OrderId == order.OrderId) ?? order;
            }
        }
    }
}
