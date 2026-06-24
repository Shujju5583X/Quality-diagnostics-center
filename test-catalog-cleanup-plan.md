# Test Catalog Cleanup Plan

## Objective
Trim the test catalog to contain ONLY the user-specified tests. Delete all others, add missing ones, and clean up related code.

---

## Current State
- **84 individual test parameters** across 23 groups
- **6 test panels** (bundles)
- Tests defined in `seed.sql` (lines 13-163), prices (lines 165-276), sample types (lines 278-282)

---

## STEP 1: DELETE Unwanted Tests from `seed.sql`

### 1A. Remove these INSERT blocks entirely:

| Test Name | Group | seed.sql Lines |
|-----------|-------|----------------|
| Packed Cell Volume (PCV) | CBC | 16 |
| Mean Corpuscular Volume (MCV) | CBC | 17 |
| Mean Corpuscular Hb (MCH) | CBC | 18 |
| Mean Corpuscular Hb Concn. (MCHC) | CBC | 19 |
| RDW | CBC | 20 |
| Neutrophils | CBC | 22 |
| Lymphocytes | CBC | 23 |
| Monocytes | CBC | 25 |
| Basophils | CBC | 26 |
| Platelet Count | CBC | 27 |
| Dengue Fever Antibody, IgG | Dengue Panel | 39 |
| Dengue Fever Antibody, IgM | Dengue Panel | 40 |
| Bicarbonate | Electrolytes | 47 |
| Magnesium | Electrolytes | 49 |
| HIV 1 Antibody Screening | HIV | 59 |
| HIV 2 Antibody Screening | HIV | 60 |
| Calcium, Total (KFT) | KFT | 67 |
| Phosphorus (KFT) | KFT | 68 |
| Alkaline Phosphatase (KFT) | KFT | 69 |
| Sodium (KFT) | KFT | 72 |
| Potassium (KFT) | KFT | 73 |
| Chloride (KFT) | KFT | 74 |
| Non-HDL Cholesterol | Lipid Profile | 83 |
| AST:ALT Ratio | LFT | 89 |
| GGTP | LFT | 90 |
| Total Protein (LFT) | LFT | 95 |
| Albumin (LFT) | LFT | 96 |
| A : G Ratio | LFT | 97 |
| Vitamin B12 | Vitamin B12 | 116 |
| Vitamin D3 (25-Hydroxy) | Vitamin D3 | 120 |
| Serum Iron | Iron Deficiency | 124 |
| Total Iron Binding Capacity (TIBC) | Iron Deficiency | 125 |
| Transferrin Saturation | Iron Deficiency | 126 |
| Rapid Malaria (HRP-2/pLDH) | Malaria | 142 |
| PBS Malarial Parasite | Malaria | 143 |
| Patient Prothrombin Time | PT-INR | 162 |
| INR | PT-INR | 163 |

### 1B. Remove corresponding UPDATE Price lines:
Lines: 169-173, 175-180, 187-188, 194, 196, 204-205, 211-213, 216-218, 226, 231-232, 237-239, 255-256, 259-261, 267-268, 275-276

### 1C. Remove corresponding ReferenceRange lines:
Line 312 (Platelet Count)

### 1D. Remove corresponding PanelTestTypes lines:
Lines: 369 (Non-HDL), 378-390 (CBC Panel removed tests), 396 (Calcium KFT), 405-406 (LFT Panel removed tests), 412 (Bicarbonate)

---

## STEP 2: RENAME Duplicate Tests in `seed.sql`

| Current Name | New Name | Lines to Change |
|-------------|----------|-----------------|
| Total Protein (KFT) | Total Protein | 70 (INSERT), 214 (Price UPDATE) |
| Albumin (KFT) | Albumin | 71 (INSERT), 215 (Price UPDATE) |

Also update:
- Category: `BIOCHEMISTRY` (keep same)
- GroupName: Change from `Kidney Function Test (KFT)` to `Biochemistry`

---

## STEP 3: ADD Missing Tests to `seed.sql`

