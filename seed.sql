-- 1. Seed Patients & Staff (Cleared for clean production build)


-- 2. Seed Test Catalog (TestTypes)
-- Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType

-- 2.1 Complete Blood Count (CBC)
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType) VALUES
('Hemoglobin (Hb)', 'g/dL', 13.0, 17.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Further confirm for Anemia', 1, 150.00, 'Blood', 0),
('Total RBC count', 'mill/cumm', 4.5, 5.5, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Further confirm for Anemia', 2, 100.00, 'Blood', 0),
('Total WBC count', 'cumm', 4000.0, 11000.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Further confirm for Anemia', 3, 150.00, 'Blood', 0),
('Eosinophils', '%', 0.0, 6.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Further confirm for Anemia', 4, 50.00, 'Blood', 0),
('Packed Cell Volume (PCV)', '%', 40.0, 50.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Measures percentage of red blood cells in blood.', 5, 50.00, 'Blood', 0),
('Mean Corpuscular Volume (MCV)', 'fL', 83.0, 101.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Measures average size of red blood cells.', 6, 50.00, 'Blood', 0),
('MCH', 'pg', 27.0, 32.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Mean Corpuscular Hemoglobin - average hemoglobin per red cell.', 7, 50.00, 'Blood', 0),
('MCHC', 'g/dL', 32.5, 34.5, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Mean Corpuscular Hemoglobin Concentration.', 8, 50.00, 'Blood', 0),
('RDW', '%', 11.6, 14.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Red cell distribution width; elevated in mixed anemias.', 9, 50.00, 'Blood', 0),
('Neutrophils', '%', 50.0, 62.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Differential WBC count - Neutrophils.', 10, 50.00, 'Blood', 0),
('Lymphocytes', '%', 20.0, 40.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Differential WBC count - Lymphocytes.', 11, 50.00, 'Blood', 0),
('Monocytes', '%', 0.0, 10.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Differential WBC count - Monocytes.', 12, 50.00, 'Blood', 0),
('Basophils', '%', 0.0, 2.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Differential WBC count - Basophils.', 13, 50.00, 'Blood', 0),
('Platelet Count', 'cumm', 150000.0, 410000.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC)', 'Fully automated cell counter', 'Borderline: 150000 - 410000 cumm. Low platelets may indicate thrombocytopenia.', 14, 100.00, 'Blood', 0),
('ESR', 'mm/hr', 0.0, 15.0, 1, 'HEMATOLOGY', 'Complete Blood Count (CBC) with ESR', 'Capillary photometry', 'Non-specific marker for inflammation', 15, 150.00, 'Blood', 0);

-- 2.2 Biochemistry
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType) VALUES
('C-Reactive Protein', 'mg/dL', 0.0, 5.0, 1, 'BIOCHEMISTRY', 'C-Reactive Protein (CRP)', 'Turbidimetry', '1. Measurement of CRP is useful for the detection and evaluation of infection, tissue injury, and inflammatory disorders.\n2. Increased CRP suggests presence of inflammation.', 1, 400.00, 'Blood', 0),
('Sodium', 'mEq/L', 136.0, 145.0, 1, 'BIOCHEMISTRY', 'Electrolytes', 'Indirect ISE', 'Used for measuring hydration status and kidney function.', 1, 150.00, 'Blood', 0),
('Potassium', 'mEq/L', 3.5, 5.1, 1, 'BIOCHEMISTRY', 'Electrolytes', 'Indirect ISE', 'Measures critical blood potassium concentration.', 2, 150.00, 'Blood', 0),
('Chloride', 'mEq/L', 98.0, 107.0, 1, 'BIOCHEMISTRY', 'Electrolytes', 'Indirect ISE', 'Measures blood chloride level.', 3, 150.00, 'Blood', 0),
('Calcium', 'mg/dL', 8.6, 10.2, 1, 'BIOCHEMISTRY', 'Electrolytes', 'Indirect ISE', 'Measures total calcium levels.', 5, 200.00, 'Blood', 0),
('Glucose, Fasting (Plasma)', 'mg/dL', 70.0, 100.0, 1, 'BIOCHEMISTRY', 'Fasting Blood Sugar (FBS)', 'Hexokinase', 'Fasting glucose reference: 70 - 100 mg/dL.', 1, 100.00, 'Blood', 0),
('Glucose, Post Prandial (Plasma)', 'mg/dL', 90.0, 140.0, 1, 'BIOCHEMISTRY', 'Post Lunch Blood Sugar (PLBS)', 'Hexokinase', 'Post prandial reference: 90 - 140 mg/dL.', 2, 100.00, 'Blood', 0),
('Glucose, Random (Plasma)', 'mg/dL', 70.0, 150.0, 1, 'BIOCHEMISTRY', 'Random Blood Sugar (RBS)', 'Hexokinase', 'Random reference: 70 - 150 mg/dL.', 3, 100.00, 'Blood', 0),
('Urea (KFT)', 'mg/dL', 13.0, 43.0, 1, 'BIOCHEMISTRY', 'Kidney Function Test (KFT)', 'Urease UV', 'Assess kidney waste clearing.', 1, 150.00, 'Blood', 0),
('Creatinine (KFT)', 'mg/dL', 0.7, 1.3, 1, 'BIOCHEMISTRY', 'Kidney Function Test (KFT)', 'Modified Jaffe, Kinetic', 'Assess muscle waste clearance.', 2, 150.00, 'Blood', 0),
('Uric Acid (KFT)', 'mg/dL', 3.5, 7.2, 1, 'BIOCHEMISTRY', 'Kidney Function Test (KFT)', 'Uricase', 'Assess uric acid clearance.', 3, 150.00, 'Blood', 0),
('Total Protein', 'g/dL', 5.7, 8.2, 1, 'BIOCHEMISTRY', 'Biochemistry', 'Biuret', 'Total protein monitoring.', 7, 150.00, 'Blood', 0),
('Albumin', 'g/dL', 3.2, 4.8, 1, 'BIOCHEMISTRY', 'Biochemistry', 'BCG', 'Albumin monitoring.', 8, 150.00, 'Blood', 0),
('Cholesterol, Total', 'mg/dL', 0.0, 200.0, 1, 'BIOCHEMISTRY', 'Lipid Profile', 'Spectrophotometry', 'Desirable: < 200 mg/dL.\nHigh: > 240 mg/dL.', 1, 200.00, 'Blood', 0),
('Triglycerides', 'mg/dL', 0.0, 150.0, 1, 'BIOCHEMISTRY', 'Lipid Profile', 'Spectrophotometry', 'Optimal: < 150 mg/dL.', 2, 250.00, 'Blood', 0),
('HDL Cholesterol', 'mg/dL', 40.0, 60.0, 1, 'BIOCHEMISTRY', 'Lipid Profile', 'Spectrophotometry', 'Low: < 40 mg/dL. High: > 60 mg/dL.', 3, 250.00, 'Blood', 0),
('LDL Cholesterol', 'mg/dL', 0.0, 100.0, 1, 'BIOCHEMISTRY', 'Lipid Profile', 'Calculated', 'Optimal: < 100 mg/dL.', 4, 200.00, 'Blood', 0),
('VLDL Cholesterol', 'mg/dL', 0.0, 30.0, 1, 'BIOCHEMISTRY', 'Lipid Profile', 'Calculated', 'Optimal: < 30 mg/dL.', 5, 200.00, 'Blood', 0),
('AST (SGOT)', 'U/L', 15.0, 40.0, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'IFCC without P5P', 'Transaminase enzyme.', 1, 200.00, 'Blood', 0),
('ALT (SGPT)', 'U/L', 10.0, 49.0, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'IFCC without P5P', 'Transaminase enzyme.', 2, 200.00, 'Blood', 0),
('Alkaline Phosphatase (LFT)', 'U/L', 30.0, 120.0, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'IFCC-AMP', 'Bone/liver health marker.', 5, 250.00, 'Blood', 0),
('Bilirubin Total', 'mg/dL', 0.3, 1.2, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'DPD', 'Bilirubin breakdown clearance.', 6, 150.00, 'Blood', 0),
('Bilirubin Direct', 'mg/dL', 0.0, 0.3, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'DPD', 'Conjugated Bilirubin.', 7, 150.00, 'Blood', 0),
('Bilirubin Indirect', 'mg/dL', 0.1, 1.0, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'Calculated', 'Unconjugated Bilirubin.', 8, 150.00, 'Blood', 0),
('HbA1c', '%', 4.0, 5.6, 1, 'BIOCHEMISTRY', 'HBA1C', 'HPLC', 'Normal: < 5.7%, Pre-diabetes: 5.7-6.4%, Diabetes: >= 6.5%', 1, 400.00, 'Blood', 0);

-- 2.3 Endocrinology
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType) VALUES
('Triiodothyronine (T3)', 'ng/dL', 40.0, 181.0, 1, 'ENDOCRINOLOGY', 'Thyroid Function Test (TFT)', 'CLIA', 'Monitors total T3 levels.', 1, 250.00, 'Blood', 0),
('Thyroxine (T4)', 'µg/dL', 4.5, 14.5, 1, 'ENDOCRINOLOGY', 'Thyroid Function Test (TFT)', 'CLIA', 'Monitors total T4 levels.', 2, 250.00, 'Blood', 0),
('TSH (Thyroid Stimulating Hormone)', 'µIU/mL', 0.35, 5.5, 1, 'ENDOCRINOLOGY', 'Thyroid Function Test (TFT)', 'CLIA', 'Ultra-sensitive TSH monitoring.', 3, 300.00, 'Blood', 0);

-- 2.4 Clinical Pathology
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType) VALUES
('Urine Color', 'Color', NULL, NULL, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Visual', 'Normal: Yellow to Amber.', 1, 100.00, 'Urine', 0),
('Urine Appearance', 'Appearance', NULL, NULL, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Visual', 'Normal: Clear.', 2, 100.00, 'Urine', 0),
('Urine Volume', 'mL', NULL, NULL, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Visual', 'Volume of urine collected for examination.', 3, 50.00, 'Urine', 0),
('Urine Specific Gravity', 'Specific Gravity', 1.005, 1.030, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Refractometer', 'Measures urine concentration.', 4, 100.00, 'Urine', 0),
('Urine pH', 'pH', 5.0, 8.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'pH Indicator Strip', 'Measures urine pH.', 5, 100.00, 'Urine', 0),
('Urine Reaction', 'pH', 5.0, 8.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'pH Strip', 'Normal: 5.0 - 8.0.', 6, 100.00, 'Urine', 0),
('Urine Protein', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Reagent Strip', '0 = Absent, 1 = Present.', 7, 100.00, 'Urine', 1),
('Urine Sugar', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Reagent Strip', '0 = Absent, 1 = Present.', 8, 100.00, 'Urine', 1),
('Urine Ketone Bodies', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Reagent Strip', '0 = Absent, 1 = Present.', 9, 100.00, 'Urine', 1),
('Urine Urobilinogen', 'mg/dL', 0.0, 1.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Reagent Strip', 'Normal: 0.1 - 1.0 mg/dL.', 10, 100.00, 'Urine', 0),
('Urine Bile Salts', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Reagent Strip', '0 = Absent, 1 = Present.', 11, 100.00, 'Urine', 1),
('Urine Bile Pigments', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Reagent Strip', '0 = Absent, 1 = Present.', 12, 100.00, 'Urine', 1),
('Urine Blood', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Reagent Strip', '0 = Absent, 1 = Present.', 13, 50.00, 'Urine', 1),
('Urine Nitrite', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Reagent Strip', '0 = Negative, 1 = Positive.', 14, 100.00, 'Urine', 1),
('Urine Leukocyte Esterase', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Reagent Strip', '0 = Negative, 1 = Positive.', 15, 100.00, 'Urine', 1),
('Urine Pus Cells', 'cells/HPF', 0.0, 5.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Microscopy', 'Upto 5 is normal.', 16, 100.00, 'Urine', 0),
('Urine Epithelial Cells', 'cells/HPF', 0.0, 5.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Microscopy', 'Upto 5 is normal.', 17, 100.00, 'Urine', 0),
('Urine RBC', 'cells/HPF', 0.0, 2.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Microscopy', 'Normal: 0-2 RBCs per HPF. Higher counts suggest hematuria.', 18, 50.00, 'Urine', 0),
('Urine Casts', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Microscopy', '0 = Absent, 1 = Present. Presence may indicate renal disease.', 19, 50.00, 'Urine', 1),
('Urine Crystals', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Microscopy', '0 = Nil, 1 = Present.', 20, 50.00, 'Urine', 1),
('Urine Others', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Microscopy', 'Other microscopic findings.', 21, 50.00, 'Urine', 1),
('Blood Grouping & Rh', 'Blood Group', 1.0, 8.0, 1, 'CLINICAL PATHOLOGY', 'Blood Group', 'Monoclonal slide agglutination', '1 = A Rh Positive\n2 = A Rh Negative\n3 = B Rh Positive\n4 = B Rh Negative\n5 = O Rh Positive\n6 = O Rh Negative\n7 = AB Rh Positive\n8 = AB Rh Negative', 1, 150.00, 'Blood', 2);

-- 2.5 Serology
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType) VALUES
('Rheumatoid Factor (RF)', 'IU/mL', 0.0, 20.0, 1, 'SEROLOGY', 'RA Factor', 'Turbidimetry', 'Negative: < 20 IU/mL.\nPositive: >= 20 IU/mL.', 1, 400.00, 'Blood', 0),
('HBsAg Screening', 'Index', 0.0, 1.0, 1, 'SEROLOGY', 'Australia Antigen (HBsAg)', 'Immunochromatography', 'Negative: < 1.0 (Non-reactive).\nPositive: >= 1.0 (Reactive).', 1, 300.00, 'Blood', 3),
('Anti-HCV Antibody', 'Index', 0.0, 1.0, 1, 'SEROLOGY', 'Hepatitis C Virus (HCV)', 'Immunochromatography', 'Negative: < 1.0 (Non-reactive).\nPositive: >= 1.0 (Reactive).', 1, 400.00, 'Blood', 3),
('VDRL Screening', 'Index', 0.0, 1.0, 1, 'SEROLOGY', 'VDRL', 'Flocculation', 'Negative: < 1.0 (Non-reactive). Positive: >= 1.0.', 1, 250.00, 'Blood', 3),
('ASO Titer', 'IU/mL', 0.0, 200.0, 1, 'SEROLOGY', 'ASO Titer', 'Turbidimetry', 'Negative: < 200 IU/mL. Positive suggests recent streptococcal infection.', 1, 300.00, 'Blood', 0),
('Rapid Malaria Test', '', NULL, NULL, 1, 'SEROLOGY', 'Rapid Malaria Test', 'Immunochromatography', 'Negative: pv-positive/pf-positive/pv pf- positive not detected.\nPositive: Plasmodium vivax (pv) or Plasmodium falciparum (pf) antigen detected.', 1, 200.00, 'Blood', 3),
('Dengue NS1 Antigen', '', NULL, NULL, 1, 'SEROLOGY', 'Dengue Serology', 'Immunochromatography', 'Reactive indicates presence of Dengue NS1 Antigen.', 1, 300.00, 'Blood', 3),
('Dengue IgG Antibody', '', NULL, NULL, 1, 'SEROLOGY', 'Dengue Serology', 'Immunochromatography', 'Reactive indicates past Dengue infection.', 2, 250.00, 'Blood', 3),
('Dengue IgM Antibody', '', NULL, NULL, 1, 'SEROLOGY', 'Dengue Serology', 'Immunochromatography', 'Reactive indicates recent or acute Dengue infection.', 3, 250.00, 'Blood', 3),
('S. Typhi O Agglutination', 'Titer', 0.0, 40.0, 1, 'SEROLOGY', 'Widal Test', 'Agglutination', 'Significant titer is > 1:80. Clinical correlation advised.', 1, 150.00, 'Blood', 3),
('S. Typhi H Agglutination', 'Titer', 0.0, 40.0, 1, 'SEROLOGY', 'Widal Test', 'Agglutination', 'Significant titer is > 1:80. Clinical correlation advised.', 2, 150.00, 'Blood', 3),
('S. Paratyphi A(H) Agglutination', 'Titer', 0.0, 20.0, 1, 'SEROLOGY', 'Widal Test', 'Agglutination', 'Significant titer is > 1:80. Clinical correlation advised.', 3, 150.00, 'Blood', 3),
('S. Paratyphi B(H) Agglutination', 'Titer', 0.0, 20.0, 1, 'SEROLOGY', 'Widal Test', 'Agglutination', 'Significant titer is > 1:80. Clinical correlation advised.', 4, 150.00, 'Blood', 3);

-- 2.6 Microbiology
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType) VALUES
('Culture & Sensitivity', 'Report', NULL, NULL, 1, 'MICROBIOLOGY', 'Culture & Sensitivity', 'Culture', 'Organism identification and antibiotic sensitivity pattern.', 1, 500.00, 'Blood', 3);

-- 3. Seed ReferenceRanges
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

-- Additional ReferenceRanges for common tests
INSERT INTO ReferenceRanges (TestTypeId, Gender, AgeMin, AgeMax, RangeLow, RangeHigh) VALUES
((SELECT TypeId FROM TestTypes WHERE Name = 'Total RBC count'), 'Male', 12, 120, 4.5, 5.5),
((SELECT TypeId FROM TestTypes WHERE Name = 'Total RBC count'), 'Female', 12, 120, 3.8, 4.8),
((SELECT TypeId FROM TestTypes WHERE Name = 'Total RBC count'), 'Other', 0, 120, 3.8, 5.5);

INSERT INTO ReferenceRanges (TestTypeId, Gender, AgeMin, AgeMax, RangeLow, RangeHigh) VALUES
((SELECT TypeId FROM TestTypes WHERE Name = 'Total WBC count'), 'All', 0, 120, 4000.0, 11000.0);

INSERT INTO ReferenceRanges (TestTypeId, Gender, AgeMin, AgeMax, RangeLow, RangeHigh) VALUES
((SELECT TypeId FROM TestTypes WHERE Name = 'Creatinine (KFT)'), 'Male', 12, 120, 0.7, 1.3),
((SELECT TypeId FROM TestTypes WHERE Name = 'Creatinine (KFT)'), 'Female', 12, 120, 0.6, 1.1),
((SELECT TypeId FROM TestTypes WHERE Name = 'Creatinine (KFT)'), 'Other', 0, 120, 0.6, 1.3);

INSERT INTO ReferenceRanges (TestTypeId, Gender, AgeMin, AgeMax, RangeLow, RangeHigh) VALUES
((SELECT TypeId FROM TestTypes WHERE Name = 'Urea (KFT)'), 'Male', 12, 120, 15.0, 40.0),
((SELECT TypeId FROM TestTypes WHERE Name = 'Urea (KFT)'), 'Female', 12, 120, 12.0, 35.0),
((SELECT TypeId FROM TestTypes WHERE Name = 'Urea (KFT)'), 'Other', 0, 120, 12.0, 40.0);

INSERT INTO ReferenceRanges (TestTypeId, Gender, AgeMin, AgeMax, RangeLow, RangeHigh) VALUES
((SELECT TypeId FROM TestTypes WHERE Name = 'Bilirubin Total'), 'All', 0, 120, 0.1, 1.2);

INSERT INTO ReferenceRanges (TestTypeId, Gender, AgeMin, AgeMax, RangeLow, RangeHigh) VALUES
((SELECT TypeId FROM TestTypes WHERE Name = 'AST (SGOT)'), 'Male', 12, 120, 5.0, 40.0),
((SELECT TypeId FROM TestTypes WHERE Name = 'AST (SGOT)'), 'Female', 12, 120, 5.0, 35.0),
((SELECT TypeId FROM TestTypes WHERE Name = 'AST (SGOT)'), 'Other', 0, 120, 5.0, 40.0);

INSERT INTO ReferenceRanges (TestTypeId, Gender, AgeMin, AgeMax, RangeLow, RangeHigh) VALUES
((SELECT TypeId FROM TestTypes WHERE Name = 'ALT (SGPT)'), 'Male', 12, 120, 5.0, 40.0),
((SELECT TypeId FROM TestTypes WHERE Name = 'ALT (SGPT)'), 'Female', 12, 120, 5.0, 35.0),
((SELECT TypeId FROM TestTypes WHERE Name = 'ALT (SGPT)'), 'Other', 0, 120, 5.0, 40.0);

INSERT INTO ReferenceRanges (TestTypeId, Gender, AgeMin, AgeMax, RangeLow, RangeHigh) VALUES
((SELECT TypeId FROM TestTypes WHERE Name = 'Cholesterol, Total'), 'Male', 20, 120, 0.0, 200.0),
((SELECT TypeId FROM TestTypes WHERE Name = 'Cholesterol, Total'), 'Female', 20, 120, 0.0, 200.0),
((SELECT TypeId FROM TestTypes WHERE Name = 'Cholesterol, Total'), 'Other', 0, 120, 0.0, 200.0);

INSERT INTO ReferenceRanges (TestTypeId, Gender, AgeMin, AgeMax, RangeLow, RangeHigh) VALUES
((SELECT TypeId FROM TestTypes WHERE Name = 'Triglycerides'), 'Male', 20, 120, 0.0, 150.0),
((SELECT TypeId FROM TestTypes WHERE Name = 'Triglycerides'), 'Female', 20, 120, 0.0, 150.0),
((SELECT TypeId FROM TestTypes WHERE Name = 'Triglycerides'), 'Other', 0, 120, 0.0, 150.0);

INSERT INTO ReferenceRanges (TestTypeId, Gender, AgeMin, AgeMax, RangeLow, RangeHigh) VALUES
((SELECT TypeId FROM TestTypes WHERE Name = 'TSH (Thyroid Stimulating Hormone)'), 'All', 20, 120, 0.4, 4.0),
((SELECT TypeId FROM TestTypes WHERE Name = 'TSH (Thyroid Stimulating Hormone)'), 'All', 0, 19, 0.7, 11.0);

-- 4. Seed TestPanels
INSERT INTO TestPanels (Name, Description, Price) VALUES
('Lipid Profile Panel', 'Comprehensive assessment of total cholesterol, triglycerides, HDL, LDL, and VLDL.', 1200.00),
('Thyroid Profile Panel', 'Thyroid Function Test including T3, T4, and TSH screening.', 900.00),
('CBC Panel', 'Complete Blood Count including Haemoglobin, RBC, WBC, ESR, and Eosinophils.', 800.00),
('KFT Panel', 'Kidney Function Test including Blood Urea, Creatinine, and Uric Acid.', 600.00),
('LFT Panel', 'Liver Function Test including SGOT, SGPT, ALP, and Bilirubin.', 700.00),
('Electrolyte Panel', 'Serum Electrolytes including Sodium, Potassium, and Chloride.', 400.00),
('Complete Urine Examination Panel', 'Comprehensive physical, chemical, and microscopic examination of urine.', 300.00),
('Dengue Profile Panel', 'Comprehensive screening for Dengue including NS1 Antigen, IgG and IgM antibodies.', 700.00);

-- 5. Seed PanelTestTypes
INSERT INTO PanelTestTypes (PanelId, TypeId) VALUES
((SELECT PanelId FROM TestPanels WHERE Name = 'Lipid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Cholesterol, Total')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Lipid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Triglycerides')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Lipid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'HDL Cholesterol')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Lipid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'LDL Cholesterol')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Lipid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'VLDL Cholesterol'));

INSERT INTO PanelTestTypes (PanelId, TypeId) VALUES
((SELECT PanelId FROM TestPanels WHERE Name = 'Thyroid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Triiodothyronine (T3)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Thyroid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Thyroxine (T4)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Thyroid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'TSH (Thyroid Stimulating Hormone)'));

INSERT INTO PanelTestTypes (PanelId, TypeId) VALUES
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Hemoglobin (Hb)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Total RBC count')),
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Total WBC count')),
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'ESR')),
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Eosinophils')),
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Packed Cell Volume (PCV)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Mean Corpuscular Volume (MCV)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'MCH')),
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'MCHC')),
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'RDW')),
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Neutrophils')),
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Lymphocytes')),
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Monocytes')),
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Basophils')),
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Platelet Count'));

INSERT INTO PanelTestTypes (PanelId, TypeId) VALUES
((SELECT PanelId FROM TestPanels WHERE Name = 'KFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Urea (KFT)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'KFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Creatinine (KFT)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'KFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Uric Acid (KFT)'));

INSERT INTO PanelTestTypes (PanelId, TypeId) VALUES
((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'AST (SGOT)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'ALT (SGPT)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Alkaline Phosphatase (LFT)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Bilirubin Total')),
((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Bilirubin Direct')),
((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Bilirubin Indirect'));

INSERT INTO PanelTestTypes (PanelId, TypeId) VALUES
((SELECT PanelId FROM TestPanels WHERE Name = 'Electrolyte Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Sodium')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Electrolyte Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Potassium')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Electrolyte Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Chloride'));

INSERT INTO PanelTestTypes (PanelId, TypeId) VALUES
((SELECT PanelId FROM TestPanels WHERE Name = 'Complete Urine Examination Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Urine Color')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Complete Urine Examination Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Urine Appearance')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Complete Urine Examination Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Urine Volume')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Complete Urine Examination Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Urine Protein')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Complete Urine Examination Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Urine Sugar')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Complete Urine Examination Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Urine Ketone Bodies')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Complete Urine Examination Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Urine Bile Salts')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Complete Urine Examination Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Urine Bile Pigments')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Complete Urine Examination Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Urine Reaction')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Complete Urine Examination Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Urine Specific Gravity')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Complete Urine Examination Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Urine Blood')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Complete Urine Examination Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Urine Leukocyte Esterase')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Complete Urine Examination Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Urine Pus Cells')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Complete Urine Examination Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Urine Epithelial Cells')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Complete Urine Examination Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Urine RBC')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Complete Urine Examination Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Urine Casts')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Complete Urine Examination Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Urine Crystals')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Complete Urine Examination Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Urine Others'));

INSERT INTO PanelTestTypes (PanelId, TypeId) VALUES
((SELECT PanelId FROM TestPanels WHERE Name = 'Dengue Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Dengue NS1 Antigen')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Dengue Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Dengue IgG Antibody')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Dengue Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Dengue IgM Antibody'));

-- 6. Add missing tests from gap analysis
INSERT INTO TestTypes (Name, Unit, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType) VALUES
('HIV 1 Antibody Screening', 'Index', 1, 'SEROLOGY', 'HIV Screening', 'Immunochromatography', 'Negative: < 1.0 (Non-reactive).', 1, 350.00, 'Blood', 3),
('HIV 2 Antibody Screening', 'Index', 1, 'SEROLOGY', 'HIV Screening', 'Immunochromatography', 'Negative: < 1.0 (Non-reactive).', 2, 350.00, 'Blood', 3);

INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, SortOrder, Price, SampleType, InputType) VALUES
('Patient Prothrombin Time', 'seconds', 11.0, 16.0, 1, 'HEMATOLOGY', 'PT / INR', 'Coagulometric', 1, 300.00, 'Blood', 0),
('Control Prothrombin Time', 'seconds', 12.0, 16.0, 1, 'HEMATOLOGY', 'PT / INR', 'Coagulometric', 2, 0.00, 'Blood', 0),
('INR', 'Ratio', 0.8, 1.2, 1, 'HEMATOLOGY', 'PT / INR', 'Calculated', 3, 0.00, 'Blood', 0),
('Serum Iron', 'µg/dL', 60.0, 170.0, 1, 'BIOCHEMISTRY', 'Iron Studies', 'Ferrozine', 1, 200.00, 'Blood', 0),
('Total Iron Binding Capacity (TIBC)', 'µg/dL', 250.0, 370.0, 1, 'BIOCHEMISTRY', 'Iron Studies', 'Ferrozine', 2, 200.00, 'Blood', 0),
('Transferrin Saturation', '%', 20.0, 50.0, 1, 'BIOCHEMISTRY', 'Iron Studies', 'Calculated', 3, 0.00, 'Blood', 0),
('Vitamin B12', 'pg/mL', 211.0, 911.0, 1, 'BIOCHEMISTRY', 'Vitamin B12', 'CLIA', 1, 800.00, 'Blood', 0);

INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType) VALUES
('Vitamin D3 (25-Hydroxy)', 'ng/mL', 30.0, 100.0, 1, 'BIOCHEMISTRY', 'Vitamin D', 'CLIA', 'Deficient: <20, Insufficient: 20-29, Sufficient: 30-100, Toxic: >100', 1, 800.00, 'Blood', 0);

INSERT INTO TestTypes (Name, Unit, IsActive, Category, GroupName, Method, Interpretation, SortOrder, Price, SampleType, InputType) VALUES
('Hemoglobin Solubility Test (HBSG)', 'Result', 1, 'HEMATOLOGY', 'Hemoglobin Solubility Test', 'Dithionite tube test', 'Negative: Normal. Positive: Suggests Hb S (sickle hemoglobin).', 1, 200.00, 'Blood', 3);

INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, SortOrder, Price, SampleType, InputType) VALUES
('GGTP', 'U/L', 0.0, 55.0, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'IFCC', 3, 150.00, 'Blood', 0),
('Total Protein (LFT)', 'g/dL', 6.0, 8.3, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'Biuret', 4, 150.00, 'Blood', 0),
('Albumin (LFT)', 'g/dL', 3.5, 5.0, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'BCG', 9, 150.00, 'Blood', 0),
('A : G Ratio', 'Ratio', 1.1, 2.5, 1, 'BIOCHEMISTRY', 'Liver Function Test (LFT)', 'Calculated', 10, 0.00, 'Blood', 0),
('Blood Urea Nitrogen (BUN)', 'mg/dL', 7.0, 20.0, 1, 'BIOCHEMISTRY', 'Kidney Function Test (KFT)', 'Calculated (Urea / 2.14)', 4, 0.00, 'Blood', 0);

INSERT INTO TestPanels (Name, Description, Price) VALUES
('PT INR Panel', 'Prothrombin Time and INR.', 400.00),
('Iron Profile Panel', 'Comprehensive assessment of Iron, TIBC, and Transferrin Saturation.', 500.00),
('TSB Panel', 'Total Serum Bilirubin including Total, Direct, and Indirect Bilirubin.', 300.00);

INSERT INTO PanelTestTypes (PanelId, TypeId) VALUES
((SELECT PanelId FROM TestPanels WHERE Name = 'PT INR Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Patient Prothrombin Time')),
((SELECT PanelId FROM TestPanels WHERE Name = 'PT INR Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Control Prothrombin Time')),
((SELECT PanelId FROM TestPanels WHERE Name = 'PT INR Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'INR')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Iron Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Serum Iron')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Iron Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Total Iron Binding Capacity (TIBC)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Iron Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Transferrin Saturation')),
((SELECT PanelId FROM TestPanels WHERE Name = 'TSB Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Bilirubin Total')),
((SELECT PanelId FROM TestPanels WHERE Name = 'TSB Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Bilirubin Direct')),
((SELECT PanelId FROM TestPanels WHERE Name = 'TSB Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Bilirubin Indirect')),
((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'GGTP')),
((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Total Protein (LFT)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Albumin (LFT)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'A : G Ratio')),
((SELECT PanelId FROM TestPanels WHERE Name = 'KFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Blood Urea Nitrogen (BUN)'));
