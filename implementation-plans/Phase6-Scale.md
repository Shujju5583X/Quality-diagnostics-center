# Phase 6 — Scale Features

## Objective
Add multi-branch support, migrate from SQLite to a production database, and expose a REST API for external integrations.

## Prerequisites
- Phase 3, 4, and 5 completed
- All core features stable and tested
- Decision on target database (PostgreSQL recommended for cost + features)

---

## Feature 1: Multi-Branch Support

### What
Support multiple lab branches with shared data but branch-specific isolation (patients, orders, staff per branch).

### Files to Create/Modify

| Layer | File | Change |
|-------|------|--------|
| Core | `LabSystem.Core/Models/Branch.cs` | **NEW** — branch entity |
| Core | `LabSystem.Core/Models/Patient.cs` | Add `BranchId` FK |
| Core | `LabSystem.Core/Models/TestOrder.cs` | Add `BranchId` FK |
| Core | `LabSystem.Core/Models/Staff.cs` | Add `BranchId` FK |
| Data | `LabSystem.Data/Migrations/V1__init.sql` | Add `Branches` table, add `BranchId` columns |
| Data | `LabSystem.Data/DatabaseInitializer.cs` | Add migration for BranchId columns |
| UI | `LabSystem.UI/App.xaml.cs` | Add branch selection at startup |
| UI | `LabSystem.UI/ViewModels/LoginViewModel.cs` | Show branch selector before login |

### Database Schema
```sql
CREATE TABLE IF NOT EXISTS Branches (
    BranchId INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Address TEXT,
    Phone TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt DATETIME
);

-- Add BranchId to existing tables
ALTER TABLE Patients ADD COLUMN BranchId INTEGER DEFAULT 1;
ALTER TABLE TestOrders ADD COLUMN BranchId INTEGER DEFAULT 1;
ALTER TABLE Staff ADD COLUMN BranchId INTEGER DEFAULT 1;
ALTER TABLE Invoices ADD COLUMN BranchId INTEGER DEFAULT 1;
```

### Branch Model
```csharp
public class Branch
{
    public int BranchId { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public string Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
```

### Branch Isolation Strategy
- Add `BranchId` filter to all repository queries
- Store current branch in `App.CurrentBranchId` (static)
- All data operations automatically filter by current branch
- Reports can optionally aggregate across branches

### Repository Query Pattern
```csharp
// Example: PatientRepository
public async Task<IEnumerable<Patient>> GetAllAsync(CancellationToken cancellationToken = default)
{
    return await _dbSet.AsNoTracking()
        .Where(p => p.BranchId == App.CurrentBranchId)
        .ToListAsync(cancellationToken);
}
```

### UI: Branch Selection at Startup
Modify `LoginView.xaml` to show a branch dropdown before PIN entry:
```xml
<ComboBox ItemsSource="{Binding Branches}" DisplayMemberPath="Name"
          SelectedItem="{Binding SelectedBranch}" Margin="0 0 0 16"/>
```

### Tests
- Test data isolation between branches
- Test branch switching loads correct data
- Test cross-branch reporting (admin view)

---

## Feature 2: Database Migration (SQLite → PostgreSQL)

### What
Provide a migration path from SQLite to PostgreSQL for production deployment with better concurrency, scalability, and backup support.

### Strategy
Use Entity Framework 6's database-agnostic features. The existing EF6 code should work with PostgreSQL via the `Npgsql.EntityFrameworkCore.PostgreSQL` provider (or `Npgsql` for EF6).

### Files to Create/Modify

| Layer | File | Change |
|-------|------|--------|
| Data | `LabSystem.Data/LabDbContext.cs` | Abstract connection string, support multiple providers |
| Data | `LabSystem.Data/MigrationRunner.cs` | **NEW** — data migration utility |
| Root | `appsettings.json` | **NEW** — configurable connection string |
| Root | `LabSystem.UI/App.config` | Update to read from appsettings |

### Migration Approach: Dual-Write Export/Import
Since this is a desktop app with existing data, the safest approach is:

1. **Export from SQLite** — read all data into memory/models
2. **Create PostgreSQL schema** — run EF migrations against PostgreSQL
3. **Import to PostgreSQL** — bulk insert all exported data
4. **Switch connection string** — point app to PostgreSQL

### MigrationRunner.cs
```csharp
public class MigrationRunner
{
    public static async Task MigrateSqliteToPostgres(string sqlitePath, string postgresConnectionString)
    {
        // 1. Read all data from SQLite
        using var sqliteContext = new LabDbContext(sqlitePath);
        var patients = await sqliteContext.Patients.ToListAsync();
        var orders = await sqliteContext.TestOrders.ToListAsync();
        var results = await sqliteContext.Results.ToListAsync();
        // ... etc for all tables

        // 2. Create PostgreSQL context
        using var pgContext = new LabDbContext(postgresConnectionString);
        await pgContext.Database.CreateIfNotExistsAsync();

        // 3. Bulk insert (with batching for performance)
        pgContext.BulkInsert(patients);  // Use EF6 Extensions or custom batcher
        pgContext.BulkInsert(orders);
        pgContext.BulkInsert(results);
        // ... etc

        await pgContext.SaveChangesAsync();
    }
}
```