### 3A. HbA1c
```sql
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('HbA1c', '%', 4.0, 5.6, 1, 'BIOCHEMISTRY', 'HBA1C', 'HPLC', 'Normal: < 5.7%, Pre-diabetes: 5.7-6.4%, Diabetes: >= 6.5%', 1);
UPDATE TestTypes SET Price = 400.00, SampleType = 'Blood' WHERE Name = 'HbA1c';
3B. ASO Titer
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('ASO Titer', 'IU/mL', 0.0, 200.0, 1, 'SEROLOGY', 'ASO Titer', 'Turbidimetry', 'Negative: < 200 IU/mL. Positive suggests recent streptococcal infection.', 1);
UPDATE TestTypes SET Price = 300.00, SampleType = 'Blood' WHERE Name = 'ASO Titer';
3C. Urine Complete tests (new group)
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('Urine Color', 'Color', NULL, NULL, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Visual', 'Normal: Yellow to Amber.', 1),
('Urine Appearance', 'Appearance', NULL, NULL, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Visual', 'Normal: Clear.', 2),
('Urine Reaction', 'pH', 5.0, 8.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'pH Strip', 'Normal: 5.0 - 8.0.', 3),
('Urine Ketone Bodies', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Reagent Strip', '0 = Absent, 1 = Present.', 4),
('Urine Urobilinogen', 'mg/dL', 0.0, 1.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Reagent Strip', 'Normal: 0.1 - 1.0 mg/dL.', 5),
('Urine Nitrite', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Reagent Strip', '0 = Negative, 1 = Positive.', 6),
('Urine Leukocyte Esterase', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Reagent Strip', '0 = Negative, 1 = Positive.', 7);
UPDATE TestTypes SET Price = 100.00, SampleType = 'Urine' WHERE GroupName = 'Urine Complete';
UPDATE TestTypes SET InputType = 1 WHERE GroupName = 'Urine Complete' AND Unit = 'Qualitative';
3D. Bile Salts and Bile Pigments (add to Urine Routine)
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('Urine Bile Salts', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Routine', 'Reagent Strip', '0 = Absent, 1 = Present.', 7),
('Urine Bile Pigments', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Routine', 'Reagent Strip', '0 = Absent, 1 = Present.', 8);
UPDATE TestTypes SET Price = 100.00, SampleType = 'Urine' WHERE Name IN ('Urine Bile Salts', 'Urine Bile Pigments');
UPDATE TestTypes SET InputType = 1 WHERE Name IN ('Urine Bile Salts', 'Urine Bile Pigments');
3E. Culture & Sensitivity (C/S)
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('Culture & Sensitivity', 'Report', NULL, NULL, 1, 'MICROBIOLOGY', 'Culture & Sensitivity', 'Culture', 'Organism identification and antibiotic sensitivity pattern.', 1);
UPDATE TestTypes SET Price = 500.00, SampleType = 'Blood' WHERE Name = 'Culture & Sensitivity';
UPDATE TestTypes SET InputType = 3 WHERE Name = 'Culture & Sensitivity';
STEP 4: Update SampleType in seed.sql
Line 280 — change 'Serum' to 'Blood':
-- FROM:
UPDATE TestTypes SET SampleType = 'Serum' WHERE Category IN ('SEROLOGY', 'IMMUNOASSAY', 'ENDOCRINOLOGY', 'BIOCHEMISTRY');
-- TO:
UPDATE TestTypes SET SampleType = 'Blood' WHERE Category IN ('SEROLOGY', 'IMMUNOASSAY', 'ENDOCRINOLOGY', 'BIOCHEMISTRY');
STEP 5: Update Test Panels in seed.sql
5A. CBC Panel — trim to 6 tests
Update PanelTestTypes (lines 376-390) to only:
INSERT INTO PanelTestTypes (PanelId, TypeId) VALUES
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Hemoglobin (Hb)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Total RBC count')),
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Total WBC count')),
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'ESR')),
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Eosinophils'));
Update panel description (line 357):
('CBC Panel', 'Complete Blood Count including Haemoglobin, RBC, WBC, ESR, and Eosinophils.', 800.00),
5B. Electrolyte Panel — remove Bicarbonate
Update PanelTestTypes (lines 408-412) to:
INSERT INTO PanelTestTypes (PanelId, TypeId) VALUES
((SELECT PanelId FROM TestPanels WHERE Name = 'Electrolyte Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Sodium')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Electrolyte Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Potassium')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Electrolyte Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Chloride'));
Update panel description (line 360):
('Electrolyte Panel', 'Serum Electrolytes including Sodium, Potassium, and Chloride.', 400.00);
5C. KFT Panel — trim
Update PanelTestTypes (lines 392-396) to:
INSERT INTO PanelTestTypes (PanelId, TypeId) VALUES
((SELECT PanelId FROM TestPanels WHERE Name = 'KFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Urea (KFT)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'KFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Creatinine (KFT)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'KFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Uric Acid (KFT)'));
Update panel description (line 358):
('KFT Panel', 'Kidney Function Test including Blood Urea, Creatinine, and Uric Acid.', 600.00),
5D. LFT Panel — trim
Update PanelTestTypes (lines 398-406) to:
INSERT INTO PanelTestTypes (PanelId, TypeId) VALUES
((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'AST (SGOT)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'ALT (SGPT)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Alkaline Phosphatase (LFT)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Bilirubin Total')),
((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Bilirubin Direct')),
((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Bilirubin Indirect'));
Update panel description (line 359):
('LFT Panel', 'Liver Function Test including SGOT, SGPT, ALP, and Bilirubin.', 700.00),
5E. Lipid Profile Panel — remove Non-HDL
Update PanelTestTypes (lines 363-369) to:
INSERT INTO PanelTestTypes (PanelId, TypeId) VALUES
((SELECT PanelId FROM TestPanels WHERE Name = 'Lipid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Cholesterol, Total')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Lipid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Triglycerides')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Lipid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'HDL Cholesterol')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Lipid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'LDL Cholesterol')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Lipid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'VLDL Cholesterol'));
Update panel description (line 355):
('Lipid Profile Panel', 'Comprehensive assessment of total cholesterol, triglycerides, HDL, LDL, and VLDL.', 1200.00),
5F. Update InputType line
Line 417 — remove Malaria and HIV references:
-- FROM:
UPDATE TestTypes SET InputType = 3 WHERE Name IN ('Rapid Malaria (HRP-2/pLDH)', 'PBS Malarial Parasite', 'HBsAg Screening', 'Anti-HCV Antibody', 'VDRL Screening', 'HIV 1 Antibody Screening', 'HIV 2 Antibody Screening');
-- TO:
UPDATE TestTypes SET InputType = 3 WHERE Name IN ('HBsAg Screening', 'Anti-HCV Antibody', 'VDRL Screening');
STEP 6: Modify LabSystem.Data\DatabaseInitializer.cs
6A. Migration v2 (line 24) — change Serum to Blood
// FROM:
UPDATE TestTypes SET SampleType = 'Serum' WHERE (SampleType IS NULL OR SampleType = '') AND Category IN ('SEROLOGY', 'IMMUNOASSAY', 'ENDOCRINOLOGY', 'BIOCHEMISTRY');
// TO:
UPDATE TestTypes SET SampleType = 'Blood' WHERE (SampleType IS NULL OR SampleType = '') AND Category IN ('SEROLOGY', 'IMMUNOASSAY', 'ENDOCRINOLOGY', 'BIOCHEMISTRY');
6B. Migration v4 (lines 49-57) — update panel descriptions
Match the updated descriptions from Step 5.
6C. Migration v5 (lines 58-76) — update panel test links
Match the trimmed PanelTestTypes from Step 5.
STEP 7: Modify LabSystem.Services\PdfReportService.cs
7A. Remove Malaria display logic (lines 523-530)
Delete or comment out:
if (testType.Name.Contains("Malarial Parasite") || testType.Name.Contains("PBS Malarial"))
{
    return val >= 1.0 ? "Detected" : "Not Detected";
}
if (testType.Name.Contains("Rapid Malaria"))
{
    return val >= 1.0 ? "Positive" : "Negative";
}
STEP 8: Modify LabSystem.Core\Services\ReferenceRangeEvaluator.cs
8A. Line 111 — remove Malarial Parasite check
Delete:
if (tt.Name != null && tt.Name.Contains("Malarial Parasite"))
8B. Line 115 — remove Rapid Malaria from the check
Change from:
if (tt.Name != null && (tt.Name.Contains("Rapid Malaria") || tt.Name.Contains("HBsAg") || tt.Name.Contains("HCV") || tt.Name.Contains("VDRL") || tt.Name.Contains("HIV")))
To:
if (tt.Name != null && (tt.Name.Contains("HBsAg") || tt.Name.Contains("HCV") || tt.Name.Contains("VDRL") || tt.Name.Contains("HIV")))
STEP 9: Modify LabSystem.UI\ViewModels\DashboardViewModel.Results.cs
9A. Line 312 — remove Rapid Malaria from the check
Change from:
if (ri.TestName.Contains("Rapid Malaria") || ri.TestName.Contains("HBsAg") || ri.TestName.Contains("HCV") || ri.TestName.Contains("VDRL") || ri.TestName.Contains("HIV"))
To:
if (ri.TestName.Contains("HBsAg") || ri.TestName.Contains("HCV") || ri.TestName.Contains("VDRL") || ri.TestName.Contains("HIV"))
Summary of Changes
File	Action
seed.sql	Delete 37 tests, add ~10 new tests, rename 2, update 6 panels, change SampleType
DatabaseInitializer.cs	Update migrations v2, v4, v5
PdfReportService.cs	Remove Malaria display logic
ReferenceRangeEvaluator.cs	Remove Malaria checks
DashboardViewModel.Results.cs	Remove Malaria check
Final Test Count
- Before: 84 parameters
- Deleted: 37
- Added: ~10
- After: ~57 parameters

---