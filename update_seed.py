import re

with open('seed.sql', 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Dengue
content = re.sub(r'-- 4\. Dengue Fever Panel\s+INSERT INTO TestTypes [^\n]+ VALUES\s+\(\'Dengue Fever Antibody, IgG\'.*?\),\s+\(\'Dengue Fever Antibody, IgM\'.*?\);\s+', '', content, flags=re.DOTALL)

# 2. Bicarbonate, Calcium, Magnesium -> Remove Bicarbonate & Magnesium
content = re.sub(r'\(\'Bicarbonate\', [^\n]+\),\s+(\(\'Calcium\', [^\n]+\)),\s+\(\'Magnesium\', [^\n]+\);', r'\1;', content)

# 3. HIV
content = re.sub(r'-- 7\. HIV 1 & 2 Antibodies\s+INSERT INTO TestTypes [^\n]+ VALUES\s+\(\'HIV 1 Antibody Screening\'.*?\),\s+\(\'HIV 2 Antibody Screening\'.*?\);\s+', '', content, flags=re.DOTALL)

# 4. KFT deletions 1
content = re.sub(r'\(\'Calcium, Total \(KFT\)\', [^\n]+\),\s+\(\'Phosphorus \(KFT\)\', [^\n]+\),\s+\(\'Alkaline Phosphatase \(KFT\)\', [^\n]+\),\s+', '', content)

# 5. KFT rename and deletions 2
content = re.sub(r'\(\'Total Protein \(KFT\)\', \'g/dL\', 5\.7, 8\.2, 1, \'BIOCHEMISTRY\', \'Kidney Function Test \(KFT\)\'(.*?)\),\s+\(\'Albumin \(KFT\)\', \'g/dL\', 3\.2, 4\.8, 1, \'BIOCHEMISTRY\', \'Kidney Function Test \(KFT\)\'(.*?)\),\s+\(\'Sodium \(KFT\)\'.*?\),\s+\(\'Potassium \(KFT\)\'.*?\),\s+\(\'Chloride \(KFT\)\'.*?\);', 
                 r"('Total Protein', 'g/dL', 5.7, 8.2, 1, 'BIOCHEMISTRY', 'Biochemistry'\1),\n('Albumin', 'g/dL', 3.2, 4.8, 1, 'BIOCHEMISTRY', 'Biochemistry'\2);", content, flags=re.DOTALL)

# 6. Lipid Non-HDL
content = re.sub(r'(\(\'VLDL Cholesterol\', [^\n]+\)),\s+\(\'Non-HDL Cholesterol\', [^\n]+\);', r'\1;', content)

# 7. LFT deletions 1
content = re.sub(r'\(\'AST:ALT Ratio\', [^\n]+\),\s+\(\'GGTP\', [^\n]+\),\s+', '', content)

# 8. LFT deletions 2
content = re.sub(r'(\(\'Bilirubin Indirect\', [^\n]+\)),\s+\(\'Total Protein \(LFT\)\', [^\n]+\),\s+\(\'Albumin \(LFT\)\', [^\n]+\),\s+\(\'A : G Ratio\', [^\n]+\);', r'\1;', content)

# 9. Vitamin B12
content = re.sub(r'-- 13\. Vitamin B12\s+INSERT INTO TestTypes [^\n]+ VALUES\s+\(\'Vitamin B12\'.*?\);\s+', '', content, flags=re.DOTALL)

# 10. Vitamin D3
content = re.sub(r'-- 14\. Vitamin D3\s+INSERT INTO TestTypes [^\n]+ VALUES\s+\(\'Vitamin D3 \(25-Hydroxy\)\'.*?\);\s+', '', content, flags=re.DOTALL)

# 11. Iron
content = re.sub(r'-- 15\. Iron Deficiency Profile\s+INSERT INTO TestTypes [^\n]+ VALUES\s+\(\'Serum Iron\'.*?\),\s+\(\'Total Iron Binding Capacity \(TIBC\)\'.*?\),\s+\(\'Transferrin Saturation\'.*?\);\s+', '', content, flags=re.DOTALL)

# 12. Malaria
content = re.sub(r'-- 19\. Malaria Test\s+INSERT INTO TestTypes [^\n]+ VALUES\s+\(\'Rapid Malaria \(HRP-2/pLDH\)\'.*?\),\s+\(\'PBS Malarial Parasite\'.*?\);\s+', '', content, flags=re.DOTALL)

# 13. PT-INR
content = re.sub(r'-- 23\. PT-INR\s+INSERT INTO TestTypes [^\n]+ VALUES\s+\(\'Patient Prothrombin Time\'.*?\),\s+\(\'INR\'.*?\);\s+', '', content, flags=re.DOTALL)

# PRICES
content = re.sub(r'UPDATE TestTypes SET Price = 100\.00 WHERE Name = \'Packed Cell Volume \(PCV\)\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 100\.00 WHERE Name = \'Mean Corpuscular Volume \(MCV\)\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 100\.00 WHERE Name = \'Mean Corpuscular Hb \(MCH\)\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 100\.00 WHERE Name = \'Mean Corpuscular Hb Concn\. \(MCHC\)\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 100\.00 WHERE Name = \'RDW\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 50\.00 WHERE Name = \'Neutrophils\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 50\.00 WHERE Name = \'Lymphocytes\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 50\.00 WHERE Name = \'Monocytes\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 50\.00 WHERE Name = \'Basophils\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 200\.00 WHERE Name = \'Platelet Count\';\s+', '', content)
content = re.sub(r'-- Dengue\s+UPDATE TestTypes SET Price = 500\.00 WHERE Name = \'Dengue Fever Antibody, IgG\';\s+UPDATE TestTypes SET Price = 500\.00 WHERE Name = \'Dengue Fever Antibody, IgM\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 200\.00 WHERE Name = \'Bicarbonate\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 250\.00 WHERE Name = \'Magnesium\';\s+', '', content)
content = re.sub(r'-- HIV\s+UPDATE TestTypes SET Price = 300\.00 WHERE Name = \'HIV 1 Antibody Screening\';\s+UPDATE TestTypes SET Price = 300\.00 WHERE Name = \'HIV 2 Antibody Screening\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 200\.00 WHERE Name = \'Calcium, Total \(KFT\)\';\s+UPDATE TestTypes SET Price = 200\.00 WHERE Name = \'Phosphorus \(KFT\)\';\s+UPDATE TestTypes SET Price = 250\.00 WHERE Name = \'Alkaline Phosphatase \(KFT\)\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 150\.00 WHERE Name = \'Sodium \(KFT\)\';\s+UPDATE TestTypes SET Price = 150\.00 WHERE Name = \'Potassium \(KFT\)\';\s+UPDATE TestTypes SET Price = 150\.00 WHERE Name = \'Chloride \(KFT\)\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 150\.00 WHERE Name = \'Total Protein \(KFT\)\';', r'UPDATE TestTypes SET Price = 150.00 WHERE Name = \'Total Protein\';', content)
content = re.sub(r'UPDATE TestTypes SET Price = 150\.00 WHERE Name = \'Albumin \(KFT\)\';', r'UPDATE TestTypes SET Price = 150.00 WHERE Name = \'Albumin\';', content)
content = re.sub(r'UPDATE TestTypes SET Price = 200\.00 WHERE Name = \'Non-HDL Cholesterol\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 100\.00 WHERE Name = \'AST:ALT Ratio\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 250\.00 WHERE Name = \'GGTP\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 150\.00 WHERE Name = \'Total Protein \(LFT\)\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 150\.00 WHERE Name = \'Albumin \(LFT\)\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 100\.00 WHERE Name = \'A : G Ratio\';\s+', '', content)
content = re.sub(r'-- Vitamins\s+UPDATE TestTypes SET Price = 900\.00 WHERE Name = \'Vitamin B12\';\s+UPDATE TestTypes SET Price = 1000\.00 WHERE Name = \'Vitamin D3\';\s+', '', content)
content = re.sub(r'-- Iron\s+UPDATE TestTypes SET Price = 300\.00 WHERE Name = \'Serum Iron\';\s+UPDATE TestTypes SET Price = 350\.00 WHERE Name = \'Total Iron Binding Capacity \(TIBC\)\';\s+UPDATE TestTypes SET Price = 200\.00 WHERE Name = \'Transferrin Saturation\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 250\.00 WHERE Name = \'Rapid Malaria \(HRP-2/pLDH\)\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 250\.00 WHERE Name = \'PBS Malarial Parasite\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 300\.00 WHERE Name = \'Patient Prothrombin Time\';\s+', '', content)
content = re.sub(r'UPDATE TestTypes SET Price = 150\.00 WHERE Name = \'INR\';\s+', '', content)

# REFERENCE RANGES
content = re.sub(r'INSERT INTO ReferenceRanges [^\n]+\s+\(\(SELECT TypeId FROM TestTypes WHERE Name = \'Packed Cell Volume \(PCV\)\'\).*?;\s+', '', content, flags=re.DOTALL)
content = re.sub(r'INSERT INTO ReferenceRanges [^\n]+\s+\(\(SELECT TypeId FROM TestTypes WHERE Name = \'Platelet Count\'\).*?;\s+', '', content, flags=re.DOTALL)

# SAMPLE TYPE
content = content.replace("UPDATE TestTypes SET SampleType = 'Serum' WHERE Category IN ('SEROLOGY', 'IMMUNOASSAY', 'ENDOCRINOLOGY', 'BIOCHEMISTRY');", "UPDATE TestTypes SET SampleType = 'Blood' WHERE Category IN ('SEROLOGY', 'IMMUNOASSAY', 'ENDOCRINOLOGY', 'BIOCHEMISTRY');")

# PANELS
content = re.sub(r'\(\(SELECT PanelId FROM TestPanels WHERE Name = \'Lipid Profile Panel\'\), \(SELECT TypeId FROM TestTypes WHERE Name = \'Non-HDL Cholesterol\'\)\);\s+', '((SELECT PanelId FROM TestPanels WHERE Name = \'Lipid Profile Panel\'), (SELECT TypeId FROM TestTypes WHERE Name = \'VLDL Cholesterol\'));\n\n', content)
content = content.replace("((SELECT PanelId FROM TestPanels WHERE Name = 'Lipid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'VLDL Cholesterol')),\n((SELECT PanelId FROM TestPanels WHERE Name = 'Lipid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'VLDL Cholesterol'));", "((SELECT PanelId FROM TestPanels WHERE Name = 'Lipid Profile Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'VLDL Cholesterol'));")

content = content.replace("('Lipid Profile Panel', 'Comprehensive assessment of total cholesterol, triglycerides, HDL, LDL, VLDL, and non-HDL cholesterol.', 1200.00)", "('Lipid Profile Panel', 'Comprehensive assessment of total cholesterol, triglycerides, HDL, LDL, and VLDL.', 1200.00)")
content = content.replace("('CBC Panel', 'Complete Blood Count including Haemoglobin, Haematocrit, RBC, WBC, Platelet, and differential counts.', 800.00)", "('CBC Panel', 'Complete Blood Count including Haemoglobin, RBC, WBC, ESR, and Eosinophils.', 800.00)")
content = content.replace("('KFT Panel', 'Kidney Function Test including Blood Urea, Creatinine, Uric Acid, and BUN.', 600.00)", "('KFT Panel', 'Kidney Function Test including Blood Urea, Creatinine, and Uric Acid.', 600.00)")
content = content.replace("('LFT Panel', 'Liver Function Test including SGOT, SGPT, ALP, Bilirubin, Protein, Albumin, and Globulin.', 700.00)", "('LFT Panel', 'Liver Function Test including SGOT, SGPT, ALP, and Bilirubin.', 700.00)")
content = content.replace("('Electrolyte Panel', 'Serum Electrolytes including Sodium, Potassium, Chloride, and Bicarbonate.', 400.00)", "('Electrolyte Panel', 'Serum Electrolytes including Sodium, Potassium, and Chloride.', 400.00)")

cbc_replacement = """((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Hemoglobin (Hb)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Total RBC count')),
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Total WBC count')),
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'ESR')),
((SELECT PanelId FROM TestPanels WHERE Name = 'CBC Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Eosinophils'));"""
content = re.sub(r'\(\(SELECT PanelId FROM TestPanels WHERE Name = \'CBC Panel\'\), \(SELECT TypeId FROM TestTypes WHERE Name = \'Hemoglobin \(Hb\)\'\)\),\s+.*?\(\(SELECT PanelId FROM TestPanels WHERE Name = \'CBC Panel\'\), \(SELECT TypeId FROM TestTypes WHERE Name = \'Basophils\'\)\);', cbc_replacement, content, flags=re.DOTALL)

kft_replacement = """((SELECT PanelId FROM TestPanels WHERE Name = 'KFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Urea (KFT)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'KFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Creatinine (KFT)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'KFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Uric Acid (KFT)'));"""
content = re.sub(r'\(\(SELECT PanelId FROM TestPanels WHERE Name = \'KFT Panel\'\), \(SELECT TypeId FROM TestTypes WHERE Name = \'Urea \(KFT\)\'\)\),\s+.*?\(\(SELECT PanelId FROM TestPanels WHERE Name = \'KFT Panel\'\), \(SELECT TypeId FROM TestTypes WHERE Name = \'Calcium, Total \(KFT\)\'\)\);', kft_replacement, content, flags=re.DOTALL)

lft_replacement = """((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'AST (SGOT)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'ALT (SGPT)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Alkaline Phosphatase (LFT)')),
((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Bilirubin Total')),
((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Bilirubin Direct')),
((SELECT PanelId FROM TestPanels WHERE Name = 'LFT Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Bilirubin Indirect'));"""
content = re.sub(r'\(\(SELECT PanelId FROM TestPanels WHERE Name = \'LFT Panel\'\), \(SELECT TypeId FROM TestTypes WHERE Name = \'AST \(SGOT\)\'\)\),\s+.*?\(\(SELECT PanelId FROM TestPanels WHERE Name = \'LFT Panel\'\), \(SELECT TypeId FROM TestTypes WHERE Name = \'Albumin \(LFT\)\'\)\);', lft_replacement, content, flags=re.DOTALL)

electrolyte_replacement = """((SELECT PanelId FROM TestPanels WHERE Name = 'Electrolyte Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Sodium')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Electrolyte Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Potassium')),
((SELECT PanelId FROM TestPanels WHERE Name = 'Electrolyte Panel'), (SELECT TypeId FROM TestTypes WHERE Name = 'Chloride'));"""
content = re.sub(r'\(\(SELECT PanelId FROM TestPanels WHERE Name = \'Electrolyte Panel\'\), \(SELECT TypeId FROM TestTypes WHERE Name = \'Sodium\'\)\),\s+.*?\(\(SELECT PanelId FROM TestPanels WHERE Name = \'Electrolyte Panel\'\), \(SELECT TypeId FROM TestTypes WHERE Name = \'Bicarbonate\'\)\);', electrolyte_replacement, content, flags=re.DOTALL)

# InputType update
content = content.replace("UPDATE TestTypes SET InputType = 3 WHERE Name IN ('Rapid Malaria (HRP-2/pLDH)', 'PBS Malarial Parasite', 'HBsAg Screening', 'Anti-HCV Antibody', 'VDRL Screening', 'HIV 1 Antibody Screening', 'HIV 2 Antibody Screening');", "UPDATE TestTypes SET InputType = 3 WHERE Name IN ('HBsAg Screening', 'Anti-HCV Antibody', 'VDRL Screening');")

# NEW TESTS (Add at end of TestTypes block, before SET realistic individual test prices)
new_tests = """-- NEW TESTS
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('HbA1c', '%', 4.0, 5.6, 1, 'BIOCHEMISTRY', 'HBA1C', 'HPLC', 'Normal: < 5.7%, Pre-diabetes: 5.7-6.4%, Diabetes: >= 6.5%', 1);

INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('ASO Titer', 'IU/mL', 0.0, 200.0, 1, 'SEROLOGY', 'ASO Titer', 'Turbidimetry', 'Negative: < 200 IU/mL. Positive suggests recent streptococcal infection.', 1);

INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('Urine Color', 'Color', NULL, NULL, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Visual', 'Normal: Yellow to Amber.', 1),
('Urine Appearance', 'Appearance', NULL, NULL, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Visual', 'Normal: Clear.', 2),
('Urine Reaction', 'pH', 5.0, 8.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'pH Strip', 'Normal: 5.0 - 8.0.', 3),
('Urine Ketone Bodies', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Reagent Strip', '0 = Absent, 1 = Present.', 4),
('Urine Urobilinogen', 'mg/dL', 0.0, 1.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Reagent Strip', 'Normal: 0.1 - 1.0 mg/dL.', 5),
('Urine Nitrite', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Reagent Strip', '0 = Negative, 1 = Positive.', 6),
('Urine Leukocyte Esterase', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Complete', 'Reagent Strip', '0 = Negative, 1 = Positive.', 7);

INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('Urine Bile Salts', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Routine', 'Reagent Strip', '0 = Absent, 1 = Present.', 7),
('Urine Bile Pigments', 'Qualitative', 0.0, 0.0, 1, 'CLINICAL PATHOLOGY', 'Urine Routine', 'Reagent Strip', '0 = Absent, 1 = Present.', 8);

INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive, Category, GroupName, Method, Interpretation, SortOrder) VALUES
('Culture & Sensitivity', 'Report', NULL, NULL, 1, 'MICROBIOLOGY', 'Culture & Sensitivity', 'Culture', 'Organism identification and antibiotic sensitivity pattern.', 1);

-- Set realistic individual test prices
"""
content = content.replace("-- Set realistic individual test prices\n", new_tests)

# Add remaining prices and properties
new_updates = """
UPDATE TestTypes SET Price = 400.00, SampleType = 'Blood' WHERE Name = 'HbA1c';
UPDATE TestTypes SET Price = 300.00, SampleType = 'Blood' WHERE Name = 'ASO Titer';
UPDATE TestTypes SET Price = 100.00, SampleType = 'Urine' WHERE GroupName = 'Urine Complete';
UPDATE TestTypes SET InputType = 1 WHERE GroupName = 'Urine Complete' AND Unit = 'Qualitative';
UPDATE TestTypes SET Price = 100.00, SampleType = 'Urine' WHERE Name IN ('Urine Bile Salts', 'Urine Bile Pigments');
UPDATE TestTypes SET InputType = 1 WHERE Name IN ('Urine Bile Salts', 'Urine Bile Pigments');
UPDATE TestTypes SET Price = 500.00, SampleType = 'Blood' WHERE Name = 'Culture & Sensitivity';
UPDATE TestTypes SET InputType = 3 WHERE Name = 'Culture & Sensitivity';

-- Set InputTypes for non-numeric TestTypes"""
content = content.replace("-- Set InputTypes for non-numeric TestTypes", new_updates)

with open('seed.sql', 'w', encoding='utf-8') as f:
    f.write(content)

print("Update complete")
