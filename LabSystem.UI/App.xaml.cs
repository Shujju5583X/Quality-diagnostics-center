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
                    string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "LabSystem.Data", "Migrations", "V1__init.sql");
                    if (File.Exists(scriptPath))
                    {
                        var sql = File.ReadAllText(scriptPath);
                        db.Database.ExecuteSqlCommand(sql);
                    }
                    
                    // Run seed if provided
                    string seedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "seed.sql");
                    if (File.Exists(seedPath))
                    {
                        var seedSql = File.ReadAllText(seedPath);
                        db.Database.ExecuteSqlCommand(seedSql);
                    }
                }
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

        private void SecureConnectionString()
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                ConnectionStringsSection section = config.GetSection("connectionStrings") as ConnectionStringsSection;
                if (section != null && !section.SectionInformation.IsProtected)
                {
                    section.SectionInformation.ProtectSection("DataProtectionConfigurationProvider");
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("connectionStrings");
                    Log.Information("App.config connectionStrings section secured via DPAPI.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to encrypt connectionStrings configuration section.");
            }
        }
    }
}
