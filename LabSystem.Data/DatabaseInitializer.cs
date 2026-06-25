using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;
using LabSystem.Core;

namespace LabSystem.Data
{
    public static class DatabaseInitializer
    {
        private static readonly List<Migration> Migrations = new List<Migration>
        {
            new Migration(2, "Update DepartmentId in TestTypes and seed sample types", @"
                INSERT OR IGNORE INTO Departments (Name)
                SELECT DISTINCT Category FROM TestTypes WHERE Category IS NOT NULL AND Category != '';

                UPDATE TestTypes
                SET DepartmentId = (SELECT DepartmentId FROM Departments WHERE Name = TestTypes.Category)
                WHERE DepartmentId IS NULL OR DepartmentId = 0;

                UPDATE TestTypes SET SampleType = 'Blood' WHERE (SampleType IS NULL OR SampleType = '') AND Category = 'HEMATOLOGY';
                UPDATE TestTypes SET SampleType = 'Blood' WHERE (SampleType IS NULL OR SampleType = '') AND Category IN ('SEROLOGY', 'IMMUNOASSAY', 'ENDOCRINOLOGY', 'BIOCHEMISTRY');
                UPDATE TestTypes SET SampleType = 'Urine' WHERE (SampleType IS NULL OR SampleType = '') AND Category = 'CLINICAL PATHOLOGY';
                UPDATE TestTypes SET SampleType = 'Blood' WHERE (SampleType IS NULL OR SampleType = '') AND Name = 'Blood Grouping & Rh';
            "),
            new Migration(3, "Seed reference ranges", @"
                INSERT INTO ReferenceRanges (TestTypeId, Gender, AgeMin, AgeMax, RangeLow, RangeHigh)
                SELECT TypeId, 'Male', 12, 120, 13.0, 17.0 FROM TestTypes WHERE Name = 'Hemoglobin (Hb)'
                AND NOT EXISTS (SELECT 1 FROM ReferenceRanges WHERE TestTypeId = (SELECT TypeId FROM TestTypes WHERE Name = 'Hemoglobin (Hb)') LIMIT 1);

                INSERT INTO ReferenceRanges (TestTypeId, Gender, AgeMin, AgeMax, RangeLow, RangeHigh)
                SELECT TypeId, 'Female', 12, 120, 12.0, 15.0 FROM TestTypes WHERE Name = 'Hemoglobin (Hb)'
                AND NOT EXISTS (SELECT 1 FROM ReferenceRanges WHERE TestTypeId = (SELECT TypeId FROM TestTypes WHERE Name = 'Hemoglobin (Hb)') AND Gender = 'Female' LIMIT 1);

                INSERT INTO ReferenceRanges (TestTypeId, Gender, AgeMin, AgeMax, RangeLow, RangeHigh)
                SELECT TypeId, 'Male', 0, 11, 11.0, 14.5 FROM TestTypes WHERE Name = 'Hemoglobin (Hb)'
                AND NOT EXISTS (SELECT 1 FROM ReferenceRanges WHERE TestTypeId = (SELECT TypeId FROM TestTypes WHERE Name = 'Hemoglobin (Hb)') AND AgeMax <= 11 LIMIT 1);

                INSERT INTO ReferenceRanges (TestTypeId, Gender, AgeMin, AgeMax, RangeLow, RangeHigh)
                SELECT TypeId, 'Female', 0, 11, 11.0, 14.5 FROM TestTypes WHERE Name = 'Hemoglobin (Hb)'
                AND NOT EXISTS (SELECT 1 FROM ReferenceRanges WHERE TestTypeId = (SELECT TypeId FROM TestTypes WHERE Name = 'Hemoglobin (Hb)') AND Gender = 'Female' AND AgeMax <= 11 LIMIT 1);

                INSERT INTO ReferenceRanges (TestTypeId, Gender, AgeMin, AgeMax, RangeLow, RangeHigh)
                SELECT TypeId, 'Other', 0, 120, 12.0, 16.0 FROM TestTypes WHERE Name = 'Hemoglobin (Hb)'
                AND NOT EXISTS (SELECT 1 FROM ReferenceRanges WHERE TestTypeId = (SELECT TypeId FROM TestTypes WHERE Name = 'Hemoglobin (Hb)') AND Gender = 'Other' LIMIT 1);
            "),
            new Migration(4, "Seed test panels", @"
                INSERT OR IGNORE INTO TestPanels (Name, Description, Price) VALUES
                ('Lipid Profile Panel', 'Comprehensive assessment of total cholesterol, triglycerides, HDL, LDL, and VLDL.', 1200.00),
                ('Thyroid Profile Panel', 'Thyroid Function Test including T3, T4, and TSH screening.', 900.00),
                ('CBC Panel', 'Complete Blood Count including Haemoglobin, RBC, WBC, ESR, and Eosinophils.', 800.00),
                ('KFT Panel', 'Kidney Function Test including Blood Urea, Creatinine, and Uric Acid.', 600.00),
                ('LFT Panel', 'Liver Function Test including SGOT, SGPT, ALP, and Bilirubin.', 700.00),
                ('Electrolyte Panel', 'Serum Electrolytes including Sodium, Potassium, and Chloride.', 400.00);
            "),
            new Migration(5, "Link panel test types", @"
                INSERT OR IGNORE INTO PanelTestTypes (PanelId, TypeId)
                SELECT p.PanelId, t.TypeId FROM TestPanels p, TestTypes t WHERE p.Name = 'Lipid Profile Panel' AND t.Name IN ('Cholesterol, Total', 'Triglycerides', 'HDL Cholesterol', 'LDL Cholesterol', 'VLDL Cholesterol');

                INSERT OR IGNORE INTO PanelTestTypes (PanelId, TypeId)
                SELECT p.PanelId, t.TypeId FROM TestPanels p, TestTypes t WHERE p.Name = 'Thyroid Profile Panel' AND t.Name IN ('Triiodothyronine (T3)', 'Thyroxine (T4)', 'TSH (Thyroid Stimulating Hormone)');

                INSERT OR IGNORE INTO PanelTestTypes (PanelId, TypeId)
                SELECT p.PanelId, t.TypeId FROM TestPanels p, TestTypes t WHERE p.Name = 'CBC Panel' AND t.Name IN ('Hemoglobin (Hb)', 'Total RBC count', 'Total WBC count', 'ESR', 'Eosinophils');

                INSERT OR IGNORE INTO PanelTestTypes (PanelId, TypeId)
                SELECT p.PanelId, t.TypeId FROM TestPanels p, TestTypes t WHERE p.Name = 'KFT Panel' AND t.Name IN ('Urea (KFT)', 'Creatinine (KFT)', 'Uric Acid (KFT)');

                INSERT OR IGNORE INTO PanelTestTypes (PanelId, TypeId)
                SELECT p.PanelId, t.TypeId FROM TestPanels p, TestTypes t WHERE p.Name = 'LFT Panel' AND t.Name IN ('AST (SGOT)', 'ALT (SGPT)', 'Alkaline Phosphatase (LFT)', 'Bilirubin Total', 'Bilirubin Direct', 'Bilirubin Indirect');

                INSERT OR IGNORE INTO PanelTestTypes (PanelId, TypeId)
                SELECT p.PanelId, t.TypeId FROM TestPanels p, TestTypes t WHERE p.Name = 'Electrolyte Panel' AND t.Name IN ('Sodium', 'Potassium', 'Chloride');
            "),
            new Migration(6, "Convert legacy -999.0 sentinel values in Results", @"
                UPDATE Results SET Value = NULL WHERE Value = -999.0;
            "),
            new Migration(7, "Add Role, PinHash, FailedLoginAttempts, LockoutEnd to Staff", @"
                ALTER TABLE Staff ADD COLUMN Role TEXT DEFAULT 'Technician';
                ALTER TABLE Staff ADD COLUMN PinHash TEXT;
                ALTER TABLE Staff ADD COLUMN FailedLoginAttempts INTEGER DEFAULT 0;
                ALTER TABLE Staff ADD COLUMN LockoutEnd DATETIME;
            "),
            new Migration(8, "Add DepartmentId column to TestTypes", @"
                ALTER TABLE TestTypes ADD COLUMN DepartmentId INTEGER REFERENCES Departments(DepartmentId) ON DELETE SET NULL;
            "),
            new Migration(9, "Add DoctorId column to TestOrders", @"
                ALTER TABLE TestOrders ADD COLUMN DoctorId INTEGER REFERENCES Doctors(DoctorId) ON DELETE SET NULL;
            "),
            new Migration(10, "Test Catalog Cleanup", @"
                DELETE FROM TestTypes WHERE Name IN ('Packed Cell Volume (PCV)', 'Mean Corpuscular Volume (MCV)', 'Mean Corpuscular Hb (MCH)', 'Mean Corpuscular Hb Concn. (MCHC)', 'RDW', 'Neutrophils', 'Lymphocytes', 'Monocytes', 'Basophils', 'Platelet Count', 'Dengue Fever Antibody, IgG', 'Dengue Fever Antibody, IgM', 'Bicarbonate', 'Magnesium', 'HIV 1 Antibody Screening', 'HIV 2 Antibody Screening', 'Calcium, Total (KFT)', 'Phosphorus (KFT)', 'Alkaline Phosphatase (KFT)', 'Sodium (KFT)', 'Potassium (KFT)', 'Chloride (KFT)', 'Non-HDL Cholesterol', 'AST:ALT Ratio', 'GGTP', 'Total Protein (LFT)', 'Albumin (LFT)', 'A : G Ratio', 'Vitamin B12', 'Vitamin D3 (25-Hydroxy)', 'Serum Iron', 'Total Iron Binding Capacity (TIBC)', 'Transferrin Saturation', 'Rapid Malaria (HRP-2/pLDH)', 'PBS Malarial Parasite', 'Patient Prothrombin Time', 'INR');
                UPDATE TestTypes SET Name = 'Total Protein', GroupName = 'Biochemistry' WHERE Name = 'Total Protein (KFT)';
                UPDATE TestTypes SET Name = 'Albumin', GroupName = 'Biochemistry' WHERE Name = 'Albumin (KFT)';
                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType) VALUES ('HbA1c', '%', 4.0, 5.6, 1, 'BIOCHEMISTRY', 'HBA1C', 'HPLC', 'Normal: < 5.7%, Pre-diabetes: 5.7-6.4%, Diabetes: >= 6.5%', 1, 400.00, 'Blood', 0);
                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType) VALUES ('ASO Titer', 'IU/mL', 0.0, 200.0, 1, 'SEROLOGY', 'ASO Titer', 'Turbidimetry', 'Negative: < 200 IU/mL. Positive suggests recent streptococcal infection.', 1, 300.00, 'Blood', 0);
                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType) VALUES ('Urine Color', 'Color', NULL, NULL, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Visual', 'Normal: Yellow to Amber.', 1, 100.00, 'Urine', 0);
                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType) VALUES ('Urine Appearance', 'Appearance', NULL, NULL, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Visual', 'Normal: Clear.', 2, 100.00, 'Urine', 0);
                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType) VALUES ('Urine Reaction', 'pH', 5.0, 8.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'pH Strip', 'Normal: 5.0 - 8.0.', 3, 100.00, 'Urine', 0);
                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType) VALUES ('Urine Ketone Bodies', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Reagent Strip', '0 = Absent, 1 = Present.', 4, 100.00, 'Urine', 1);
                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType) VALUES ('Urine Urobilinogen', 'mg/dL', 0.0, 1.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Reagent Strip', 'Normal: 0.1 - 1.0 mg/dL.', 5, 100.00, 'Urine', 0);
                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType) VALUES ('Urine Nitrite', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Reagent Strip', '0 = Negative, 1 = Positive.', 6, 100.00, 'Urine', 1);
                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType) VALUES ('Urine Leukocyte Esterase', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Reagent Strip', '0 = Negative, 1 = Positive.', 7, 100.00, 'Urine', 1);
                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType) VALUES ('Urine Bile Salts', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Routine', 'Reagent Strip', '0 = Absent, 1 = Present.', 7, 100.00, 'Urine', 1);
                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType) VALUES ('Urine Bile Pigments', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Routine', 'Reagent Strip', '0 = Absent, 1 = Present.', 8, 100.00, 'Urine', 1);
                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType) VALUES ('Culture & Sensitivity', 'Report', NULL, NULL, 1, 'MICROBIOLOGY', 'Culture & Sensitivity', 'Culture', 'Organism identification and antibiotic sensitivity pattern.', 1, 500.00, 'Blood', 3);
                UPDATE TestPanels SET Description = 'Comprehensive assessment of total cholesterol, triglycerides, HDL, LDL, and VLDL.' WHERE Name = 'Lipid Profile Panel';
                UPDATE TestPanels SET Description = 'Complete Blood Count including Haemoglobin, RBC, WBC, ESR, and Eosinophils.' WHERE Name = 'CBC Panel';
                UPDATE TestPanels SET Description = 'Kidney Function Test including Blood Urea, Creatinine, and Uric Acid.' WHERE Name = 'KFT Panel';
                UPDATE TestPanels SET Description = 'Liver Function Test including SGOT, SGPT, ALP, and Bilirubin.' WHERE Name = 'LFT Panel';
                UPDATE TestPanels SET Description = 'Serum Electrolytes including Sodium, Potassium, and Chloride.' WHERE Name = 'Electrolyte Panel';
                UPDATE TestTypes SET SampleType = 'Blood' WHERE Category IN ('SEROLOGY', 'IMMUNOASSAY', 'ENDOCRINOLOGY', 'BIOCHEMISTRY');
            "),
            new Migration(11, "Add Patient Title and Detailed Age Columns", @"
                ALTER TABLE Patients ADD COLUMN Title TEXT;
                ALTER TABLE Patients ADD COLUMN AgeYears INTEGER;
                ALTER TABLE Patients ADD COLUMN AgeMonths INTEGER;
                ALTER TABLE Patients ADD COLUMN AgeDays INTEGER;

                -- Migrate existing patients' DOB to AgeYears using julianday to calculate age in years
                UPDATE Patients 
                SET AgeYears = CAST((julianday('now') - julianday(DateOfBirth)) / 365.25 AS INTEGER)
                WHERE DateOfBirth IS NOT NULL;
            "),
            new Migration(12, "Add Rapid Malaria Test type", @"
                INSERT OR IGNORE INTO Departments (Name) VALUES ('SEROLOGY');

                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType, DepartmentId)
                SELECT 'Rapid Malaria Test', '', NULL, NULL, 1, 'SEROLOGY', 'Rapid Malaria Test', 'Immunochromatography', 'Negative: pv-positive/pf-positive/pv pf- positive not detected.\nPositive: Plasmodium vivax (pv) or Plasmodium falciparum (pf) antigen detected.', 1, 200.00, 'Blood', 3, DepartmentId
                FROM Departments WHERE Name = 'SEROLOGY'
                AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Rapid Malaria Test');
            "),
            new Migration(13, "Add Widal Test panel and link test types", @"
                INSERT OR IGNORE INTO TestPanels (Name, Description, Price) VALUES
                ('Widal Test', 'Widal test package containing all Widal-related agglutination tests.', 400.00);

                INSERT OR IGNORE INTO PanelTestTypes (PanelId, TypeId)
                SELECT p.PanelId, t.TypeId FROM TestPanels p, TestTypes t 
                WHERE p.Name = 'Widal Test' 
                AND t.Name IN ('S. Typhi O Agglutination', 'S. Typhi H Agglutination', 'S. Paratyphi A(H) Agglutination', 'S. Paratyphi B(H) Agglutination');
            ")
        };

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

            ApplyMigrations(db);

            var staffCount = db.Database.SqlQuery<int>("SELECT COUNT(*) FROM Staff").FirstOrDefault();
            if (staffCount == 0)
            {
                db.Database.ExecuteSqlCommand("INSERT INTO Staff (FullName, PinHash) VALUES ('Lab Technician', NULL);");
                Log.Information("Default staff record seeded without PIN.");
            }
            else
            {
                Log.Information("Staff records exist; skipping PIN reset.");
            }
        }

        private static void InitializeSchema(LabDbContext db)
        {
            var scriptPath = FileUtilities.FindFileUpwards("LabSystem.Data", "Migrations", "V1__init.sql");
            if (scriptPath != null && File.Exists(scriptPath))
            {
                var sql = File.ReadAllText(scriptPath);
                db.Database.ExecuteSqlCommand(sql);
                Log.Information("Database schema initialized from: {Path}", scriptPath);
            }
            else
            {
                Log.Warning("Could not find V1__init.sql schema file.");
            }

            var seedPath = FileUtilities.FindFileUpwards("", "seed.sql");
            if (seedPath != null && File.Exists(seedPath))
            {
                var seedSql = File.ReadAllText(seedPath);
                db.Database.ExecuteSqlCommand(seedSql);
                Log.Information("Seed data applied from: {Path}", seedPath);
            }
        }

        private static void ApplyMigrations(LabDbContext db)
        {
            try
            {
                db.Database.ExecuteSqlCommand(@"
                    CREATE TABLE IF NOT EXISTS SchemaVersion (
                        Version INTEGER PRIMARY KEY,
                        AppliedAt DATETIME NOT NULL
                    );
                ");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to create SchemaVersion tracking table.");
                return;
            }

            int currentVersion = 0;
            try
            {
                var result = db.Database.SqlQuery<int?>("SELECT MAX(Version) FROM SchemaVersion").FirstOrDefault();
                currentVersion = result.GetValueOrDefault();
            }
            catch
            {
                // SchemaVersion table exists but query failed — treat as version 0
            }

            // If no SchemaVersion record exists but tables already exist (pre-migration DB), mark as current
            if (currentVersion == 0)
            {
                try
                {
                    db.Database.ExecuteSqlCommand("INSERT OR IGNORE INTO SchemaVersion (Version, AppliedAt) VALUES (1, CURRENT_TIMESTAMP);");
                    currentVersion = 1;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to record initial SchemaVersion.");
                }
            }

            foreach (var migration in Migrations)
            {
                if (migration.Version <= currentVersion)
                    continue;

                try
                {
                    db.Database.ExecuteSqlCommand(migration.Sql);
                    db.Database.ExecuteSqlCommand(string.Format(
                        "INSERT INTO SchemaVersion (Version, AppliedAt) VALUES ({0}, CURRENT_TIMESTAMP);",
                        migration.Version));
                    Log.Information("Applied migration v{0}: {1}", migration.Version, migration.Description);
                }
                catch (Exception ex)
                {
                    string msg = ex.Message ?? "";
                    if (msg.IndexOf("duplicate column name", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        msg.IndexOf("already exists", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        try
                        {
                            db.Database.ExecuteSqlCommand(string.Format(
                                "INSERT OR IGNORE INTO SchemaVersion (Version, AppliedAt) VALUES ({0}, CURRENT_TIMESTAMP);",
                                migration.Version));
                            Log.Information("Migration v{0} ({1}) skipped (schema already up-to-date) and marked as applied.", migration.Version, migration.Description);
                        }
                        catch (Exception innerEx)
                        {
                            Log.Warning(innerEx, "Failed to record SchemaVersion for pre-applied migration v{0}.", migration.Version);
                        }
                    }
                    else
                    {
                        Log.Warning(ex, "Migration v{0} ({1}) encountered errors.", migration.Version, migration.Description);
                    }
                }
            }
        }

        private class Migration
        {
            public int Version { get; private set; }
            public string Description { get; private set; }
            public string Sql { get; private set; }

            public Migration(int version, string description, string sql)
            {
                Version = version;
                Description = description;
                Sql = sql;
            }
        }
    }
}
