CREATE TABLE IF NOT EXISTS Patients (
    PatientId INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName TEXT NOT NULL,
    DateOfBirth TEXT,
    ContactPhone TEXT,
    ContactEmail TEXT,
    CreatedAt TEXT NOT NULL,
    Gender TEXT
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
    Price REAL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS Staff (
    StaffId INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName TEXT NOT NULL,
    Role TEXT,
    PinHash TEXT NOT NULL,
    FailedLoginAttempts INTEGER DEFAULT 0,
    LockoutEnd TEXT
);

CREATE TABLE IF NOT EXISTS TestOrders (
    OrderId INTEGER PRIMARY KEY AUTOINCREMENT,
    PatientId INTEGER NOT NULL,
    OrderedAt TEXT NOT NULL,
    Status TEXT,
    Notes TEXT,
    ReferredBy TEXT,
    FOREIGN KEY(PatientId) REFERENCES Patients(PatientId)
);

CREATE TABLE IF NOT EXISTS Invoices (
    InvoiceId INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderId INTEGER NOT NULL UNIQUE,
    TotalAmount REAL NOT NULL,
    IsPaid INTEGER DEFAULT 0,
    PaidAt TEXT,
    CreatedAt TEXT NOT NULL,
    PaymentMethod TEXT,
    FOREIGN KEY(OrderId) REFERENCES TestOrders(OrderId)
);

CREATE TABLE IF NOT EXISTS Results (
    ResultId INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderId INTEGER NOT NULL,
    TypeId INTEGER NOT NULL,
    Value REAL NOT NULL,
    RecordedAt TEXT NOT NULL,
    TechnicianId INTEGER NOT NULL,
    IsAbnormal INTEGER NOT NULL,
    FOREIGN KEY(OrderId) REFERENCES TestOrders(OrderId),
    FOREIGN KEY(TypeId) REFERENCES TestTypes(TypeId),
    FOREIGN KEY(TechnicianId) REFERENCES Staff(StaffId)
);

CREATE TABLE IF NOT EXISTS Reports (
    ReportId INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderId INTEGER NOT NULL,
    FilePath TEXT NOT NULL,
    GeneratedAt TEXT NOT NULL,
    FOREIGN KEY(OrderId) REFERENCES TestOrders(OrderId)
);

CREATE TABLE IF NOT EXISTS AuditLogs (
    LogId INTEGER PRIMARY KEY AUTOINCREMENT,
    Action TEXT NOT NULL,
    EntityType TEXT,
    EntityId INTEGER,
    UserId INTEGER,
    Timestamp DATETIME NOT NULL,
    Details TEXT,
    FOREIGN KEY(UserId) REFERENCES Staff(StaffId)
);

CREATE INDEX IF NOT EXISTS IX_TestOrders_PatientId ON TestOrders (PatientId);
CREATE INDEX IF NOT EXISTS IX_Results_OrderId ON Results (OrderId);
CREATE INDEX IF NOT EXISTS IX_Results_TypeId ON Results (TypeId);
CREATE INDEX IF NOT EXISTS IX_Results_TechnicianId ON Results (TechnicianId);
CREATE INDEX IF NOT EXISTS IX_Reports_OrderId ON Reports (OrderId);
CREATE INDEX IF NOT EXISTS IX_AuditLogs_UserId ON AuditLogs (UserId);
