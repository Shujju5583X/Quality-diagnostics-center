INSERT INTO Patients (FullName, DateOfBirth, ContactPhone, ContactEmail, CreatedAt) VALUES ('John Doe', '1980-05-15', '555-1234', 'john@example.com', '2026-06-08T00:00:00Z');
INSERT INTO Patients (FullName, DateOfBirth, ContactPhone, ContactEmail, CreatedAt) VALUES ('Jane Smith', '1992-11-20', '555-5678', 'jane@example.com', '2026-06-08T00:00:00Z');
INSERT INTO Patients (FullName, DateOfBirth, ContactPhone, ContactEmail, CreatedAt) VALUES ('Alice Johnson', '1975-02-10', '555-8765', 'alice@example.com', '2026-06-08T00:00:00Z');

INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive) VALUES ('Hemoglobin', 'g/dL', 13.8, 17.2, 1);
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive) VALUES ('White Blood Cell Count', '10^9/L', 4.5, 11.0, 1);
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive) VALUES ('Platelet Count', '10^9/L', 150, 450, 1);
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive) VALUES ('Glucose (Fasting)', 'mg/dL', 70, 99, 1);
INSERT INTO TestTypes (Name, Unit, ReferenceRangeLow, ReferenceRangeHigh, IsActive) VALUES ('Cholesterol (Total)', 'mg/dL', 0, 200, 1);

-- Password is '1234' (hashed with BCrypt)
INSERT INTO Staff (FullName, Role, PinHash) VALUES ('Dr. Robert Brown', 'Admin', '$2a$11$/kj.NC923I71HcIDmIOASeJhA7Il5NLBh6Mb/nO8Thz/J2ooDHwIC');
INSERT INTO Staff (FullName, Role, PinHash) VALUES ('Tech Sarah', 'Technician', '$2a$11$/kj.NC923I71HcIDmIOASeJhA7Il5NLBh6Mb/nO8Thz/J2ooDHwIC');
