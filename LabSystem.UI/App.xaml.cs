using System;
using System.Configuration;
using System.IO;
using System.Windows;
using LabSystem.Core.Interfaces;
using LabSystem.Data;
using LabSystem.Data.Repositories;
using LabSystem.Services;
using SimpleInjector;
using Serilog;
using System.Reflection;


namespace LabSystem.UI
{
    public partial class App : Application
    {
        public static Container Container { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Set SQLite database folder path to output directory
            AppDomain.CurrentDomain.SetData("DataDirectory", AppDomain.CurrentDomain.BaseDirectory);

            base.OnStartup(e);

            // Initialize Serilog
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(logDir);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(Path.Combine(logDir, "lab_.log"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                Log.Fatal(args.ExceptionObject as Exception, "Unhandled exception occurred.");
                MessageBox.Show("A critical error occurred. Please check the logs.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            InitializeDatabase();

            // Setup SimpleInjector
            Container = new Container();
            Container.Options.EnableAutoVerification = false;

            // Register DbContext
            Container.Register<LabDbContext>(Lifestyle.Transient);

            // Register Repositories
            Container.Register(typeof(IRepository<>), typeof(Repository<>), Lifestyle.Transient);
            Container.Register<IPatientRepository, PatientRepository>(Lifestyle.Transient);
            Container.Register<ITestOrderRepository, TestOrderRepository>(Lifestyle.Transient);
            Container.Register<IResultRepository, ResultRepository>(Lifestyle.Transient);
            Container.Register<ITestTypeRepository, TestTypeRepository>(Lifestyle.Transient);
            Container.Register<IStaffRepository, StaffRepository>(Lifestyle.Transient);
            Container.Register<IAuditLogRepository, AuditLogRepository>(Lifestyle.Transient);
            Container.Register<IReportRepository, ReportRepository>(Lifestyle.Transient);

            // Register Services
            Container.Register<IAuthService, AuthService>(Lifestyle.Transient);
            Container.Register<IOrderService, OrderService>(Lifestyle.Transient);
            Container.Register<IResultService, ResultService>(Lifestyle.Transient);
            Container.Register<IPdfReportService>(() => new PdfReportService(
                Container.GetInstance<IResultRepository>(),
                GetLetterheadPath()), Lifestyle.Transient);
            Container.Register<IBackupService, SqliteBackupService>(Lifestyle.Transient);
            Container.Register<IBillingService, BillingService>(Lifestyle.Transient);

            // Register ViewModels
            Container.Register<ViewModels.MainViewModel>();
            Container.Register<ViewModels.LoginViewModel>();
            Container.Register<ViewModels.DashboardViewModel>();

            var mainWindow = new MainWindow();
            mainWindow.DataContext = Container.GetInstance<ViewModels.MainViewModel>();
            mainWindow.Show();
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
                Path.Combine(baseDir, "Sample reports", "10 001.jpg.jpeg"),
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
                db.Database.Initialize(false);

                bool tablesExist;
                try
                {
                    db.Database.ExecuteSqlCommand("SELECT 1 FROM Patients LIMIT 1;");
                    tablesExist = true;
                }
                catch
                {
                    tablesExist = false;
                }

                if (!tablesExist)
                {
                    InitializeSchema(db);
                }

                EnsureSchemaUpToDate(db);
            }
        }

        private static void InitializeSchema(LabDbContext db)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string sql = null;
            string seedSql = null;

            using (var stream = assembly.GetManifestResourceStream("LabSystem.UI.Resources.V1__init.sql"))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        sql = reader.ReadToEnd();
                    }
                }
            }

            using (var stream = assembly.GetManifestResourceStream("LabSystem.UI.Resources.seed.sql"))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        seedSql = reader.ReadToEnd();
                    }
                }
            }

            if (string.IsNullOrEmpty(sql))
            {
                var scriptPath = FindFileUpwards("LabSystem.Data", "Migrations", "V1__init.sql");
                if (scriptPath != null && File.Exists(scriptPath))
                {
                    sql = File.ReadAllText(scriptPath);
                    Log.Information("Loaded schema from: {Path}", scriptPath);
                }
            }

            if (string.IsNullOrEmpty(seedSql))
            {
                var seedPath = FindFileUpwards("", "seed.sql");
                if (seedPath != null && File.Exists(seedPath))
                {
                    seedSql = File.ReadAllText(seedPath);
                    Log.Information("Loaded seed data from: {Path}", seedPath);
                }
            }

            if (!string.IsNullOrEmpty(sql))
            {
                db.Database.ExecuteSqlCommand(sql);
                Log.Information("Database schema initialized.");
            }
            else
            {
                Log.Warning("Could not find V1__init.sql schema file.");
            }

            if (!string.IsNullOrEmpty(seedSql))
            {
                db.Database.ExecuteSqlCommand(seedSql);
                Log.Information("Seed data applied.");
            }
        }

        private static void EnsureSchemaUpToDate(LabDbContext db)
        {
            var migrations = new[]
            {
                new { Table = "Patients", Column = "Gender", Type = "TEXT" },
                new { Table = "TestOrders", Column = "ReferredBy", Type = "TEXT" },
                new { Table = "Staff", Column = "FailedLoginAttempts", Type = "INTEGER DEFAULT 0" },
                new { Table = "Staff", Column = "LockoutEnd", Type = "TEXT" },
            };

            foreach (var migration in migrations)
            {
                try
                {
                    db.Database.ExecuteSqlCommand($"ALTER TABLE {migration.Table} ADD COLUMN {migration.Column} {migration.Type};");
                    Log.Debug("Applied migration: Added column {Column} to {Table}", migration.Column, migration.Table);
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Migration skipped (column may already exist): {Table}.{Column}", migration.Table, migration.Column);
                }
            }
        }

        private static string FindFileUpwards(params string[] pathParts)
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, Path.Combine(pathParts));
                if (File.Exists(candidate))
                    return candidate;
                dir = dir.Parent;
            }
            return null;
        }
    }
}
