using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Serilog;
using LabSystem.Core;

namespace LabSystem.Data
{
    public static class DatabaseInitializer
    {
        public static void Initialize(LabDbContext db)
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

            var staffCount = db.Database.SqlQuery<int>("SELECT COUNT(*) FROM Staff").FirstOrDefault();
            if (staffCount == 0)
            {
                db.Database.ExecuteSqlCommand("INSERT INTO Staff (FullName, PinHash) VALUES ('Lab Technician', '$2a$11$kqAe1LF4dOyHZYBxS2LVCuqYuFe86RStkbVUPpbbDNeFJPPH5wRei');");
                Log.Information("Default staff record seeded.");
            }
            else
            {
                Log.Information("Staff records exist; skipping PIN reset.");
            }
        }

        private static string LoadFromResource(string suffix)
        {
            var assemblies = new[] { 
                Assembly.GetExecutingAssembly(), 
                Assembly.GetEntryAssembly(),
                Assembly.GetCallingAssembly()
            }.Where(a => a != null).Distinct();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var resourceName = assembly.GetManifestResourceNames()
                        .FirstOrDefault(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
                    if (resourceName != null)
                    {
                        using (var stream = assembly.GetManifestResourceStream(resourceName))
                        {
                            if (stream != null)
                            {
                                using (var reader = new StreamReader(stream))
                                {
                                    return reader.ReadToEnd();
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore errors during assembly inspection
                }
            }
            return null;
        }

        private static void InitializeSchema(LabDbContext db)
        {
            string sql = LoadFromResource("V1__init.sql");
            string seedSql = LoadFromResource("seed.sql");

            if (string.IsNullOrEmpty(sql))
            {
                var scriptPath = FileUtilities.FindFileUpwards("LabSystem.Data", "Migrations", "V1__init.sql");
                if (scriptPath != null && File.Exists(scriptPath))
                {
                    sql = File.ReadAllText(scriptPath);
                    Log.Information("Loaded schema from: {Path}", scriptPath);
                }
            }

            if (string.IsNullOrEmpty(seedSql))
            {
                var seedPath = FileUtilities.FindFileUpwards("", "seed.sql");
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
            // Safe incremental column additions (idempotent — skipped if column already exists)
            var migrations = new[]
            {
                new { Table = "Patients",    Column = "Gender",               Type = "TEXT" },
                new { Table = "TestOrders",  Column = "ReferredBy",           Type = "TEXT" },
                new { Table = "TestTypes",   Column = "SampleType",           Type = "TEXT" },
                new { Table = "TestTypes",   Column = "InputType",            Type = "INTEGER DEFAULT 0" },
                new { Table = "Invoices",    Column = "DiscountAmount",        Type = "REAL DEFAULT 0" },
                new { Table = "Invoices",    Column = "TaxAmount",             Type = "REAL DEFAULT 0" },
                new { Table = "Invoices",    Column = "DiscountPercent",       Type = "REAL DEFAULT 0" },
                new { Table = "Invoices",    Column = "TaxPercent",            Type = "REAL DEFAULT 0" },
                // Audit timestamp columns for simplified single-person workflow
                new { Table = "TestOrders",  Column = "CreatedAt",            Type = "DATETIME" },
                new { Table = "TestOrders",  Column = "UpdatedAt",            Type = "DATETIME" },
                new { Table = "Results",     Column = "CreatedAt",            Type = "DATETIME" },
                new { Table = "Results",     Column = "UpdatedAt",            Type = "DATETIME" },
                new { Table = "Results",     Column = "ValueText",            Type = "TEXT" },
                new { Table = "Invoices",    Column = "UpdatedAt",            Type = "DATETIME" },
                // Remove legacy auth columns from Staff table
                new { Table = "Staff",       Column = "Role",                 Type = "TEXT" },
                new { Table = "Staff",       Column = "PinHash",              Type = "TEXT" },
                new { Table = "Staff",       Column = "FailedLoginAttempts",  Type = "INTEGER DEFAULT 0" },
                new { Table = "Staff",       Column = "LockoutEnd",           Type = "DATETIME" },
                new { Table = "Staff",       Column = "CreatedAt",            Type = "DATETIME" },
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

            // Ensure no NULL values exist for Staff.CreatedAt to avoid mapping issues
            try
            {
                db.Database.ExecuteSqlCommand("UPDATE Staff SET CreatedAt = CURRENT_TIMESTAMP WHERE CreatedAt IS NULL;");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to update NULL CreatedAt values in Staff table.");
            }

            try
            {
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

                db.Database.ExecuteSqlCommand("CREATE INDEX IF NOT EXISTS IX_Specimens_OrderId ON Specimens (OrderId);");
                db.Database.ExecuteSqlCommand("CREATE INDEX IF NOT EXISTS IX_ReferenceRanges_TestTypeId ON ReferenceRanges (TestTypeId);");
                db.Database.ExecuteSqlCommand("CREATE INDEX IF NOT EXISTS IX_Payments_InvoiceId ON Payments (InvoiceId);");

                // QC tables
                db.Database.ExecuteSqlCommand(@"
                    CREATE TABLE IF NOT EXISTS QcRuns (
                        QcRunId INTEGER PRIMARY KEY AUTOINCREMENT,
                        TestTypeId INTEGER NOT NULL,
                        ControlName TEXT NOT NULL,
                        RunDate DATETIME NOT NULL,
                        MeasuredValue REAL NOT NULL,
                        LotNumber TEXT,
                        TargetValue REAL,
                        SD REAL,
                        CreatedAt DATETIME,
                        FOREIGN KEY(TestTypeId) REFERENCES TestTypes(TypeId)
                    );
                ");

                db.Database.ExecuteSqlCommand(@"
                    CREATE TABLE IF NOT EXISTS QcLots (
                        QcLotId INTEGER PRIMARY KEY AUTOINCREMENT,
                        TestTypeId INTEGER NOT NULL,
                        LotNumber TEXT NOT NULL,
                        TargetValue REAL NOT NULL,
                        SD REAL NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        CreatedAt DATETIME,
                        FOREIGN KEY(TestTypeId) REFERENCES TestTypes(TypeId)
                    );
                ");

                db.Database.ExecuteSqlCommand("CREATE INDEX IF NOT EXISTS IX_QcRuns_TestTypeId ON QcRuns (TestTypeId);");
                db.Database.ExecuteSqlCommand("CREATE INDEX IF NOT EXISTS IX_QcRuns_RunDate ON QcRuns (RunDate);");
                db.Database.ExecuteSqlCommand("CREATE INDEX IF NOT EXISTS IX_QcLots_TestTypeId ON QcLots (TestTypeId);");

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

                Log.Information("Schema tables verified/created.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create/verify schema tables.");
            }

            try
            {
                var hasRejectedRows = db.Database.SqlQuery<int>("SELECT COUNT(*) FROM Results WHERE Value = -999.0").FirstOrDefault() > 0;
                if (hasRejectedRows)
                {
                    db.Database.ExecuteSqlCommand("UPDATE Results SET Value = NULL WHERE Value = -999.0;");
                    Log.Information("Data migration: Converted legacy -999.0 sentinel values in Results table to NULL.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to run Results -999.0 sentinel migration.");
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
                        ('Thyroid Profile Panel', 'Thyroid Function Test including T3, T4, and TSH screening.', 900.00),
                        ('CBC Panel', 'Complete Blood Count including Haemoglobin, Haematocrit, RBC, WBC, Platelet, and differential counts.', 800.00),
                        ('KFT Panel', 'Kidney Function Test including Blood Urea, Creatinine, Uric Acid, and BUN.', 600.00),
                        ('LFT Panel', 'Liver Function Test including SGOT, SGPT, ALP, Bilirubin, Protein, Albumin, and Globulin.', 700.00),
                        ('Electrolyte Panel', 'Serum Electrolytes including Sodium, Potassium, Chloride, and Bicarbonate.', 400.00);

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

                        INSERT INTO PanelTestTypes (PanelId, TypeId) VALUES
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Hemoglobin (Hb)')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Packed Cell Volume (PCV)')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Total RBC count')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Total WBC count')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Platelet Count')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Mean Corpuscular Volume (MCV)')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Mean Corpuscular Hb (MCH)')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Mean Corpuscular Hb Concn. (MCHC)')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'RDW')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Neutrophils')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Lymphocytes')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Eosinophils')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Monocytes')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Basophils'));

                        INSERT INTO PanelTestTypes (PanelId, TypeId) VALUES
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'KFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Urea (KFT)')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'KFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Creatinine (KFT)')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'KFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Uric Acid (KFT)')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'KFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Calcium, Total (KFT)'));

                        INSERT INTO PanelTestTypes (PanelId, TypeId) VALUES
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'AST (SGOT)')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'ALT (SGPT)')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Alkaline Phosphatase (LFT)')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Bilirubin Total')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Bilirubin Direct')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Bilirubin Indirect')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Total Protein (LFT)')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Albumin (LFT)'));

                        INSERT INTO PanelTestTypes (PanelId, TypeId) VALUES
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'Electrolyte Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Sodium')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'Electrolyte Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Potassium')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'Electrolyte Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Chloride')),
                        ((SELECT PanelId FROM TestPanels WHERE Name = 'Electrolyte Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Bicarbonate'));
                    ");
                    Log.Information("Seeded test panels.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to apply seed migrations.");
            }
        }
    }
}
