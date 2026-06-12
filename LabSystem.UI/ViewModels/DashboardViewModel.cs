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

        public ObservableCollection<Patient> Patients { get; } = new ObservableCollection<Patient>();
        public ObservableCollection<TestOrder> Orders { get; } = new ObservableCollection<TestOrder>();
        public ObservableCollection<TestTypeSelection> TestTypes { get; } = new ObservableCollection<TestTypeSelection>();

        // Items for entering results for the selected order
        public ObservableCollection<ResultInput> SelectedOrderResults { get; } = new ObservableCollection<ResultInput>();

        // Catalog Management items
        public ObservableCollection<TestType> CatalogTestTypes { get; } = new ObservableCollection<TestType>();

        // Billing Items
        public ObservableCollection<Invoice> Invoices { get; } = new ObservableCollection<Invoice>();

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
                OnPropertyChanged(nameof(IsSelectedOrderSpecimenRejected));
                LoadResultsForSelectedOrder();
            }
        }

        private Invoice _selectedInvoice;
        public Invoice SelectedInvoice
        {
            get => _selectedInvoice;
            set { _selectedInvoice = value; OnPropertyChanged(); }
        }

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
        public ICommand AddPaymentCashCommand { get; }
        public ICommand AddPaymentUpiCommand { get; }
        public ICommand PreviousPatientPageCommand { get; }
        public ICommand NextPatientPageCommand { get; }

        public DashboardViewModel(
            IPatientRepository patientRepo,
            ITestOrderRepository orderRepo,
            IResultRepository resultRepo,
            IRepository<TestType> testTypeRepo,
            IOrderService orderService,
            IResultService resultService,
            IPdfReportService reportService,
            IBackupService backupService,
            IBillingService billingService,
            IRepository<TestPanel> testPanelRepo)
        {
            _patientRepo = patientRepo;
            _orderRepo = orderRepo;
            _resultRepo = resultRepo;
            _testTypeRepo = testTypeRepo;
            _orderService = orderService;
            _resultService = resultService;
            _reportService = reportService;
            _backupService = backupService;
            _billingService = billingService;
            _testPanelRepo = testPanelRepo;

            AddPatientCommand = new RelayCommand(async o => await ExecuteAddPatientAsync(o));
            CreateOrderCommand = new RelayCommand(async o => await ExecuteCreateOrderAsync(o));
            SaveResultsCommand = new RelayCommand(async o => await ExecuteSaveResultsAsync(o));
            GenerateReportCommand = new RelayCommand(ExecuteGenerateReport);
            GenerateBillCommand = new RelayCommand(async o => await ExecuteGenerateBillAsync(o));
            BackupCommand = new RelayCommand(async o => await ExecuteBackupAsync(o));
            RefreshCommand = new RelayCommand(async o => await LoadDataAsync());
            SaveCatalogTestCommand = new RelayCommand(async o => await ExecuteSaveCatalogTestAsync(o));
            AddCatalogTestCommand = new RelayCommand(async o => await ExecuteAddCatalogTestAsync(o));
            AddPaymentCashCommand = new RelayCommand(async o => await ExecuteAddPaymentAsync("Cash"));
            AddPaymentUpiCommand = new RelayCommand(async o => await ExecuteAddPaymentAsync("UPI"));

            PreviousPatientPageCommand = new RelayCommand(async o =>
            {
                if (PatientCurrentPage > 1)
                {
                    PatientCurrentPage--;
                    await LoadPatientsAsync();
                }
            });

            NextPatientPageCommand = new RelayCommand(async o =>
            {
                if (PatientCurrentPage < PatientTotalPages)
                {
                    PatientCurrentPage++;
                    await LoadPatientsAsync();
                }
            });

            _ = LoadDataAsync();
            _ = RefreshAbnormalCountAsync();
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

                // Load Invoices
                Invoices.Clear();
                var invoices = await _billingService.GetAllInvoicesAsync();
                foreach (var inv in invoices)
                {
                    Invoices.Add(inv);
                }

                // Load ReferredBy autocomplete history
                await LoadReferredByHistoryAsync();

                // Calculate Dashboard Statistics
                CalculateDashboardStatsFromLoadedData();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load dashboard data.");
                MessageBox.Show("Error loading data from database. See logs.", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateDashboardStatsFromLoadedData()
        {
            try
            {
                TotalPatients = PatientTotalCount;
                PendingOrders = Orders.Count(o => o.Status == "Pending");
                CompletedOrders = Orders.Count(o => o.Status == "Complete");
                AbnormalResultsFlagged = _cachedAbnormalCount;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to calculate dashboard statistics.");
            }
        }

        private int _cachedAbnormalCount;

        private async Task RefreshAbnormalCountAsync()
        {
            try
            {
                var results = await _resultRepo.GetAllAsync();
                _cachedAbnormalCount = results.Count(r => r.IsAbnormal);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load abnormal count.");
            }
        }
    }
}
