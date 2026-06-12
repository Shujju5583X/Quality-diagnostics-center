using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
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
                .MinimumLevel.Warning()
                .WriteTo.File(Path.Combine(logDir, "lab_.log"), rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 5 * 1024 * 1024, // 5MB max per log file to avoid filling disk
                    retainedFileCountLimit: 14) // Keep only 2 weeks of logs
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
            Container.Register<IPatientRepository, PatientRepository>(Lifestyle.Transient);
            Container.Register<ITestOrderRepository, TestOrderRepository>(Lifestyle.Transient);
            Container.Register<IResultRepository, ResultRepository>(Lifestyle.Transient);
            Container.Register<ITestTypeRepository, TestTypeRepository>(Lifestyle.Transient);
            Container.Register<IRepository<TestPanel>, TestPanelRepository>(Lifestyle.Transient);
            Container.RegisterConditional(typeof(IRepository<>), typeof(Repository<>), Lifestyle.Transient, c => !c.Handled);
            Container.Register<IStaffRepository, StaffRepository>(Lifestyle.Transient);
            Container.Register<IAuditLogRepository, AuditLogRepository>(Lifestyle.Transient);
            Container.Register<IReportRepository, ReportRepository>(Lifestyle.Transient);
            Container.Register<IQCResultRepository, QCResultRepository>(Lifestyle.Transient);

            // Register Services
            Container.Register<IAuthService, AuthService>(Lifestyle.Transient);
            Container.Register<IOrderService, OrderService>(Lifestyle.Transient);
            Container.Register<IResultService, ResultService>(Lifestyle.Transient);
            Container.Register<IPdfReportService>(() => new PdfReportService(
                Container.GetInstance<IResultRepository>(),
                Container.GetInstance<IRepository<LabSystem.Core.Models.TestType>>(),
                Container.GetInstance<IRepository<LabSystem.Core.Models.TestPanel>>(),
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
                new { Table = "TestTypes", Column = "SampleType", Type = "TEXT" },
                new { Table = "TestOrders", Column = "DoctorId", Type = "INTEGER" },
                new { Table = "Invoices", Column = "DiscountAmount", Type = "REAL DEFAULT 0" },
                new { Table = "Invoices", Column = "TaxAmount", Type = "REAL DEFAULT 0" },
            };

            foreach (var migration in migrations)
            {
                try
                {
                    db.Database.ExecuteSqlCommand($"ALTER TABLE {migration.Table} ADD COLUMN {migration.Column} {migration.Type};");
                    Log.Information("Applied migration: Added column {Column} to {Table}", migration.Column, migration.Table);
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Migration skipped (column may already exist): {Table}.{Column}", migration.Table, migration.Column);
                }
            }

            try
            {
                db.Database.ExecuteSqlCommand(@"
                    CREATE TABLE IF NOT EXISTS Doctors (
                        DoctorId INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Specialization TEXT,
                        ClinicName TEXT,
                        ContactPhone TEXT,
                        CommissionPercent REAL DEFAULT 0
                    );
                ");
                
                db.Database.ExecuteSqlCommand(@"
                    CREATE TABLE IF NOT EXISTS Specimens (
                        SpecimenId INTEGER PRIMARY KEY AUTOINCREMENT,
                        OrderId INTEGER NOT NULL,
                        Barcode TEXT NOT NULL UNIQUE,
                        SampleType TEXT NOT NULL,
                        CollectionTime DATETIME,
                        CollectedBy TEXT,
                        Status TEXT NOT NULL,
                        RejectionReason TEXT,
                        FOREIGN KEY(OrderId) REFERENCES TestOrders(OrderId) ON DELETE CASCADE
                    );
                ");

                db.Database.ExecuteSqlCommand(@"
                    CREATE TABLE IF NOT EXISTS ReferenceRanges (
                        ReferenceRangeId INTEGER PRIMARY KEY AUTOINCREMENT,
                        TestTypeId INTEGER NOT NULL,
                        Gender TEXT NOT NULL,
                        AgeMin INTEGER NOT NULL,
                        AgeMax INTEGER NOT NULL,
                        RangeLow REAL,
                        RangeHigh REAL,
                        FOREIGN KEY(TestTypeId) REFERENCES TestTypes(TypeId) ON DELETE CASCADE
                    );
                ");

                db.Database.ExecuteSqlCommand(@"
                    CREATE TABLE IF NOT EXISTS TestPanels (
                        PanelId INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Description TEXT,
                        Price REAL NOT NULL
                    );
                ");

                db.Database.ExecuteSqlCommand(@"
                    CREATE TABLE IF NOT EXISTS PanelTestTypes (
                        PanelId INTEGER NOT NULL,
                        TypeId INTEGER NOT NULL,
                        PRIMARY KEY(PanelId, TypeId),
                        FOREIGN KEY(PanelId) REFERENCES TestPanels(PanelId) ON DELETE CASCADE,
                        FOREIGN KEY(TypeId) REFERENCES TestTypes(TypeId) ON DELETE CASCADE
                    );
                ");

                db.Database.ExecuteSqlCommand(@"
                    CREATE TABLE IF NOT EXISTS Payments (
                        PaymentId INTEGER PRIMARY KEY AUTOINCREMENT,
                        InvoiceId INTEGER NOT NULL,
                        Amount REAL NOT NULL,
                        PaymentMethod TEXT,
                        PaymentDate DATETIME NOT NULL,
                        FOREIGN KEY(InvoiceId) REFERENCES Invoices(InvoiceId) ON DELETE CASCADE
                    );
                ");

                db.Database.ExecuteSqlCommand(@"
                    CREATE TABLE IF NOT EXISTS QCResults (
                        QCResultId INTEGER PRIMARY KEY AUTOINCREMENT,
                        TestTypeId INTEGER NOT NULL,
                        ControlLevel TEXT NOT NULL,
                        ExpectedMean REAL NOT NULL,
                        StandardDeviation REAL NOT NULL,
                        MeasuredValue REAL NOT NULL,
                        RecordedAt DATETIME NOT NULL,
                        TechnicianId INTEGER NOT NULL,
                        Remarks TEXT,
                        FOREIGN KEY(TestTypeId) REFERENCES TestTypes(TypeId) ON DELETE CASCADE,
                        FOREIGN KEY(TechnicianId) REFERENCES Staff(StaffId) ON DELETE CASCADE
                    );
                ");

                db.Database.ExecuteSqlCommand("CREATE INDEX IF NOT EXISTS IX_TestOrders_DoctorId ON TestOrders (DoctorId);");
                db.Database.ExecuteSqlCommand("CREATE INDEX IF NOT EXISTS IX_Specimens_OrderId ON Specimens (OrderId);");
                db.Database.ExecuteSqlCommand("CREATE INDEX IF NOT EXISTS IX_ReferenceRanges_TestTypeId ON ReferenceRanges (TestTypeId);");
                db.Database.ExecuteSqlCommand("CREATE INDEX IF NOT EXISTS IX_Payments_InvoiceId ON Payments (InvoiceId);");
                db.Database.ExecuteSqlCommand("CREATE INDEX IF NOT EXISTS IX_QCResults_TestTypeId ON QCResults (TestTypeId);");

                try
                {
                    db.Database.ExecuteSqlCommand("ALTER TABLE Results ADD COLUMN IsAmended INTEGER NOT NULL DEFAULT 0;");
                    db.Database.ExecuteSqlCommand("ALTER TABLE Results ADD COLUMN AmendmentReason TEXT;");
                    db.Database.ExecuteSqlCommand("ALTER TABLE Results ADD COLUMN AmendedAt DATETIME;");
                }
                catch (Exception)
                {
                    // Columns might already exist, ignore error
                }

                Log.Information("Phase 2/3/4 database tables verified/created.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create Phase 2 tables.");
            }

            try
            {
                // Set SampleTypes for all TestTypes if they are null/empty
                db.Database.ExecuteSqlCommand(@"
                    UPDATE TestTypes SET SampleType = 'Blood' WHERE (SampleType IS NULL OR SampleType = '') AND Category = 'HEMATOLOGY';
                    UPDATE TestTypes SET SampleType = 'Serum' WHERE (SampleType IS NULL OR SampleType = '') AND Category IN ('SEROLOGY', 'IMMUNOASSAY', 'ENDOCRINOLOGY', 'BIOCHEMISTRY');
                    UPDATE TestTypes SET SampleType = 'Urine' WHERE (SampleType IS NULL OR SampleType = '') AND Category = 'CLINICAL PATHOLOGY';
                    UPDATE TestTypes SET SampleType = 'Blood' WHERE (SampleType IS NULL OR SampleType = '') AND Name = 'Blood Grouping & Rh';
                ");

                // Check if Doctors table is empty
                var doctorCount = db.Database.SqlQuery<int>("SELECT COUNT(*) FROM Doctors").FirstOrDefault();
                if (doctorCount == 0)
                {
                    db.Database.ExecuteSqlCommand(@"
                        INSERT INTO Doctors (Name, Specialization, ClinicName, ContactPhone, CommissionPercent) VALUES
                        ('Dr. Robert Clark', 'Cardiologist', 'Metro Heart Care', '555-0199', 15.0),
                        ('Dr. Alice Vance', 'General Physician', 'City Clinic', '555-0288', 10.0),
                        ('Dr. Sarah Patel', 'Endocrinologist', 'Diabetes Care Center', '555-0377', 12.0);
                    ");
                    Log.Information("Seeded referring doctors.");
                }

                // Check if ReferenceRanges table is empty
                var refRangeCount = db.Database.SqlQuery<int>("SELECT COUNT(*) FROM ReferenceRanges").FirstOrDefault();
                if (refRangeCount == 0)
                {
                    db.Database.ExecuteSqlCommand(@"
                        INSERT INTO ReferenceRanges (TestTypeId, Gender, AgeMin, AgeMax, RangeLow, RangeHigh) VALUES
                        ((SELECT TypeId FROM TestTypes WHERE Name = 'Hemoglobin (Hb)'), 'Male', 12, 120, 13.0, 17.0),
                        ((SELECT TypeId FROM TestTypes WHERE Name = 'Hemoglobin (Hb)'), 'Female', 12, 120, 12.0, 15.0),
                        ((SELECT TypeId FROM TestTypes WHERE Name = 'Hemoglobin (Hb)'), 'Male', 0, 11, 11.0, 14.5),
                        ((SELECT TypeId FROM TestTypes WHERE Name = 'Hemoglobin (Hb)'), 'Female', 0, 11, 11.0, 14.5),
                        ((SELECT TypeId FROM TestTypes WHERE Name = 'Hemoglobin (Hb)'), 'Other', 0, 120, 12.0, 16.0);
                    ");
                    Log.Information("Seeded reference ranges.");
                }

                // Check if TestPanels table is empty
                var panelCount = db.Database.SqlQuery<int>("SELECT COUNT(*) FROM TestPanels").FirstOrDefault();
                if (panelCount == 0)
                {
                    db.Database.ExecuteSqlCommand(@"
                        INSERT INTO TestPanels (Name, Description, Price) VALUES
                        ('Lipid Profile Panel', 'Comprehensive assessment of total cholesterol, triglycerides, HDL, LDL, VLDL, and non-HDL cholesterol.', 1200.00),
                        ('Thyroid Profile Panel', 'Thyroid Function Test including T3, T4, and TSH screening.', 900.00);
                        
                        INSERT INTO PanelTestTypes (PanelId, TypeId) VALUES
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'Lipid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Cholesterol, Total')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'Lipid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Triglycerides')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'Lipid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'HDL Cholesterol')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'Lipid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'LDL Cholesterol')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'Lipid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'VLDL Cholesterol')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'Lipid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Non-HDL Cholesterol'));

                        INSERT INTO PanelTestTypes (PanelId, TypeId) VALUES
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'Thyroid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Triiodothyronine (T3)')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'Thyroid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Thyroxine (T4)')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'Thyroid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'TSH (Thyroid Stimulating Hormone)'));
                    ");
                    Log.Information("Seeded test panels.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to apply seed migrations.");
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
