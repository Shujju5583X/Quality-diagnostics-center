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
            "),
            new Migration(14, "Convert Widal tests to dropdown inputs and set ranges", @"
                UPDATE TestTypes SET InputType = 3, ReferenceRangeHigh = 40.0 WHERE Name IN ('S. Typhi O Agglutination', 'S. Typhi H Agglutination');
                UPDATE TestTypes SET InputType = 3, ReferenceRangeHigh = 20.0 WHERE Name IN ('S. Paratyphi A(H) Agglutination', 'S. Paratyphi B(H) Agglutination');
            "),
            new Migration(15, "Add missing CBC and Urine tests from sample reports", @"
                -- CBC tests
                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, DepartmentId)
                SELECT 'Packed Cell Volume (PCV)', '%', 40.0, 50.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Measures percentage of red blood cells in blood.', 5, 50.00, 'Blood', DepartmentId FROM Departments WHERE Name = 'HEMATOLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Packed Cell Volume (PCV)');
                
                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, DepartmentId)
                SELECT 'Mean Corpuscular Volume (MCV)', 'fL', 83.0, 101.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Measures average size of red blood cells.', 6, 50.00, 'Blood', DepartmentId FROM Departments WHERE Name = 'HEMATOLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Mean Corpuscular Volume (MCV)');

                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, DepartmentId)
                SELECT 'MCH', 'pg', 27.0, 32.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Mean Corpuscular Hemoglobin - average hemoglobin per red cell.', 7, 50.00, 'Blood', DepartmentId FROM Departments WHERE Name = 'HEMATOLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'MCH');

                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, DepartmentId)
                SELECT 'MCHC', 'g/dL', 32.5, 34.5, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Mean Corpuscular Hemoglobin Concentration.', 8, 50.00, 'Blood', DepartmentId FROM Departments WHERE Name = 'HEMATOLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'MCHC');

                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, DepartmentId)
                SELECT 'RDW', '%', 11.6, 14.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Red cell distribution width; elevated in mixed anemias.', 9, 50.00, 'Blood', DepartmentId FROM Departments WHERE Name = 'HEMATOLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'RDW');

                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, DepartmentId)
                SELECT 'Neutrophils', '%', 50.0, 62.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Differential WBC count - Neutrophils.', 10, 50.00, 'Blood', DepartmentId FROM Departments WHERE Name = 'HEMATOLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Neutrophils');

                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, DepartmentId)
                SELECT 'Lymphocytes', '%', 20.0, 40.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Differential WBC count - Lymphocytes.', 11, 50.00, 'Blood', DepartmentId FROM Departments WHERE Name = 'HEMATOLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Lymphocytes');

                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, DepartmentId)
                SELECT 'Monocytes', '%', 0.0, 10.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Differential WBC count - Monocytes.', 12, 50.00, 'Blood', DepartmentId FROM Departments WHERE Name = 'HEMATOLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Monocytes');

                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, DepartmentId)
                SELECT 'Basophils', '%', 0.0, 2.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Differential WBC count - Basophils.', 13, 50.00, 'Blood', DepartmentId FROM Departments WHERE Name = 'HEMATOLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Basophils');

                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, DepartmentId)
                SELECT 'Platelet Count', 'cumm', 150000.0, 410000.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Borderline: 150000 - 410000 cumm. Low platelets may indicate thrombocytopenia.', 14, 100.00, 'Blood', DepartmentId FROM Departments WHERE Name = 'HEMATOLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Platelet Count');

                -- Urine tests
                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType, DepartmentId)
                SELECT 'Urine Volume', 'mL', NULL, NULL, 1, 'CLINICAL PATHOLOGY', 'Urine Routine', 'Visual', 'Volume of urine collected for examination.', 9, 50.00, 'Urine', 0, DepartmentId FROM Departments WHERE Name = 'CLINICAL PATHOLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Urine Volume');

                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType, DepartmentId)
                SELECT 'Urine Blood', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Routine', 'Reagent Strip', '0 = Absent, 1 = Present.', 10, 50.00, 'Urine', 1, DepartmentId FROM Departments WHERE Name = 'CLINICAL PATHOLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Urine Blood');

                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType, DepartmentId)
                SELECT 'Urine RBC', 'cells/HPF', 0.0, 2.0, 1, 'CLINICAL PATHOLOGY', 'Urine Routine', 'Microscopy', 'Normal: 0-2 RBCs per HPF. Higher counts suggest hematuria.', 11, 50.00, 'Urine', 0, DepartmentId FROM Departments WHERE Name = 'CLINICAL PATHOLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Urine RBC');

                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType, DepartmentId)
                SELECT 'Urine Casts', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Routine', 'Microscopy', '0 = Absent, 1 = Present. Presence may indicate renal disease.', 12, 50.00, 'Urine', 1, DepartmentId FROM Departments WHERE Name = 'CLINICAL PATHOLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Urine Casts');

                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType, DepartmentId)
                SELECT 'Urine Crystals', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Routine', 'Microscopy', '0 = Nil, 1 = Present.', 13, 50.00, 'Urine', 1, DepartmentId FROM Departments WHERE Name = 'CLINICAL PATHOLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Urine Crystals');

                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType, DepartmentId)
                SELECT 'Urine Others', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Routine', 'Microscopy', 'Other microscopic findings.', 14, 50.00, 'Urine', 1, DepartmentId FROM Departments WHERE Name = 'CLINICAL PATHOLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Urine Others');

                -- Link to CBC Panel
                INSERT OR IGNORE INTO PanelTestTypes (PanelId, TypeId)
                SELECT p.PanelId, t.TypeId FROM TestPanels p, TestTypes t 
                WHERE p.Name = 'CBC Panel' 
                AND t.Name IN ('Packed Cell Volume (PCV)', 'Mean Corpuscular Volume (MCV)', 'MCH', 'MCHC', 'RDW', 'Neutrophils', 'Lymphocytes', 'Monocytes', 'Basophils', 'Platelet Count');
            " ),
            new Migration(16, "Add Complete Urine Examination Panel", @"
                INSERT OR IGNORE INTO TestPanels (Name, Description, Price) VALUES
                ('Complete Urine Examination Panel', 'Comprehensive physical, chemical, and microscopic examination of urine.', 300.00);

                INSERT OR IGNORE INTO PanelTestTypes (PanelId, TypeId)
                SELECT p.PanelId, t.TypeId FROM TestPanels p, TestTypes t 
                WHERE p.Name = 'Complete Urine Examination Panel' 
                AND t.Name IN ('Urine Color', 'Urine Appearance', 'Urine Volume', 'Urine Protein', 'Urine Sugar', 'Urine Ketone Bodies', 'Urine Bile Salts', 'Urine Bile Pigments', 'Urine Reaction', 'Urine Specific Gravity', 'Urine Blood', 'Urine Leukocyte Esterase', 'Urine Pus Cells', 'Urine Epithelial Cells', 'Urine RBC', 'Urine Casts', 'Urine Crystals', 'Urine Others');
            "),
            new Migration(17, "Add Dengue Profile Panel and tests", @"
                INSERT OR IGNORE INTO TestTypes (Name, Unit, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType, DepartmentId) 
                SELECT 'Dengue NS1 Antigen', '', 1, 'SEROLOGY', 'Dengue Serology', 'Immunochromatography', 'Reactive indicates presence of Dengue NS1 Antigen.', 1, 300.00, 'Blood', 3, DepartmentId FROM Departments WHERE Name = 'SEROLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Dengue NS1 Antigen');

                INSERT OR IGNORE INTO TestTypes (Name, Unit, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType, DepartmentId) 
                SELECT 'Dengue IgG Antibody', '', 1, 'SEROLOGY', 'Dengue Serology', 'Immunochromatography', 'Reactive indicates past Dengue infection.', 2, 250.00, 'Blood', 3, DepartmentId FROM Departments WHERE Name = 'SEROLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Dengue IgG Antibody');

                INSERT OR IGNORE INTO TestTypes (Name, Unit, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType, DepartmentId) 
                SELECT 'Dengue IgM Antibody', '', 1, 'SEROLOGY', 'Dengue Serology', 'Immunochromatography', 'Reactive indicates recent or acute Dengue infection.', 3, 250.00, 'Blood', 3, DepartmentId FROM Departments WHERE Name = 'SEROLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Dengue IgM Antibody');

                INSERT OR IGNORE INTO TestPanels (Name, Description, Price) VALUES
                ('Dengue Profile Panel', 'Comprehensive screening for Dengue including NS1 Antigen, IgG and IgM antibodies.', 700.00);

                INSERT OR IGNORE INTO PanelTestTypes (PanelId, TypeId)
                SELECT p.PanelId, t.TypeId FROM TestPanels p, TestTypes t 
                WHERE p.Name = 'Dengue Profile Panel' 
                AND t.Name IN ('Dengue NS1 Antigen', 'Dengue IgG Antibody', 'Dengue IgM Antibody');
            "),
            new Migration(18, "Add missing tests from gap analysis", @"
                INSERT OR IGNORE INTO TestTypes (Name, Unit, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType, DepartmentId) 
                SELECT 'HIV 1 Antibody Screening', 'Index', 1, 'SEROLOGY', 'HIV Screening', 'Immunochromatography', 'Negative: < 1.0 (Non-reactive).', 1, 350.00, 'Blood', 3, DepartmentId FROM Departments WHERE Name = 'SEROLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'HIV 1 Antibody Screening');

                INSERT OR IGNORE INTO TestTypes (Name, Unit, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType, DepartmentId) 
                SELECT 'HIV 2 Antibody Screening', 'Index', 1, 'SEROLOGY', 'HIV Screening', 'Immunochromatography', 'Negative: < 1.0 (Non-reactive).', 2, 350.00, 'Blood', 3, DepartmentId FROM Departments WHERE Name = 'SEROLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'HIV 2 Antibody Screening');

                INSERT OR IGNORE INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, SortOrder, Price, SampleType, InputType, DepartmentId) 
                SELECT 'Patient Prothrombin Time', 'seconds', 11.0, 16.0, 1, 'HEMATOLOGY', 'PT / INR', 'Coagulometric', 1, 300.00, 'Blood', 0, DepartmentId FROM Departments WHERE Name = 'HEMATOLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Patient Prothrombin Time');

                INSERT OR IGNORE INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, SortOrder, Price, SampleType, InputType, DepartmentId) 
                SELECT 'Control Prothrombin Time', 'seconds', 12.0, 16.0, 1, 'HEMATOLOGY', 'PT / INR', 'Coagulometric', 2, 0.00, 'Blood', 0, DepartmentId FROM Departments WHERE Name = 'HEMATOLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Control Prothrombin Time');

                INSERT OR IGNORE INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, SortOrder, Price, SampleType, InputType, DepartmentId) 
                SELECT 'INR', 'Ratio', 0.8, 1.2, 1, 'HEMATOLOGY', 'PT / INR', 'Calculated', 3, 0.00, 'Blood', 0, DepartmentId FROM Departments WHERE Name = 'HEMATOLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'INR');

                INSERT OR IGNORE INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, SortOrder, Price, SampleType, InputType, DepartmentId) 
                SELECT 'Serum Iron', 'µg/dL', 60.0, 170.0, 1, 'BIOCHEMISTRY', 'Iron Studies', 'Ferrozine', 1, 200.00, 'Blood', 0, DepartmentId FROM Departments WHERE Name = 'BIOCHEMISTRY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Serum Iron');

                INSERT OR IGNORE INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, SortOrder, Price, SampleType, InputType, DepartmentId) 
                SELECT 'Total Iron Binding Capacity (TIBC)', 'µg/dL', 250.0, 370.0, 1, 'BIOCHEMISTRY', 'Iron Studies', 'Ferrozine', 2, 200.00, 'Blood', 0, DepartmentId FROM Departments WHERE Name = 'BIOCHEMISTRY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Total Iron Binding Capacity (TIBC)');

                INSERT OR IGNORE INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, SortOrder, Price, SampleType, InputType, DepartmentId) 
                SELECT 'Transferrin Saturation', '%', 20.0, 50.0, 1, 'BIOCHEMISTRY', 'Iron Studies', 'Calculated', 3, 0.00, 'Blood', 0, DepartmentId FROM Departments WHERE Name = 'BIOCHEMISTRY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Transferrin Saturation');

                INSERT OR IGNORE INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, SortOrder, Price, SampleType, InputType, DepartmentId) 
                SELECT 'Vitamin B12', 'pg/mL', 211.0, 911.0, 1, 'BIOCHEMISTRY', 'Vitamin B12', 'CLIA', 1, 800.00, 'Blood', 0, DepartmentId FROM Departments WHERE Name = 'BIOCHEMISTRY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Vitamin B12');

                INSERT OR IGNORE INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType, DepartmentId) 
                SELECT 'Vitamin D3 (25-Hydroxy)', 'ng/mL', 30.0, 100.0, 1, 'BIOCHEMISTRY', 'Vitamin D', 'CLIA', 'Deficient: <20, Insufficient: 20-29, Sufficient: 30-100, Toxic: >100', 1, 800.00, 'Blood', 0, DepartmentId FROM Departments WHERE Name = 'BIOCHEMISTRY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Vitamin D3 (25-Hydroxy)');

                INSERT OR IGNORE INTO TestTypes (Name, Unit, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType, DepartmentId) 
                SELECT 'Hemoglobin Solubility Test (HBSG)', 'Result', 1, 'HEMATOLOGY', 'Hemoglobin Solubility Test', 'Dithionite tube test', 'Negative: Normal. Positive: Suggests Hb S (sickle hemoglobin).', 1, 200.00, 'Blood', 3, DepartmentId FROM Departments WHERE Name = 'HEMATOLOGY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Hemoglobin Solubility Test (HBSG)');

                INSERT OR IGNORE INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, SortOrder, Price, SampleType, InputType, DepartmentId) 
                SELECT 'GGTP', 'U/L', 0.0, 55.0, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'IFCC', 3, 150.00, 'Blood', 0, DepartmentId FROM Departments WHERE Name = 'BIOCHEMISTRY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'GGTP');

                INSERT OR IGNORE INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, SortOrder, Price, SampleType, InputType, DepartmentId) 
                SELECT 'Total Protein (LFT)', 'g/dL', 6.0, 8.3, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'Biuret', 4, 150.00, 'Blood', 0, DepartmentId FROM Departments WHERE Name = 'BIOCHEMISTRY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Total Protein (LFT)');

                INSERT OR IGNORE INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, SortOrder, Price, SampleType, InputType, DepartmentId) 
                SELECT 'Albumin (LFT)', 'g/dL', 3.5, 5.0, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'BCG', 9, 150.00, 'Blood', 0, DepartmentId FROM Departments WHERE Name = 'BIOCHEMISTRY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Albumin (LFT)');

                INSERT OR IGNORE INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, SortOrder, Price, SampleType, InputType, DepartmentId) 
                SELECT 'A : G Ratio', 'Ratio', 1.1, 2.5, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'Calculated', 10, 0.00, 'Blood', 0, DepartmentId FROM Departments WHERE Name = 'BIOCHEMISTRY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'A : G Ratio');

                INSERT OR IGNORE INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, SortOrder, Price, SampleType, InputType, DepartmentId) 
                SELECT 'Blood Urea Nitrogen (BUN)', 'mg/dL', 7.0, 20.0, 1, 'BIOCHEMISTRY', 'Kidney Function Test (KFT)', 'Calculated (Urea / 2.14)', 4, 0.00, 'Blood', 0, DepartmentId FROM Departments WHERE Name = 'BIOCHEMISTRY' AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Blood Urea Nitrogen (BUN)');

                INSERT OR IGNORE INTO TestPanels (Name, Description, Price) VALUES
                ('PT INR Panel', 'Prothrombin Time and INR.', 400.00),
                ('Iron Profile Panel', 'Comprehensive assessment of Iron, TIBC, and Transferrin Saturation.', 500.00),
                ('TSB Panel', 'Total Serum Bilirubin including Total, Direct, and Indirect Bilirubin.', 300.00);

                INSERT OR IGNORE INTO PanelTestTypes (PanelId, TypeId)
                SELECT p.PanelId, t.TypeId FROM TestPanels p, TestTypes t 
                WHERE p.Name = 'PT INR Panel' AND t.Name IN ('Patient Prothrombin Time', 'Control Prothrombin Time', 'INR');

                INSERT OR IGNORE INTO PanelTestTypes (PanelId, TypeId)
                SELECT p.PanelId, t.TypeId FROM TestPanels p, TestTypes t 
                WHERE p.Name = 'Iron Profile Panel' AND t.Name IN ('Serum Iron', 'Total Iron Binding Capacity (TIBC)', 'Transferrin Saturation');

                INSERT OR IGNORE INTO PanelTestTypes (PanelId, TypeId)
                SELECT p.PanelId, t.TypeId FROM TestPanels p, TestTypes t 
                WHERE p.Name = 'TSB Panel' AND t.Name IN ('Bilirubin Total', 'Bilirubin Direct', 'Bilirubin Indirect');

                INSERT OR IGNORE INTO PanelTestTypes (PanelId, TypeId)
                SELECT p.PanelId, t.TypeId FROM TestPanels p, TestTypes t 
                WHERE p.Name = 'LFT Panel' AND t.Name IN ('GGTP', 'Total Protein (LFT)', 'Albumin (LFT)', 'A : G Ratio');

                INSERT OR IGNORE INTO PanelTestTypes (PanelId, TypeId)
                SELECT p.PanelId, t.TypeId FROM TestPanels p, TestTypes t 
                WHERE p.Name = 'KFT Panel' AND t.Name IN ('Blood Urea Nitrogen (BUN)');
            "),
            new Migration(19, "Add Instrument column to TestTypes and seed CBC instrument", @"
                ALTER TABLE TestTypes ADD COLUMN Instrument TEXT;
                UPDATE TestTypes SET Instrument = 'Fully automated cell counter ERBA H-360' WHERE Category = 'HEMATOLOGY' AND GroupName = 'Complete Blood Count (CBC)';
            "),
            new Migration(20, "Update CBC instrument to ERBA H-360", @"
                UPDATE TestTypes SET Instrument = 'Fully automated cell counter ERBA H-360' WHERE Category = 'HEMATOLOGY' AND GroupName = 'Complete Blood Count (CBC)' AND Instrument = 'Fully automated cell counter - Mindray 300';
            "),
            new Migration(21, "Add ref ranges config fields to TestTypes", @"
                ALTER TABLE TestTypes ADD COLUMN HasBesideRefRanges BOOLEAN DEFAULT 0;
                ALTER TABLE TestTypes ADD COLUMN HasTextRefRanges BOOLEAN DEFAULT 0;
                ALTER TABLE TestTypes ADD COLUMN TextReferenceString TEXT;
                ALTER TABLE TestTypes ADD COLUMN TextReferenceNormalValue TEXT;
            " ),
            new Migration(22, "Add ABO Grouping and Rh Typing", @"
                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType, DepartmentId)
                SELECT 'BLOOD GROUP', '', NULL, NULL, 1, 'SEROLOGY', 'ABO GROUPING & RH TYPING', 'Agglutination', '', 1, 150.00, 'Blood', 2, DepartmentId
                FROM Departments WHERE Name = 'SEROLOGY'
                AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'BLOOD GROUP');

                INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType, DepartmentId)
                SELECT 'Rh TYPING', '', NULL, NULL, 1, 'SEROLOGY', 'ABO GROUPING & RH TYPING', 'Agglutination', '', 2, 100.00, 'Blood', 2, DepartmentId
                FROM Departments WHERE Name = 'SEROLOGY'
                AND NOT EXISTS (SELECT 1 FROM TestTypes WHERE Name = 'Rh TYPING');
            "),
            new Migration(23, "Fix InputType for ABO Grouping and Rh Typing", @"
                UPDATE TestTypes SET InputType = 3 WHERE Name IN ('BLOOD GROUP', 'Rh TYPING');
            "),
            new Migration(24, "Add ABO Grouping and Rh Typing Panel", @"
                INSERT INTO TestPanels (Name, Description, Price)
                SELECT 'ABO GROUPING & RH TYPING', 'ABO Grouping and Rh Typing Panel', 250.00
                WHERE NOT EXISTS (SELECT 1 FROM TestPanels WHERE Name = 'ABO GROUPING & RH TYPING');

                INSERT OR IGNORE INTO PanelTestTypes (PanelId, TypeId)
                SELECT p.PanelId, t.TypeId FROM TestPanels p, TestTypes t
                WHERE p.Name = 'ABO GROUPING & RH TYPING' AND t.Name IN ('BLOOD GROUP', 'Rh TYPING');
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

        private static string LoadEmbeddedResource(string filename)
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetEntryAssembly() ?? System.Reflection.Assembly.GetExecutingAssembly();
                var resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(r => r.EndsWith(filename, System.StringComparison.OrdinalIgnoreCase));

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
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to load embedded resource: {Filename}", filename);
            }
            return null;
        }

        private static void InitializeSchema(LabDbContext db)
        {
            var scriptPath = FileUtilities.FindFileUpwards("LabSystem.Data", "Migrations", "V1__init.sql");
            string sql = null;
            if (scriptPath != null && File.Exists(scriptPath))
            {
                sql = File.ReadAllText(scriptPath);
                Log.Information("Database schema loaded from disk: {Path}", scriptPath);
            }
            else
            {
                Log.Information("V1__init.sql not found on disk; attempting to load from embedded resources...");
                sql = LoadEmbeddedResource("V1__init.sql");
            }

            if (!string.IsNullOrEmpty(sql))
            {
                db.Database.ExecuteSqlCommand(sql);
                Log.Information("Database schema initialized successfully.");
            }
            else
            {
                Log.Warning("Could not find V1__init.sql schema file on disk or in embedded resources.");
            }

            var seedPath = FileUtilities.FindFileUpwards("", "seed.sql");
            string seedSql = null;
            if (seedPath != null && File.Exists(seedPath))
            {
                seedSql = File.ReadAllText(seedPath);
                Log.Information("Seed data loaded from disk: {Path}", seedPath);
            }
            else
            {
                Log.Information("seed.sql not found on disk; attempting to load from embedded resources...");
                seedSql = LoadEmbeddedResource("seed.sql");
            }

            if (!string.IsNullOrEmpty(seedSql))
            {
                db.Database.ExecuteSqlCommand(seedSql);
                Log.Information("Seed data applied successfully.");
            }
            else
            {
                Log.Warning("Could not find seed.sql file on disk or in embedded resources.");
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
