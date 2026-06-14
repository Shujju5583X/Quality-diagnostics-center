using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.Core;
using LabSystem.Data;
using LabSystem.Data.Repositories;
using LabSystem.Services;
using SimpleInjector;
using Serilog;
using System.Reflection;
using System.Threading;


namespace LabSystem.UI
{
    public partial class App : Application
    {
        public static Container Container { get; private set; }
        public static int AuthenticatedStaffId { get; set; } = 1;

        // Hold a reference so we can trigger backup on exit
        private IBackupService _backupServiceForExit;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Set SQLite database folder path to output directory
            AppDomain.CurrentDomain.SetData("DataDirectory", AppDomain.CurrentDomain.BaseDirectory);

            base.OnStartup(e);

            // Initialize Serilog
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(logDir);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.File(Path.Combine(logDir, "lab_.log"), rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 5 * 1024 * 1024, // 5MB max per log file
                    retainedFileCountLimit: 14)           // Keep only 2 weeks of logs
                .CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                Log.Fatal(args.ExceptionObject as Exception, "Unhandled exception occurred.");
                MessageBox.Show("A critical error occurred. Please check the logs.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            try
            {
                InitializeDatabase();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to initialize database.");
                MessageBox.Show($"Database Init Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }

            // Setup SimpleInjector
            Container = new Container();
            Container.Options.EnableAutoVerification = false;

            // Register DbContext
            Container.Register<LabDbContext>(() => new LabDbContext(), Lifestyle.Transient);
            Container.Register<IUnitOfWork, UnitOfWork>(Lifestyle.Transient);

            // Register Repositories
            Container.Register<IPatientRepository, PatientRepository>(Lifestyle.Transient);
            Container.Register<ITestOrderRepository, TestOrderRepository>(Lifestyle.Transient);
            Container.Register<IResultRepository, ResultRepository>(Lifestyle.Transient);
            Container.Register<ITestTypeRepository, TestTypeRepository>(Lifestyle.Transient);
            Container.Register<IRepository<TestPanel>, TestPanelRepository>(Lifestyle.Transient);
            Container.Register<IStaffRepository, StaffRepository>(Lifestyle.Transient);
            Container.Register<IInvoiceRepository, InvoiceRepository>(Lifestyle.Transient);
            Container.Register<IPaymentRepository, PaymentRepository>(Lifestyle.Transient);
            Container.Register<IReportRepository, ReportRepository>(Lifestyle.Transient);
            Container.Register<IQcRepository, QcRepository>(Lifestyle.Transient);
            Container.Register<IAppointmentRepository, AppointmentRepository>(Lifestyle.Transient);
            
            // Fallback for any other IRepository<T>
            Container.RegisterConditional(typeof(IRepository<>), typeof(Repository<>), Lifestyle.Transient, c => !c.Handled);

            // Register Services
            Container.Register<IOrderService, OrderService>(Lifestyle.Transient);
            Container.Register<IResultService, ResultService>(Lifestyle.Transient);
            Container.Register<IPdfReportService>(() => new PdfReportService(
                Container.GetInstance<IResultRepository>(),
                Container.GetInstance<IRepository<LabSystem.Core.Models.TestType>>(),
                Container.GetInstance<IRepository<LabSystem.Core.Models.TestPanel>>(),
                GetLetterheadPath()), Lifestyle.Transient);
            Container.Register<IBackupService, SqliteBackupService>(Lifestyle.Transient);
            Container.Register<IBillingService, BillingService>(Lifestyle.Transient);
            Container.Register<IWorkflowService, WorkflowService>(Lifestyle.Transient);
            Container.Register<IStaffService, StaffService>(Lifestyle.Transient);
            Container.Register<QcService>(Lifestyle.Transient);
            Container.Register<IAppointmentService, AppointmentService>(Lifestyle.Transient);
            Container.Register<ISmsService>(() => new SmsService("", "", Container.GetInstance<IRepository<SmsLog>>()), Lifestyle.Singleton);

            // Register ViewModels
            Container.Register<ViewModels.MainViewModel>();
            Container.Register<ViewModels.DashboardViewModel>();
            Container.Register<ViewModels.UnifiedQueueViewModel>();
            Container.Register<ViewModels.LoginViewModel>();
            Container.Register<ViewModels.PatientsTabViewModel>();
            Container.Register<ViewModels.OrdersTabViewModel>();
            Container.Register<ViewModels.LabTabViewModel>();
            Container.Register<ViewModels.BillingTabViewModel>();
            Container.Register<ViewModels.QcViewModel>();
            Container.Register<ViewModels.AppointmentsViewModel>();
            Container.Register<ViewModels.StaffManagementViewModel>();

            // Hold a backup service reference for auto-backup on exit
            _backupServiceForExit = Container.GetInstance<IBackupService>();

            // Display PIN Login modal before opening dashboard
            var loginViewModel = Container.GetInstance<ViewModels.LoginViewModel>();
            loginViewModel.InitializeAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            var loginView = new Views.LoginView();
            loginView.DataContext = loginViewModel;
            loginViewModel.CloseAction = () => loginView.DialogResult = true;

            bool? loginResult = loginView.ShowDialog();
            if (loginResult != true || !loginViewModel.IsLoginSuccess)
            {
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow();
            mainWindow.DataContext = Container.GetInstance<ViewModels.MainViewModel>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Auto-backup on application close — fire-and-forget with 15s timeout
            try
            {
                Log.Information("Application closing — triggering automatic backup.");
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                {
                    _backupServiceForExit?.BackupNowAsync(cts.Token).ConfigureAwait(false).GetAwaiter().GetResult();
                    Log.Information("Auto-backup on exit completed successfully.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Auto-backup on exit failed or timed out.");
            }

            Log.CloseAndFlush();
            base.OnExit(e);
        }

        private static string GetLetterheadPath()
        {
            var path = FileUtilities.FindFileUpwards("Sample reports", "10 001.jpg.jpeg");
            if (path != null && File.Exists(path))
                return path;

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new[]
            {
                Path.Combine(baseDir, "Assets", "letterhead.jpg"),
                Path.Combine(baseDir, "Assets", "letterhead.jpeg"),
                Path.Combine(baseDir, "Assets", "letterhead.png"),
                Path.Combine(baseDir, "letterhead.jpg"),
                Path.Combine(baseDir, "letterhead.jpeg"),
                Path.Combine(baseDir, "letterhead.png"),
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            return candidates[0];
        }

        private static void InitializeDatabase()
        {
            using (var db = new LabDbContext())
            {
                DatabaseInitializer.Initialize(db);
            }
        }

    }
}