### Connection String Configuration
```json
// appsettings.json
{
  "Database": {
    "Provider": "SQLite",  // or "PostgreSQL"
    "ConnectionString": "Data Source=lab.db"
    // PostgreSQL: "Host=localhost;Port=5432;Database=lab;Username=user;Password=pass"
  }
}
```

### Tests
- Test SQLite export reads all records
- Test PostgreSQL import preserves data integrity
- Test foreign key relationships after migration
- Test auto-increment sequences are correct after import

---

## Feature 3: REST API Layer

### What
Expose a REST API for external integrations (mobile app, third-party lab systems, referral portals).

### Files to Create

| Layer | File | Change |
|-------|------|--------|
| Root | `LabSystem.Api/` | **NEW** — ASP.NET Web API project |
| Root | `LabSystem.Api/Controllers/PatientsController.cs` | **NEW** |
| Root | `LabSystem.Api/Controllers/OrdersController.cs` | **NEW** |
| Root | `LabSystem.Api/Controllers/ResultsController.cs` | **NEW** |
| Root | `LabSystem.Api/Controllers/ReportsController.cs` | **NEW** |
| Core | `LabSystem.Core/Interfaces/IApiService.cs` | **NEW** — API business logic |

### API Project Setup
```
LabSystem.Api/
├── Controllers/
│   ├── PatientsController.cs
│   ├── OrdersController.cs
│   ├── ResultsController.cs
│   └── ReportsController.cs
├── Models/
│   ├── PatientDto.cs
│   ├── OrderDto.cs
│   └── ResultDto.cs
├── Middleware/
│   └── ApiKeyAuthMiddleware.cs
├── Program.cs
└── LabSystem.Api.csproj
```

### API Endpoints
```
GET    /api/patients              — List patients (paginated, filterable)
GET    /api/patients/{id}         — Get patient details
GET    /api/patients/{id}/orders  — Get patient orders
GET    /api/patients/{id}/history — Get patient result history

GET    /api/orders                — List orders
GET    /api/orders/{id}           — Get order details
GET    /api/orders/{id}/results   — Get order results
POST   /api/orders                — Create new order

GET    /api/results/{id}          — Get result details
PUT    /api/results/{id}/amend    — Amend a result

GET    /api/reports/{orderId}/pdf — Download PDF report
GET    /api/reports/{orderId}/invoice — Download invoice PDF

GET    /api/testtypes             — List available test types
GET    /api/testpanels            — List test panels
```

### DTO Models
```csharp
public class PatientDto
{
    public int Id { get; set; }
    public string Uhid { get; set; }
    public string FullName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string Gender { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; }
    public DateTime OrderedAt { get; set; }
    public string Status { get; set; }
    public List<TestTypeDto> Tests { get; set; }
    public List<SpecimenDto> Specimens { get; set; }
}

public class ResultDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string TestName { get; set; }
    public double? Value { get; set; }
    public string ValueText { get; set; }
    public string Unit { get; set; }
    public bool IsAbnormal { get; set; }
    public bool IsAmended { get; set; }
    public string AmendmentReason { get; set; }
    public DateTime RecordedAt { get; set; }
}
```

### API Key Authentication (ApiKeyAuthMiddleware.cs)
```csharp
public class ApiKeyAuthMiddleware
{
    private const string API_KEY_HEADER = "X-Api-Key";
    private readonly RequestDelegate _next;
    private readonly string _validApiKey;

    public ApiKeyAuthMiddleware(RequestDelegate next, string validApiKey)
    {
        _next = next;
        _validApiKey = validApiKey;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API Key missing.");
            return;
        }

        if (!_validApiKey.Equals(extractedApiKey))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Invalid API Key.");
            return;
        }

        await _next(context);
    }
}
```

### Program.cs (Minimal API Setup)
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register same DI as main app
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<ITestOrderRepository, TestOrderRepository>();
// ... etc

var app = builder.Build();
app.UseMiddleware<ApiKeyAuthMiddleware>(builder.Configuration["ApiKey"]);
app.MapControllers();
app.Run();
```

### Tests
- Test API returns patient list (200 OK)
- Test API returns 401 without API key
- Test API creates order (201 Created)
- Test API downloads PDF report
- Test pagination works correctly

---

## Effort Estimate

| Feature | Days |
|---------|------|
| Multi-Branch Support | 2 |
| Database Migration (SQLite → PostgreSQL) | 2 |
| REST API Layer | 3 |
| Testing | 1 |
| **Total** | **8 days** |

## Verification

After completing Phase 6:
1. `dotnet build` — 0 errors for all projects
2. `dotnet test` — all tests pass (main + API)
3. Manual: Switch branch → verify data isolation
4. Manual: Run migration tool → verify data transfers correctly
5. Manual: Start API project → call `GET /api/patients` → verify JSON response
6. Manual: Call `GET /api/reports/{id}/pdf` → verify PDF download

---

## Post-Phase 6: Production Readiness Checklist

Before deploying to production:
- [ ] Replace hardcoded API keys with secure key vault
- [ ] Add HTTPS enforcement
- [ ] Add rate limiting on API endpoints
- [ ] Add database connection pooling
- [ ] Set up automated backups (pg_dump cron)
- [ ] Add application logging to file/event log
- [ ] Create deployment scripts (Docker / Windows Service)
- [ ] Write user manual / training documentation
