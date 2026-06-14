-- Seed Patients
INSERT INTO Patients (FullName, DateOfBirth, ContactPhone, ContactEmail, CreatedAt, Gender, Uhid) VALUES ('John Doe', '1980-05-15', '555-1234', 'john@example.com', '2026-06-08T00:00:00Z', 'Male', 'QDC-2026-00001');
INSERT INTO Patients (FullName, DateOfBirth, ContactPhone, ContactEmail, CreatedAt, Gender, Uhid) VALUES ('Jane Smith', '1992-11-20', '555-5678', 'jane@example.com', '2026-06-08T00:00:00Z', 'Female', 'QDC-2026-00002');
INSERT INTO Patients (FullName, DateOfBirth, ContactPhone, ContactEmail, CreatedAt, Gender, Uhid) VALUES ('Alice Johnson', '1975-02-10', '555-8765', 'alice@example.com', '2026-06-08T00:00:00Z', 'Female', 'QDC-2026-00003');
INSERT INTO Patients (FullName, DateOfBirth, ContactPhone, ContactEmail, CreatedAt, Gender, Uhid) VALUES ('Yash M. Patel', '2005-08-25', '0123456789', 'yash@example.com', '2026-06-09T00:00:00Z', 'Male', 'QDC-2026-00004');

-- Seed Staff (Single-operator mode - no authentication)
INSERT INTO Staff (FullName) VALUES ('Lab Technician');

-- Seed Test Catalog (TestTypes)

