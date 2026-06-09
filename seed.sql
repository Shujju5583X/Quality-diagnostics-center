-- Seed Patients
INSERT INTO Patients (FullName, DateOfBirth, ContactPhone, ContactEmail, CreatedAt, Gender) VALUES ('John Doe', '1980-05-15', '555-1234', 'john@example.com', '2026-06-08T00:00:00Z', 'Male');
INSERT INTO Patients (FullName, DateOfBirth, ContactPhone, ContactEmail, CreatedAt, Gender) VALUES ('Jane Smith', '1992-11-20', '555-5678', 'jane@example.com', '2026-06-08T00:00:00Z', 'Female');
INSERT INTO Patients (FullName, DateOfBirth, ContactPhone, ContactEmail, CreatedAt, Gender) VALUES ('Alice Johnson', '1975-02-10', '555-8765', 'alice@example.com', '2026-06-08T00:00:00Z', 'Female');
INSERT INTO Patients (FullName, DateOfBirth, ContactPhone, ContactEmail, CreatedAt, Gender) VALUES ('Yash M. Patel', '2005-08-25', '0123456789', 'yash@example.com', '2026-06-09T00:00:00Z', 'Male');

-- Seed Staff (Password is '1234' hashed with BCrypt)
INSERT INTO Staff (FullName, Role, PinHash, FailedLoginAttempts, LockoutEnd) VALUES ('Dr. Robert Brown', 'Admin', '$2a$11$/kj.NC923I71HcIDmIOASeJhA7Il5NLBh6Mb/nO8Thz/J2ooDHwIC', 0, NULL);
INSERT INTO Staff (FullName, Role, PinHash, FailedLoginAttempts, LockoutEnd) VALUES ('Tech Sarah', 'Technician', '$2a$11$/kj.NC923I71HcIDmIOASeJhA7Il5NLBh6Mb/nO8Thz/J2ooDHwIC', 0, NULL);

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
