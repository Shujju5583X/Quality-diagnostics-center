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
            // Enable PDFsharp to resolve installed Windows fonts automatically
            PdfSharp.Fonts.GlobalFontSettings.UseWindowsFontsUnderWindows = true;

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

            // Initialize DB
            using (var db = new LabDbContext())
            {
                db.Database.Initialize(false);
                
                // Run V1__init.sql if tables don't exist
                try
                {
                    db.Database.ExecuteSqlCommand("SELECT 1 FROM Patients LIMIT 1;");
                }
                catch
                {
                    // Attempt to load from Embedded Resources (Production Failsafe)
                    string sql = null;
                    string seedSql = null;
                    var assembly = Assembly.GetExecutingAssembly();
                    
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

                    // Fallback to relative file path for local development/scaffolding
                    if (string.IsNullOrEmpty(sql))
                    {
                        string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "LabSystem.Data", "Migrations", "V1__init.sql");
                        if (File.Exists(scriptPath))
                        {
                            sql = File.ReadAllText(scriptPath);
                        }
                    }

                    if (string.IsNullOrEmpty(seedSql))
                    {
                        string seedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "seed.sql");
                        if (File.Exists(seedPath))
                        {
                            seedSql = File.ReadAllText(seedPath);
                        }
                    }

                    // Execute schemas if found
                    if (!string.IsNullOrEmpty(sql))
                    {
                        db.Database.ExecuteSqlCommand(sql);
                    }
                    if (!string.IsNullOrEmpty(seedSql))
                    {
                        db.Database.ExecuteSqlCommand(seedSql);
                    }
                }

                // Dynamically update schema for existing database files to avoid EF exceptions
                try { db.Database.ExecuteSqlCommand("ALTER TABLE Patients ADD COLUMN Gender TEXT;"); } catch { }
                try { db.Database.ExecuteSqlCommand("ALTER TABLE TestOrders ADD COLUMN ReferredBy TEXT;"); } catch { }
                try { db.Database.ExecuteSqlCommand("ALTER TABLE Staff ADD COLUMN FailedLoginAttempts INTEGER DEFAULT 0;"); } catch { }
                try { db.Database.ExecuteSqlCommand("ALTER TABLE Staff ADD COLUMN LockoutEnd TEXT;"); } catch { }
            }

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

            // Register Services
            Container.Register<IAuthService, AuthService>(Lifestyle.Transient);
            Container.Register<IOrderService, OrderService>(Lifestyle.Transient);
            Container.Register<IResultService, ResultService>(Lifestyle.Transient);
            Container.Register<IPdfReportService, PdfReportService>(Lifestyle.Transient);
            Container.Register<IBackupService, SqliteBackupService>(Lifestyle.Transient);

            // Register ViewModels
            Container.Register<ViewModels.MainViewModel>();
            Container.Register<ViewModels.LoginViewModel>();
            Container.Register<ViewModels.DashboardViewModel>();

            var mainWindow = new MainWindow();
            mainWindow.DataContext = Container.GetInstance<ViewModels.MainViewModel>();
            mainWindow.Show();
        }
    }
}