-- 1. Complete Blood Count (CBC)
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('Hemoglobin (Hb)', 'g/dL', 13.0, 17.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Further confirm for Anemia', 1),
('Total RBC count', 'mill/cumm', 4.5, 5.5, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Further confirm for Anemia', 2),
('Packed Cell Volume (PCV)', '%', 40.0, 50.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Further confirm for Anemia', 3),
('Mean Corpuscular Volume (MCV)', 'fL', 83.0, 101.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Further confirm for Anemia', 4),
('Mean Corpuscular Hb (MCH)', 'pg', 27.0, 32.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Further confirm for Anemia', 5),
('Mean Corpuscular Hb Concn. (MCHC)', 'g/dL', 32.5, 34.5, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Further confirm for Anemia', 6),
('RDW', '%', 11.6, 14.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Further confirm for Anemia', 7),
('Total WBC count', 'cumm', 4000.0, 11000.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Further confirm for Anemia', 8),
('Neutrophils', '%', 50.0, 62.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Further confirm for Anemia', 9),
('Lymphocytes', '%', 20.0, 40.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Further confirm for Anemia', 10),
('Eosinophils', '%', 0.0, 6.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Further confirm for Anemia', 11),
('Monocytes', '%', 0.0, 10.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Further confirm for Anemia', 12),
('Basophils', '%', 0.0, 2.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Further confirm for Anemia', 13),
('Platelet Count', 'cumm', 150000.0, 410000.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Further confirm for Anemia', 14);

-- 2. Complete Blood Count (CBC) with ESR (Adds ESR parameter to group)
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('ESR', 'mm/hr', 0.0, 15.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC) with ESR', 'Capillary photometry', 'Non-specific marker for inflammation', 15);

-- 3. C-Reactive Protein (CRP)
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('C-Reactive Protein', 'mg/dL', 0.0, 5.0, 1, 'BIOCHEMISTRY', 'C-Reactive Protein (CRP)', 'Turbidimetry', '1. Measurement of CRP is useful for the detection and evaluation of infection, tissue injury, and inflammatory disorders.\n2. Increased CRP suggests presence of inflammation.', 1);

-- 4. Dengue Fever Panel
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('Dengue Fever Antibody, IgG', 'Index', 0.0, 1.8, 1, 'SEROLOGY', 'Dengue Fever Panel', 'ELISA', 'Negative (< 1.80): No detectable IgG antibody.\nPositive (> 2.20): IgG antibody detected.', 1),
('Dengue Fever Antibody, IgM', 'Index', 0.0, 0.9, 1, 'SEROLOGY', 'Dengue Fever Panel', 'ELISA', 'Negative (< 0.90): No detectable IgM antibody.\nPositive (> 1.10): IgM antibody detected.', 2);

-- 5. Electrolytes
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('Sodium', 'mEq/L', 136.0, 145.0, 1, 'BIOCHEMISTRY', 'Electrolytes', 'Indirect ISE', 'Used for measuring hydration status and kidney function.', 1),
('Potassium', 'mEq/L', 3.5, 5.1, 1, 'BIOCHEMISTRY', 'Electrolytes', 'Indirect ISE', 'Measures critical blood potassium concentration.', 2),
('Chloride', 'mEq/L', 98.0, 107.0, 1, 'BIOCHEMISTRY', 'Electrolytes', 'Indirect ISE', 'Measures blood chloride level.', 3),
('Bicarbonate', 'mEq/L', 22.0, 28.0, 1, 'BIOCHEMISTRY', 'Electrolytes', 'Indirect ISE', 'Measures bicarbonate level.', 4),
('Calcium', 'mg/dL', 8.6, 10.2, 1, 'BIOCHEMISTRY', 'Electrolytes', 'Indirect ISE', 'Measures total calcium levels.', 5),
('Magnesium', 'mg/dL', 1.8, 2.3, 1, 'BIOCHEMISTRY', 'Electrolytes', 'Indirect ISE', 'Measures magnesium levels.', 6);

-- 6. Glucose fasting, PP, and Random
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('Glucose, Fasting (Plasma)', 'mg/dL', 70.0, 100.0, 1, 'BIOCHEMISTRY', 'Fasting Blood Sugar (FBS)', 'Hexokinase', 'Fasting glucose reference: 70 - 100 mg/dL.', 1),
('Glucose, Post Prandial (Plasma)', 'mg/dL', 90.0, 140.0, 1, 'BIOCHEMISTRY', 'Post Lunch Blood Sugar (PLBS)', 'Hexokinase', 'Post prandial reference: 90 - 140 mg/dL.', 2),
('Glucose, Random (Plasma)', 'mg/dL', 70.0, 150.0, 1, 'BIOCHEMISTRY', 'Random Blood Sugar (RBS)', 'Hexokinase', 'Random reference: 70 - 150 mg/dL.', 3);

-- 7. HIV 1 & 2 Antibodies
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('HIV 1 Antibody Screening', 'Index', 0.0, 1.0, 1, 'SEROLOGY', 'HIV 1 & 2 Antibodies Screening', 'CLIA / Tri-Dot', 'Positive indicates antibody detected. Negative indicates no antibody detected.', 1),
('HIV 2 Antibody Screening', 'Index', 0.0, 1.0, 1, 'SEROLOGY', 'HIV 1 & 2 Antibodies Screening', 'CLIA / Tri-Dot', 'Positive indicates antibody detected. Negative indicates no antibody detected.', 2);

-- 8. Kidney Function Test (KFT)
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('Urea (KFT)', 'mg/dL', 13.0, 43.0, 1, 'BIOCHEMISTRY', 'Kidney Function Test (KFT)', 'Urease UV', 'Assess kidney waste clearing.', 1),
('Creatinine (KFT)', 'mg/dL', 0.7, 1.3, 1, 'BIOCHEMISTRY', 'Kidney Function Test (KFT)', 'Modified Jaffe, Kinetic', 'Assess muscle waste clearance.', 2),
('Uric Acid (KFT)', 'mg/dL', 3.5, 7.2, 1, 'BIOCHEMISTRY', 'Kidney Function Test (KFT)', 'Uricase', 'Assess uric acid clearance.', 3),
('Calcium, Total (KFT)', 'mg/dL', 8.7, 10.4, 1, 'BIOCHEMISTRY', 'Kidney Function Test (KFT)', 'Arsenazo III', 'Total calcium monitoring.', 4),
('Phosphorus (KFT)', 'mg/dL', 2.4, 5.1, 1, 'BIOCHEMISTRY', 'Kidney Function Test (KFT)', 'Molybdate UV', 'Phosphorus monitoring.', 5),
('Alkaline Phosphatase (KFT)', 'U/L', 30.0, 120.0, 1, 'BIOCHEMISTRY', 'Kidney Function Test (KFT)', 'IFCC', 'ALP enzyme marker.', 6),
('Total Protein (KFT)', 'g/dL', 5.7, 8.2, 1, 'BIOCHEMISTRY', 'Kidney Function Test (KFT)', 'Biuret', 'Total protein monitoring.', 7),
('Albumin (KFT)', 'g/dL', 3.2, 4.8, 1, 'BIOCHEMISTRY', 'Kidney Function Test (KFT)', 'BCG', 'Albumin monitoring.', 8),
('Sodium (KFT)', 'mEq/L', 136.0, 145.0, 1, 'BIOCHEMISTRY', 'Kidney Function Test (KFT)', 'Indirect ISE', 'Electrolyte monitoring.', 9),
('Potassium (KFT)', 'mEq/L', 3.5, 5.1, 1, 'BIOCHEMISTRY', 'Kidney Function Test (KFT)', 'Indirect ISE', 'Electrolyte monitoring.', 10),
('Chloride (KFT)', 'mEq/L', 98.0, 107.0, 1, 'BIOCHEMISTRY', 'Kidney Function Test (KFT)', 'Indirect ISE', 'Electrolyte monitoring.', 11);

-- 9. Lipid Profile
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('Cholesterol, Total', 'mg/dL', 0.0, 200.0, 1, 'BIOCHEMISTRY', 'Lipid Profile', 'Spectrophotometry', 'Desirable: < 200 mg/dL.\nHigh: > 240 mg/dL.', 1),
('Triglycerides', 'mg/dL', 0.0, 150.0, 1, 'BIOCHEMISTRY', 'Lipid Profile', 'Spectrophotometry', 'Optimal: < 150 mg/dL.', 2),
('HDL Cholesterol', 'mg/dL', 40.0, 60.0, 1, 'BIOCHEMISTRY', 'Lipid Profile', 'Spectrophotometry', 'Low: < 40 mg/dL. High: > 60 mg/dL.', 3),
('LDL Cholesterol', 'mg/dL', 0.0, 100.0, 1, 'BIOCHEMISTRY', 'Lipid Profile', 'Calculated', 'Optimal: < 100 mg/dL.', 4),
('VLDL Cholesterol', 'mg/dL', 0.0, 30.0, 1, 'BIOCHEMISTRY', 'Lipid Profile', 'Calculated', 'Optimal: < 30 mg/dL.', 5),
('Non-HDL Cholesterol', 'mg/dL', 0.0, 130.0, 1, 'BIOCHEMISTRY', 'Lipid Profile', 'Calculated', 'Optimal: < 130 mg/dL.', 6);

-- 10. Liver Function Test (LFT)
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('AST (SGOT)', 'U/L', 15.0, 40.0, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'IFCC without P5P', 'Transaminase enzyme.', 1),
('ALT (SGPT)', 'U/L', 10.0, 49.0, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'IFCC without P5P', 'Transaminase enzyme.', 2),
('AST:ALT Ratio', 'Ratio', 0.0, 1.0, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'Calculated', 'Ratio of SGOT to SGPT.', 3),
('GGTP', 'U/L', 0.0, 73.0, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'IFCC', 'Biliary tract health marker.', 4),
('Alkaline Phosphatase (LFT)', 'U/L', 30.0, 120.0, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'IFCC-AMP', 'Bone/liver health marker.', 5),
('Bilirubin Total', 'mg/dL', 0.3, 1.2, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'DPD', 'Bilirubin breakdown clearance.', 6),
('Bilirubin Direct', 'mg/dL', 0.0, 0.3, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'DPD', 'Conjugated Bilirubin.', 7),
('Bilirubin Indirect', 'mg/dL', 0.1, 1.0, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'Calculated', 'Unconjugated Bilirubin.', 8),
('Total Protein (LFT)', 'g/dL', 5.7, 8.2, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'Biuret', 'Serum total proteins.', 9),
('Albumin (LFT)', 'g/dL', 3.2, 4.8, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'BCG', 'Serum albumin.', 10),
('A : G Ratio', 'Ratio', 0.9, 2.0, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'Calculated', 'Albumin-to-globulin ratio.', 11);

-- 11. Thyroid Profile (TFT)
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('Triiodothyronine (T3)', 'ng/dL', 40.0, 181.0, 1, 'ENDOCRINOLOGY', 'Thyroid Function Test (TFT)', 'CLIA', 'Monitors total T3 levels.', 1),
('Thyroxine (T4)', 'µg/dL', 4.5, 14.5, 1, 'ENDOCRINOLOGY', 'Thyroid Function Test (TFT)', 'CLIA', 'Monitors total T4 levels.', 2),
('TSH (Thyroid Stimulating Hormone)', 'µIU/mL', 0.35, 5.5, 1, 'ENDOCRINOLOGY', 'Thyroid Function Test (TFT)', 'CLIA', 'Ultra-sensitive TSH monitoring.', 3);

-- 12. Urine Routine (Clinical Pathology)
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('Urine Specific Gravity', 'Specific Gravity', 1.005, 1.030, 1, 'CLINICAL PATHOLOGY', 'Urine Routine', 'Refractometer', 'Measures urine concentration.', 1),
('Urine pH', 'pH', 5.0, 8.0, 1, 'CLINICAL PATHOLOGY', 'Urine Routine', 'pH Indicator Strip', 'Measures urine pH.', 2),
('Urine Pus Cells', 'cells/HPF', 0.0, 5.0, 1, 'CLINICAL PATHOLOGY', 'Urine Routine', 'Microscopy', 'Upto 5 is normal.', 3),
('Urine Epithelial Cells', 'cells/HPF', 0.0, 5.0, 1, 'CLINICAL PATHOLOGY', 'Urine Routine', 'Microscopy', 'Upto 5 is normal.', 4),
('Urine Sugar', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Routine', 'Reagent Strip', '0 = Absent, 1 = Present.', 5),
('Urine Protein', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Routine', 'Reagent Strip', '0 = Absent, 1 = Present.', 6);

-- 13. Vitamin B12
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('Vitamin B12', 'pg/mL', 120.0, 807.0, 1, 'IMMUNOASSAY', 'Vitamin B12', 'CLIA', 'Vitamin B12 is a water-soluble vitamin. Deficiency causes anemia and neurological issues.', 1);

-- 14. Vitamin D3
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('Vitamin D3 (25-Hydroxy)', 'ng/mL', 30.0, 100.0, 1, 'IMMUNOASSAY', 'Vitamin D3', 'CLIA', 'Deficiency: <20 ng/mL.\nInsufficiency: 20-30 ng/mL.\nSufficiency: 30-100 ng/mL.\nToxicity: >100 ng/mL.', 1);

-- 15. Iron Deficiency Profile
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('Serum Iron', 'µg/dL', 65.0, 175.0, 1, 'BIOCHEMISTRY', 'Iron Deficiency Profile', 'Ferrozine', 'Measures blood iron level.', 1),
('Total Iron Binding Capacity (TIBC)', 'µg/dL', 250.0, 450.0, 1, 'BIOCHEMISTRY', 'Iron Deficiency Profile', 'Ferrozine', 'Measures maximum iron binding capacity of transferrin.', 2),
('Transferrin Saturation', '%', 20.0, 50.0, 1, 'BIOCHEMISTRY', 'Iron Deficiency Profile', 'Calculated', 'Percent of transferrin bound with iron.', 3);

-- 16. Rheumatoid Arthritis (RA) Factor
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('Rheumatoid Factor (RF)', 'IU/mL', 0.0, 20.0, 1, 'SEROLOGY', 'RA Factor', 'Turbidimetry', 'Negative: < 20 IU/mL.\nPositive: >= 20 IU/mL.', 1);

-- 17. Australia Antigen (HBsAg)
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('HBsAg Screening', 'Index', 0.0, 1.0, 1, 'SEROLOGY', 'Australia Antigen (HBsAg)', 'Immunochromatography', 'Negative: < 1.0 (Non-reactive).\nPositive: >= 1.0 (Reactive).', 1);

-- 18. Hepatitis C Virus (HCV)
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('Anti-HCV Antibody', 'Index', 0.0, 1.0, 1, 'SEROLOGY', 'Hepatitis C Virus (HCV)', 'Immunochromatography', 'Negative: < 1.0 (Non-reactive).\nPositive: >= 1.0 (Reactive).', 1);

-- 19. Malaria Test
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('Rapid Malaria (HRP-2/pLDH)', 'Index', 0.0, 0.0, 1, 'HEMATOLOGY', 'Malaria Test', 'Immunochromatography', '0 = Negative, 1 = Positive.', 1),
('PBS Malarial Parasite', 'Index', 0.0, 0.0, 1, 'HEMATOLOGY', 'Malaria Test', 'Microscopy', '0 = Not Detected, 1 = Detected.', 2);

-- 20. Widal Test
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('S. Typhi O Agglutination', 'Titer', 0.0, 80.0, 1, 'SEROLOGY', 'Widal Test', 'Agglutination', 'Significant titer is > 1:80. Clinical correlation advised.', 1),
('S. Typhi H Agglutination', 'Titer', 0.0, 80.0, 1, 'SEROLOGY', 'Widal Test', 'Agglutination', 'Significant titer is > 1:80. Clinical correlation advised.', 2),
('S. Paratyphi A(H) Agglutination', 'Titer', 0.0, 80.0, 1, 'SEROLOGY', 'Widal Test', 'Agglutination', 'Significant titer is > 1:80. Clinical correlation advised.', 3),
('S. Paratyphi B(H) Agglutination', 'Titer', 0.0, 80.0, 1, 'SEROLOGY', 'Widal Test', 'Agglutination', 'Significant titer is > 1:80. Clinical correlation advised.', 4);

-- 21. VDRL (Syphilis)
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('VDRL Screening', 'Index', 0.0, 1.0, 1, 'SEROLOGY', 'VDRL', 'Flocculation', 'Negative: < 1.0 (Non-reactive). Positive: >= 1.0.', 1);

-- 22. Blood Group
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('Blood Grouping & Rh', 'Blood Group', 1.0, 8.0, 1, 'CLINICAL PATHOLOGY', 'Blood Group', 'Monoclonal slide agglutination', '1 = A Rh Positive\n2 = A Rh Negative\n3 = B Rh Positive\n4 = B Rh Negative\n5 = O Rh Positive\n6 = O Rh Negative\n7 = AB Rh Positive\n8 = AB Rh Negative', 1);

-- 23. PT-INR
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('Patient Prothrombin Time', 'seconds', 12.0, 16.0, 1, 'HEMATOLOGY', 'PT-INR', 'Coagulation', 'Assess coagulation pathways.', 1),
('INR', 'INR', 2.0, 3.0, 1, 'HEMATOLOGY', 'PT-INR', 'Calculated', 'Standard therapy INR range: 2.0 - 3.0.', 2);

-- Set realistic individual test prices
-- CBC Group
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'Hemoglobin (Hb)';
UPDATE TestTypes SET Price = 100.00 WHERE Name = 'Total RBC count';
UPDATE TestTypes SET Price = 100.00 WHERE Name = 'Packed Cell Volume (PCV)';
UPDATE TestTypes SET Price = 100.00 WHERE Name = 'Mean Corpuscular Volume (MCV)';
UPDATE TestTypes SET Price = 100.00 WHERE Name = 'Mean Corpuscular Hb (MCH)';
UPDATE TestTypes SET Price = 100.00 WHERE Name = 'Mean Corpuscular Hb Concn. (MCHC)';
UPDATE TestTypes SET Price = 100.00 WHERE Name = 'RDW';
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'Total WBC count';
UPDATE TestTypes SET Price = 50.00 WHERE Name = 'Neutrophils';
UPDATE TestTypes SET Price = 50.00 WHERE Name = 'Lymphocytes';
UPDATE TestTypes SET Price = 50.00 WHERE Name = 'Eosinophils';
UPDATE TestTypes SET Price = 50.00 WHERE Name = 'Monocytes';
UPDATE TestTypes SET Price = 50.00 WHERE Name = 'Basophils';
UPDATE TestTypes SET Price = 200.00 WHERE Name = 'Platelet Count';
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'ESR';

-- CRP
UPDATE TestTypes SET Price = 400.00 WHERE Name = 'C-Reactive Protein';

-- Dengue
UPDATE TestTypes SET Price = 500.00 WHERE Name = 'Dengue Fever Antibody, IgG';
UPDATE TestTypes SET Price = 500.00 WHERE Name = 'Dengue Fever Antibody, IgM';

-- Electrolytes
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'Sodium';
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'Potassium';
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'Chloride';
UPDATE TestTypes SET Price = 200.00 WHERE Name = 'Bicarbonate';
UPDATE TestTypes SET Price = 200.00 WHERE Name = 'Calcium';
UPDATE TestTypes SET Price = 250.00 WHERE Name = 'Magnesium';

-- Sugar
UPDATE TestTypes SET Price = 100.00 WHERE Name = 'Glucose, Fasting (Plasma)';
UPDATE TestTypes SET Price = 100.00 WHERE Name = 'Glucose, Post Prandial (Plasma)';
UPDATE TestTypes SET Price = 100.00 WHERE Name = 'Glucose, Random (Plasma)';

-- HIV
UPDATE TestTypes SET Price = 300.00 WHERE Name = 'HIV 1 Antibody Screening';
UPDATE TestTypes SET Price = 300.00 WHERE Name = 'HIV 2 Antibody Screening';

-- KFT
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'Urea (KFT)';
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'Creatinine (KFT)';
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'Uric Acid (KFT)';
UPDATE TestTypes SET Price = 200.00 WHERE Name = 'Calcium, Total (KFT)';
UPDATE TestTypes SET Price = 200.00 WHERE Name = 'Phosphorus (KFT)';
UPDATE TestTypes SET Price = 250.00 WHERE Name = 'Alkaline Phosphatase (KFT)';
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'Total Protein (KFT)';
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'Albumin (KFT)';
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'Sodium (KFT)';
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'Potassium (KFT)';
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'Chloride (KFT)';

-- Lipid
UPDATE TestTypes SET Price = 200.00 WHERE Name = 'Cholesterol, Total';
UPDATE TestTypes SET Price = 250.00 WHERE Name = 'Triglycerides';
UPDATE TestTypes SET Price = 250.00 WHERE Name = 'HDL Cholesterol';
UPDATE TestTypes SET Price = 200.00 WHERE Name = 'LDL Cholesterol';
UPDATE TestTypes SET Price = 200.00 WHERE Name = 'VLDL Cholesterol';
UPDATE TestTypes SET Price = 200.00 WHERE Name = 'Non-HDL Cholesterol';

-- LFT
UPDATE TestTypes SET Price = 200.00 WHERE Name = 'AST (SGOT)';
UPDATE TestTypes SET Price = 200.00 WHERE Name = 'ALT (SGPT)';
UPDATE TestTypes SET Price = 100.00 WHERE Name = 'AST:ALT Ratio';
UPDATE TestTypes SET Price = 250.00 WHERE Name = 'GGTP';
UPDATE TestTypes SET Price = 250.00 WHERE Name = 'Alkaline Phosphatase (LFT)';
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'Bilirubin Total';
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'Bilirubin Direct';
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'Bilirubin Indirect';
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'Total Protein (LFT)';
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'Albumin (LFT)';
UPDATE TestTypes SET Price = 100.00 WHERE Name = 'A : G Ratio';

-- Thyroid
UPDATE TestTypes SET Price = 250.00 WHERE Name = 'Triiodothyronine (T3)';
UPDATE TestTypes SET Price = 250.00 WHERE Name = 'Thyroxine (T4)';
UPDATE TestTypes SET Price = 300.00 WHERE Name = 'TSH (Thyroid Stimulating Hormone)';

-- Urine
UPDATE TestTypes SET Price = 100.00 WHERE Name = 'Urine Specific Gravity';
UPDATE TestTypes SET Price = 100.00 WHERE Name = 'Urine pH';
UPDATE TestTypes SET Price = 100.00 WHERE Name = 'Urine Pus Cells';
UPDATE TestTypes SET Price = 100.00 WHERE Name = 'Urine Epithelial Cells';
UPDATE TestTypes SET Price = 100.00 WHERE Name = 'Urine Sugar';
UPDATE TestTypes SET Price = 100.00 WHERE Name = 'Urine Protein';

-- Vitamins
UPDATE TestTypes SET Price = 900.00 WHERE Name = 'Vitamin B12';
UPDATE TestTypes SET Price = 1000.00 WHERE Name = 'Vitamin D3';

-- Iron
UPDATE TestTypes SET Price = 300.00 WHERE Name = 'Serum Iron';
UPDATE TestTypes SET Price = 350.00 WHERE Name = 'Total Iron Binding Capacity (TIBC)';
UPDATE TestTypes SET Price = 200.00 WHERE Name = 'Transferrin Saturation';

-- Others
UPDATE TestTypes SET Price = 400.00 WHERE Name = 'Rheumatoid Factor (RF)';
UPDATE TestTypes SET Price = 300.00 WHERE Name = 'HBsAg Screening';
UPDATE TestTypes SET Price = 400.00 WHERE Name = 'Anti-HCV Antibody';
UPDATE TestTypes SET Price = 250.00 WHERE Name = 'Rapid Malaria (HRP-2/pLDH)';
UPDATE TestTypes SET Price = 250.00 WHERE Name = 'PBS Malarial Parasite';
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'S. Typhi O Agglutination';
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'S. Typhi H Agglutination';
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'S. Paratyphi A(H) Agglutination';
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'S. Paratyphi B(H) Agglutination';
UPDATE TestTypes SET Price = 250.00 WHERE Name = 'VDRL Screening';
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'Blood Grouping & Rh';
UPDATE TestTypes SET Price = 300.00 WHERE Name = 'Patient Prothrombin Time';
UPDATE TestTypes SET Price = 150.00 WHERE Name = 'INR';

-- Set SampleTypes for all TestTypes based on category and names
UPDATE TestTypes SET SampleType = 'Blood' WHERE Category = 'HEMATOLOGY';
UPDATE TestTypes SET SampleType = 'Serum' WHERE Category IN ('SEROLOGY', 'IMMUNOASSAY', 'ENDOCRINOLOGY', 'BIOCHEMISTRY');
UPDATE TestTypes SET SampleType = 'Urine' WHERE Category = 'CLINICAL PATHOLOGY';
UPDATE TestTypes SET SampleType = 'Blood' WHERE Name = 'Blood Grouping & Rh';

-- Seed ReferenceRanges
INSERT INTO ReferenceRanges (TestTypeId, Gender, AgeMin, AgeMax, RangeLow, RangeHigh) VALUES
((SELECT TypeId FROM TestTypes WHERE Name = 'Hemoglobin (Hb)'), 'Male', 12, 120, 13.0, 17.0),
((SELECT TypeId FROM TestTypes WHERE Name = 'Hemoglobin (Hb)'), 'Female', 12, 120, 12.0, 15.0),
((SELECT TypeId FROM TestTypes WHERE Name = 'Hemoglobin (Hb)'), 'Male', 0, 11, 11.0, 14.5),
((SELECT TypeId FROM TestTypes WHERE Name = 'Hemoglobin (Hb)'), 'Female', 0, 11, 11.0, 14.5),
((SELECT TypeId FROM TestTypes WHERE Name = 'Hemoglobin (Hb)'), 'Other', 0, 120, 12.0, 16.0);

INSERT INTO ReferenceRanges (TestTypeId, Gender, AgeMin, AgeMax, RangeLow, RangeHigh) VALUES
((SELECT TypeId FROM TestTypes WHERE Name = 'Glucose, Fasting (Plasma)'), 'Male', 0, 120, 70.0, 100.0),
((SELECT TypeId FROM TestTypes WHERE Name = 'Glucose, Fasting (Plasma)'), 'Female', 0, 120, 70.0, 100.0),
((SELECT TypeId FROM TestTypes WHERE Name = 'Glucose, Fasting (Plasma)'), 'Other', 0, 120, 70.0, 100.0);

-- Seed TestPanels
INSERT INTO TestPanels (Name, Description, Price) VALUES
('Lipid Profile Panel', 'Comprehensive assessment of total cholesterol, triglycerides, HDL, LDL, VLDL, and non-HDL cholesterol.', 1200.00),
('Thyroid Profile Panel', 'Thyroid Function Test including T3, T4, and TSH screening.', 900.00);

-- Seed PanelTestTypes
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

-- Set InputTypes for non-numeric TestTypes
UPDATE TestTypes SET InputType = 1 WHERE Name IN ('Urine Sugar', 'Urine Protein');
UPDATE TestTypes SET InputType = 2 WHERE Name = 'Blood Grouping & Rh';
UPDATE TestTypes SET InputType = 3 WHERE Name IN ('Rapid Malaria (HRP-2/pLDH)', 'PBS Malarial Parasite', 'HBsAg Screening', 'Anti-HCV Antibody', 'VDRL Screening', 'HIV 1 Antibody Screening', 'HIV 2 Antibody Screening');
