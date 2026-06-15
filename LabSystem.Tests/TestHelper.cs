using LabSystem.Core;
using LabSystem.Data;

namespace LabSystem.Tests
{
    public static class TestHelper
    {
        public static string FindFileUpwards(params string[] pathParts)
        {
            return FileUtilities.FindFileUpwards(pathParts);
        }

        public static void InitializeTestDatabase(LabDbContext context)
        {
            context.Database.ExecuteSqlCommand(@"
                CREATE TABLE IF NOT EXISTS Patients (
                    PatientId INTEGER PRIMARY KEY AUTOINCREMENT,
                    FullName TEXT NOT NULL,
                    Age INTEGER NOT NULL DEFAULT 0,
                    ContactPhone TEXT,
                    ContactEmail TEXT,
                    CreatedAt DATETIME NOT NULL,
                    Gender TEXT,
                    Uhid TEXT UNIQUE,
                    BranchId INTEGER DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS Departments (
                    DepartmentId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE
                );

                CREATE TABLE IF NOT EXISTS Doctors (
                    DoctorId INTEGER PRIMARY KEY AUTOINCREMENT,
                    FullName TEXT NOT NULL,
                    ContactPhone TEXT NOT NULL,
                    Commission REAL NOT NULL DEFAULT 0.0,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
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
                    InputType INTEGER DEFAULT 0,
                    DepartmentId INTEGER REFERENCES Departments(DepartmentId) ON DELETE SET NULL
                );

                CREATE TABLE IF NOT EXISTS Staff (
                    StaffId INTEGER PRIMARY KEY AUTOINCREMENT,
                    FullName TEXT NOT NULL,
                    Role TEXT,
                    PinHash TEXT,
                    FailedLoginAttempts INTEGER DEFAULT 0,
                    LockoutEnd DATETIME,
                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    BranchId INTEGER DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS TestOrders (
                    OrderId INTEGER PRIMARY KEY AUTOINCREMENT,
                    PatientId INTEGER NOT NULL,
                    DoctorId INTEGER REFERENCES Doctors(DoctorId) ON DELETE SET NULL,
                    OrderedAt DATETIME NOT NULL,
                    Status TEXT,
                    Notes TEXT,
                    ReferredBy TEXT,
                    CreatedAt DATETIME NOT NULL,
                    UpdatedAt DATETIME NOT NULL,
                    BranchId INTEGER DEFAULT 1,
                    FOREIGN KEY(PatientId) REFERENCES Patients(PatientId)
                );

                CREATE TABLE IF NOT EXISTS OrderTestTypes (
                    OrderId INTEGER NOT NULL,
                    TypeId INTEGER NOT NULL,
                    PRIMARY KEY (OrderId, TypeId),
                    FOREIGN KEY(OrderId) REFERENCES TestOrders(OrderId) ON DELETE CASCADE,
                    FOREIGN KEY(TypeId) REFERENCES TestTypes(TypeId) ON DELETE CASCADE
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
                    AmountPaid REAL NOT NULL DEFAULT 0.0,
                    DiscountAmount REAL DEFAULT 0,
                    TaxAmount REAL DEFAULT 0,
                    DiscountPercent REAL DEFAULT 0,
                    TaxPercent REAL DEFAULT 0,
                    IsPaid INTEGER DEFAULT 0,
                    PaidAt DATETIME,
                    CreatedAt DATETIME NOT NULL,
                    UpdatedAt DATETIME,
                    PaymentMethod TEXT,
                    BranchId INTEGER DEFAULT 1,
                    FOREIGN KEY(OrderId) REFERENCES TestOrders(OrderId)
                );

                CREATE TABLE IF NOT EXISTS Payments (
                    PaymentId INTEGER PRIMARY KEY AUTOINCREMENT,
                    InvoiceId INTEGER NOT NULL,
                    Amount REAL NOT NULL,
                    PaymentMethod TEXT,
                    PaymentDate DATETIME NOT NULL,
                    FOREIGN KEY(InvoiceId) REFERENCES Invoices(InvoiceId) ON DELETE CASCADE
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

                CREATE TABLE IF NOT EXISTS QcRuns (
                    QcRunId INTEGER PRIMARY KEY AUTOINCREMENT,
                    TestTypeId INTEGER NOT NULL,
                    ControlName TEXT NOT NULL,
                    RunDate DATETIME NOT NULL,
                    MeasuredValue REAL NOT NULL,
                    LotNumber TEXT,
                    TargetValue REAL,
                    SD REAL,
                    CreatedAt DATETIME,
                    FOREIGN KEY(TestTypeId) REFERENCES TestTypes(TypeId)
                );

                CREATE TABLE IF NOT EXISTS QcLots (
                    QcLotId INTEGER PRIMARY KEY AUTOINCREMENT,
                    TestTypeId INTEGER NOT NULL,
                    LotNumber TEXT NOT NULL,
                    TargetValue REAL NOT NULL,
                    SD REAL NOT NULL,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedAt DATETIME,
                    FOREIGN KEY(TestTypeId) REFERENCES TestTypes(TypeId)
                );

                CREATE TABLE IF NOT EXISTS Appointments (
                    AppointmentId INTEGER PRIMARY KEY AUTOINCREMENT,
                    PatientId INTEGER NOT NULL,
                    AppointmentDate DATETIME NOT NULL,
                    DurationMinutes INTEGER NOT NULL DEFAULT 15,
                    Purpose TEXT,
                    Status TEXT NOT NULL DEFAULT 'Scheduled',
                    Notes TEXT,
                    CreatedAt DATETIME,
                    UpdatedAt DATETIME,
                    FOREIGN KEY(PatientId) REFERENCES Patients(PatientId)
                );

                CREATE TABLE IF NOT EXISTS Branches (
                    BranchId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Address TEXT,
                    Phone TEXT,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedAt DATETIME
                );

                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT
                );

                CREATE INDEX IF NOT EXISTS IX_TestOrders_PatientId ON TestOrders (PatientId);
                CREATE INDEX IF NOT EXISTS IX_Results_OrderId ON Results (OrderId);
                CREATE INDEX IF NOT EXISTS IX_Results_TypeId ON Results (TypeId);
                CREATE INDEX IF NOT EXISTS IX_Results_TechnicianId ON Results (TechnicianId);
                CREATE INDEX IF NOT EXISTS IX_Reports_OrderId ON Reports (OrderId);
                CREATE INDEX IF NOT EXISTS IX_Specimens_OrderId ON Specimens (OrderId);
                CREATE INDEX IF NOT EXISTS IX_ReferenceRanges_TestTypeId ON ReferenceRanges (TestTypeId);
                CREATE INDEX IF NOT EXISTS IX_Payments_InvoiceId ON Payments (InvoiceId);
                CREATE INDEX IF NOT EXISTS IX_QcRuns_TestTypeId ON QcRuns (TestTypeId);
                CREATE INDEX IF NOT EXISTS IX_QcRuns_RunDate ON QcRuns (RunDate);
                CREATE INDEX IF NOT EXISTS IX_QcLots_TestTypeId ON QcLots (TestTypeId);
                CREATE INDEX IF NOT EXISTS IX_Appointments_AppointmentDate ON Appointments (AppointmentDate);
            ");
        }
    }
}

