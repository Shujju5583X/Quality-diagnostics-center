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
using System.Threading.Tasks;


namespace LabSystem.UI
{
    public partial class App : Application
    {
        public static Container Container { get; private set; }
        public static int AuthenticatedStaffId { get; set; }

        // Hold a reference so we can trigger backup on exit
        private IBackupService _backupServiceForExit;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Set SQLite database folder path to Local AppData output directory
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Quality Diagnostics Center");
            try
            {
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }
            }
            catch (Exception)
            {
                appDataPath = AppDomain.CurrentDomain.BaseDirectory;
            }
            AppDomain.CurrentDomain.SetData("DataDirectory", appDataPath);

            // Single-operator mode: default staff ID is 1
            AuthenticatedStaffId = 1;

            base.OnStartup(e);

            // Initialize Serilog
            string logDir = Path.Combine(FileUtilities.GetWritableDataDirectory(), "Logs");
            try
            {
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
            }
            catch (Exception)
            {
            }
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Path.Combine(logDir, "lab_.log"), rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 5 * 1024 * 1024, // 5MB max per log file
                    retainedFileCountLimit: 14)           // Keep only 2 weeks of logs
                .CreateLogger();
            Log.Information("Application startup begin.");

            // Catch WPF Dispatcher thread exceptions (XAML parse, binding, rendering errors)
            DispatcherUnhandledException += (s, args) =>
            {
                Log.Fatal(args.Exception, "Dispatcher unhandled exception.");
                string innerMessage = args.Exception.InnerException != null ? args.Exception.InnerException.Message : "";
                MessageBox.Show("Dispatcher error: " + args.Exception.Message + "\n\n" + innerMessage,
                    "Dispatcher Error", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                Log.Fatal(args.ExceptionObject as Exception, "Unhandled exception occurred.");
                MessageBox.Show("A critical error occurred. Please check the logs.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                Log.Fatal(args.Exception, "Unobserved task exception.");
                args.SetObserved();
            };

            try
            {
                InitializeDatabase();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to initialize database.");
                MessageBox.Show("Database Init Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            // Fallback for any other IRepository<T>
            Container.RegisterConditional(typeof(IRepository<>), typeof(Repository<>), Lifestyle.Transient, c => !c.Handled);

            // Register Services
            Container.Register<IOrderService, OrderService>(Lifestyle.Transient);
            Container.Register<IResultService, ResultService>(Lifestyle.Transient);
            Container.Register<IPdfReportService>(() => new PdfReportService(
                Container.GetInstance<IResultRepository>(),
                Container.GetInstance<IRepository<LabSystem.Core.Models.TestType>>(),
                Container.GetInstance<IRepository<LabSystem.Core.Models.TestPanel>>(),
                GetLetterheadPath(),
                Container.GetInstance<IRepository<LabSystem.Core.Models.Setting>>()), Lifestyle.Transient);
            Container.Register<IBackupService, SqliteBackupService>(Lifestyle.Transient);
            Container.Register<IBillingService>(() => new BillingService(
                Container.GetInstance<IInvoiceRepository>(),
                Container.GetInstance<IPaymentRepository>(),
                Container.GetInstance<ITestOrderRepository>(),
                Container.GetInstance<IRepository<TestPanel>>(),
                Container.GetInstance<IRepository<DoctorCommission>>(),
                Container.GetInstance<IRepository<Doctor>>(),
                Container.GetInstance<IUnitOfWork>()), Lifestyle.Transient);
            Container.Register<IStaffService, StaffService>(Lifestyle.Transient);
            Container.Register<ICsvBackupService, CsvBackupService>(Lifestyle.Transient);

            // Register ViewModels
            Container.Register<ViewModels.MainViewModel>();
            Container.Register<ViewModels.DashboardViewModel>(() => new ViewModels.DashboardViewModel(
                Container.GetInstance<IPatientRepository>(),
                Container.GetInstance<ITestOrderRepository>(),
                Container.GetInstance<IOrderService>(),
                Container.GetInstance<IPdfReportService>(),
                Container.GetInstance<IResultRepository>(),
                Container.GetInstance<IRepository<TestType>>(),
                Container.GetInstance<IResultService>(),
                Container.GetInstance<IBillingService>(),
                Container.GetInstance<IRepository<TestPanel>>(),
                Container.GetInstance<IBackupService>(),
                Container.GetInstance<IRepository<Doctor>>(),
                Container.GetInstance<IRepository<Department>>(),
                Container.GetInstance<IRepository<Setting>>(),
                Container.GetInstance<IUnitOfWork>(),
                Container.GetInstance<IPaymentRepository>(),
                Container.GetInstance<IRepository<DoctorCommission>>(),
                Container.GetInstance<ICsvBackupService>(),
                Container.GetInstance<IStaffService>(),
                Container.GetInstance<IStaffRepository>(),
                Container.GetInstance<IRepository<Invoice>>()), Lifestyle.Singleton);
            Container.Register<ViewModels.LoginViewModel>();
            Container.Register<ViewModels.PinSetupViewModel>(Lifestyle.Transient);
            Container.Register<ViewModels.StaffManagementViewModel>();

            try
            {
                // Hold a backup service reference for auto-backup on exit
                _backupServiceForExit = Container.GetInstance<IBackupService>();

                // Create MainWindow and assign as Application.MainWindow
                Log.Information("Creating MainWindow...");
                var mainWindow = new MainWindow();
                this.MainWindow = mainWindow;
                Log.Information("MainWindow created and assigned as Application.MainWindow.");

                // Resolve MainViewModel and set as DataContext.
                // MainViewModel starts on the login screen (inside the main window)
                // and switches to DashboardViewModel after successful PIN entry.
                Log.Information("Resolving MainViewModel...");
                mainWindow.DataContext = Container.GetInstance<ViewModels.MainViewModel>();
                Log.Information("MainViewModel resolved and assigned.");

                Log.Information("Calling mainWindow.Show()...");
                mainWindow.Show();
                Log.Information("mainWindow.Show() completed.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to start MainWindow after login.");
                string startupInnerMessage = ex.InnerException != null ? ex.InnerException.Message : "";
                MessageBox.Show("Startup crash: " + ex.Message + "\n\n" + startupInnerMessage,
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                Log.Information("Application closing — triggering automatic backup.");
                if (_backupServiceForExit != null)
                {
                    var backupTask = Task.Run(() => _backupServiceForExit.BackupNowAsync(CancellationToken.None));
                    if (backupTask.Wait(TimeSpan.FromSeconds(30)))
                    {
                        if (backupTask.IsFaulted)
                            Log.Warning(backupTask.Exception, "Auto-backup on exit failed.");
                        else
                            Log.Information("Auto-backup on exit completed successfully.");
                    }
                    else
                    {
                        Log.Warning("Auto-backup on exit timed out after 30 seconds.");
                    }
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

            var path = FileUtilities.FindFileUpwards("Sample reports", "10 001.jpg.jpeg");
            if (path != null && File.Exists(path))
                return path;

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
