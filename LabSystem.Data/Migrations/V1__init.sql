CREATE TABLE IF NOT EXISTS Patients (
    PatientId INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName TEXT NOT NULL,
    DateOfBirth DATETIME,
    ContactPhone TEXT,
    ContactEmail TEXT,
    CreatedAt DATETIME NOT NULL,
    Gender TEXT,
    Uhid TEXT UNIQUE
);

CREATE TABLE IF NOT EXISTS TestTypes (
    TypeId INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Unit TEXT,
    ReferenceRangeLow REAL,
    ReferenceRangeHigh REAL,
    IsActive INTEGER DEFAULT 1,
    Category TEXT,
    GroupName TEXT,
    Method TEXT,
    Interpretation TEXT,
    SortOrder INTEGER DEFAULT 0,
    Price REAL DEFAULT 0,
    SampleType TEXT,
    InputType INTEGER DEFAULT 0
);

CREATE TABLE IF NOT EXISTS Staff (
    StaffId INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName TEXT NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS TestOrders (
    OrderId INTEGER PRIMARY KEY AUTOINCREMENT,
    PatientId INTEGER NOT NULL,
    OrderedAt DATETIME NOT NULL,
    Status TEXT,
    Notes TEXT,
    ReferredBy TEXT,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    FOREIGN KEY(PatientId) REFERENCES Patients(PatientId)
);

CREATE TABLE IF NOT EXISTS OrderTestTypes (
    OrderId INTEGER NOT NULL,
    TypeId INTEGER NOT NULL,
    PackageId INTEGER,
    BilledCost REAL NOT NULL DEFAULT 0.0,
    PRIMARY KEY (OrderId, TypeId),
    FOREIGN KEY(OrderId) REFERENCES TestOrders(OrderId) ON DELETE CASCADE,
    FOREIGN KEY(TypeId) REFERENCES TestTypes(TypeId) ON DELETE CASCADE,
    FOREIGN KEY(PackageId) REFERENCES TestPanels(PanelId)
);

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

CREATE TABLE IF NOT EXISTS TestPanels (
    PanelId INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT,
    Price REAL NOT NULL
);

CREATE TABLE IF NOT EXISTS PanelTestTypes (
    PanelId INTEGER NOT NULL,
    TypeId INTEGER NOT NULL,
    PRIMARY KEY (PanelId, TypeId),
    FOREIGN KEY(PanelId) REFERENCES TestPanels(PanelId) ON DELETE CASCADE,
    FOREIGN KEY(TypeId) REFERENCES TestTypes(TypeId) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS Invoices (
    InvoiceId INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderId INTEGER NOT NULL UNIQUE,
    TotalAmount REAL NOT NULL,
    DiscountAmount REAL DEFAULT 0,
    TaxAmount REAL DEFAULT 0,
    Status TEXT DEFAULT 'Pending' CHECK (Status IN ('Pending', 'Partial', 'Paid')),
    IsPaid INTEGER DEFAULT 0,
    PaidAt DATETIME,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME,
    PaymentMethod TEXT,
    FOREIGN KEY(OrderId) REFERENCES TestOrders(OrderId)
);

CREATE TABLE IF NOT EXISTS Results (
    ResultId INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderId INTEGER NOT NULL,
    TypeId INTEGER NOT NULL,
    Value REAL,
    ValueText TEXT,
    RecordedAt DATETIME NOT NULL,
    TechnicianId INTEGER NOT NULL,
    IsAbnormal INTEGER NOT NULL,
    CreatedAt DATETIME,
    UpdatedAt DATETIME,
    IsAmended INTEGER NOT NULL DEFAULT 0,
    AmendmentReason TEXT,
    AmendedAt DATETIME,
    FOREIGN KEY(OrderId) REFERENCES TestOrders(OrderId),
    FOREIGN KEY(TypeId) REFERENCES TestTypes(TypeId),
    FOREIGN KEY(TechnicianId) REFERENCES Staff(StaffId)
);

CREATE TABLE IF NOT EXISTS Reports (
    ReportId INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderId INTEGER NOT NULL,
    FilePath TEXT NOT NULL,
    GeneratedAt DATETIME NOT NULL,
    FOREIGN KEY(OrderId) REFERENCES TestOrders(OrderId)
);

CREATE TABLE IF NOT EXISTS SchemaVersion (
    Version INTEGER PRIMARY KEY,
    AppliedAt DATETIME NOT NULL
);
INSERT INTO SchemaVersion (Version, AppliedAt) VALUES (1, CURRENT_TIMESTAMP);

CREATE TABLE IF NOT EXISTS DoctorCommissions (
    CommissionId INTEGER PRIMARY KEY AUTOINCREMENT,
    DoctorId INTEGER NOT NULL,
    InvoiceId INTEGER NOT NULL,
    CommissionAmount REAL NOT NULL,
    Status TEXT NOT NULL DEFAULT 'Unpaid' CHECK (Status IN ('Unpaid', 'Paid')),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(DoctorId) REFERENCES Doctors(DoctorId),
    FOREIGN KEY(InvoiceId) REFERENCES Invoices(InvoiceId)
);

CREATE INDEX IF NOT EXISTS IX_TestOrders_PatientId ON TestOrders (PatientId);
CREATE INDEX IF NOT EXISTS IX_Results_OrderId ON Results (OrderId);
CREATE INDEX IF NOT EXISTS IX_Results_TypeId ON Results (TypeId);
CREATE INDEX IF NOT EXISTS IX_Results_TechnicianId ON Results (TechnicianId);
CREATE INDEX IF NOT EXISTS IX_Reports_OrderId ON Reports (OrderId);
CREATE INDEX IF NOT EXISTS IX_Specimens_OrderId ON Specimens (OrderId);
CREATE INDEX IF NOT EXISTS IX_ReferenceRanges_TestTypeId ON ReferenceRanges (TestTypeId);
