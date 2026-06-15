# Project Review — Quality Diagnostics Center

## What This Project Is (in Plain English)

A **Windows desktop app** (WPF) for a single lab technician to:

- Register patients
- Order lab tests
- Enter and print results
- Generate invoices and collect payments
- Run backups

The architecture is well-chosen for this purpose. SQLite is the right database, WPF is suitable for a local Windows desktop app, and the layered design (Core → Data → Services → UI) is correct. You've assembled something that genuinely works.

The main problem is that **tutorials teach enterprise patterns** and you've applied all of them — even the ones your friend's single-person lab doesn't need. That created unnecessary complexity.

---

## ✅ What's Working Well (Keep These)

| Component | Why It's Good |
| --------- | ------------- |
| **Layered architecture** (Core/Data/Services/UI) | Correct. Keeps business logic out of the UI. |
| **Entity Framework 6 + SQLite** | Right tool for a local, single-file database. |
| **BCrypt PIN hashing** in `StaffService` | Proper approach for storing credentials. |
| **DPAPI-encrypted `key.dat`** in `SecureConfigurationManager` | Good use of OS-level key storage for the DB encryption key. |
| **Serilog with rolling file logs** | Practical diagnostics for a deployed desktop app. |
| **Auto-backup on exit** (SQLite + Excel) | Excellent for a medical setting — critical safety net. |
| **`WorkflowService.QuickFinalizeAsync`** | Well-designed: runs results + billing in a DB transaction, PDF outside. |
| **NUnit test suite** | Good coverage across services. |
| **Partial class split of `DashboardViewModel`** | Reasonable way to manage a large VM. |
| **Material Design theming** | Clean, professional UI. |

---

## ❌ Redundant / Unnecessary Elements

### 1. The Entire `LabSystem.Api` Project — Remove It

**Files:** `LabSystem.Api/` (the entire project + its entry in `LabSystem.sln`)

This is a full ASP.NET Core REST API with 3 controllers, API key auth middleware, CORS configuration, and its own DI setup. It was never connected to the WPF UI, has no consumer, and conflicts with the desktop-first design.

For a single-person local lab, there is no scenario where a REST API running on localhost adds value. It's dead code that adds build time, a second `Program.cs` to maintain, and real security surface area (the open CORS wildcard).

**Recommendation:** Delete `LabSystem.Api/` and remove it from the solution. If your friend ever needs remote access, revisit this decision then.

---

### 2. `MigrationRunner.cs` — Remove or Label Clearly

**File:** [`LabSystem.Data/MigrationRunner.cs`](file:///E:/Quality%20diagnostics%20center/LabSystem.Data/MigrationRunner.cs)

The `MigrateSqliteToPostgresAsync` method reads data from SQLite but then logs `"Import to PostgreSQL requires Npgsql provider."` and stops — it's an unfinished stub that does nothing. The `ExportToCsvAsync` method is useful but is never called anywhere in the app.

**Recommendation:** Delete `MigrateSqliteToPostgresAsync`. Move `ExportToCsvAsync` into `SqliteBackupService` where it belongs, and wire it up to the Backup button if CSV export is desired.

---

### 3. The Four "Pass-Through" Tab ViewModels — Collapse Them

**Files:**

- [`LabSystem.UI/ViewModels/PatientsTabViewModel.cs`](file:///E:/Quality%20diagnostics%20center/LabSystem.UI/ViewModels/PatientsTabViewModel.cs) (15 lines, 1 property)
- [`LabSystem.UI/ViewModels/OrdersTabViewModel.cs`](file:///E:/Quality%20diagnostics%20center/LabSystem.UI/ViewModels/OrdersTabViewModel.cs) (19 lines, 3 properties)
- [`LabSystem.UI/ViewModels/LabTabViewModel.cs`](file:///E:/Quality%20diagnostics%20center/LabSystem.UI/ViewModels/LabTabViewModel.cs) (20 lines, 3 properties)
- [`LabSystem.UI/ViewModels/BillingTabViewModel.cs`](file:///E:/Quality%20diagnostics%20center/LabSystem.UI/ViewModels/BillingTabViewModel.cs) (19 lines, 2 properties)

These are not real ViewModels. They are simple data bags created only so `DashboardViewModel`'s constructor can receive grouped parameters. The `DashboardViewModel` then immediately unpacks them:

```csharp
_patientRepo = patientsTabVM.PatientRepo;
_orderRepo = ordersTabVM.OrderRepo;
// etc.
```

This is circular complexity — extra classes that exist purely to work around the fact that `DashboardViewModel` has too many constructor parameters.

**Recommendation:** Delete all four. Inject the repositories and services directly into `DashboardViewModel`. SimpleInjector handles large constructor injection just fine.

---

### 4. The `tools/` Directory — Remove from Git

**Files:** `tools/check_db.cs`, `tools/test.cs`, `tools/test.exe`, `tools/find_dbs.py`, `tools/list_tables.py`, `tools/query_db.py`, `tools/seed_db.py`, `tools/refactor_dashboard.py`

These are personal debugging and scaffolding scripts you ran while building the project. They contain hardcoded absolute paths (`E:\Quality diagnostics center\...`), compiled binaries (`.exe` in git), and code that duplicates what the app does automatically. None of them should be in the repository.

**Recommendation:** Delete `tools/` from the repository. Add `tools/` to `.gitignore` if you want to keep local copies.

---

### 5. `System.Data.SQLite.dll.bak` in the Root — Remove

**File:** [`System.Data.SQLite.dll.bak`](file:///E:/Quality%20diagnostics%20center/System.Data.SQLite.dll.bak) (441 KB)

A backup of a NuGet-managed DLL sitting in your project root. NuGet restores this automatically — there's no reason to keep a manual copy in source control.

**Recommendation:** Delete it. Add `*.bak` to `.gitignore`.

---

### 6. `InternalTrace.23604.log` in `LabSystem.Tests/` — Remove

**File:** `LabSystem.Tests/InternalTrace.23604.log`

An NUnit internal trace log committed by accident. This is a runtime output file, not source code.

**Recommendation:** Delete it. Add `*.log` to `.gitignore` for the Tests project too (it already is for the root, but the Tests folder may not be covered).

---

## ⚠️ Specific Problems to Fix

### Problem 1 — "God ViewModel": `DashboardViewModel` Does Too Much

`DashboardViewModel.cs` has **550 lines in the main file alone**, plus 6 more partial class files. It manages patients, orders, results, billing, revenue reports, catalog management, and patient history — all at once. Every UI operation fires from a single God object.

This isn't catastrophic (the partial class split helps), but it makes changes risky. Editing the billing code requires opening the same class that controls patient registration.

**Recommendation (Medium term):** The partial class split is the right direction. Make sure each partial file has a clearly defined scope and doesn't reach across into another's state. Eventually, extract `QcViewModel`, `AppointmentsViewModel`, and `StaffManagementViewModel` as the pattern shows — they're already separate.

---

### Problem 2 — SMS Credentials Are Never Read from Config

> [!NOTE]
> SMS has since been removed from the project entirely. This problem no longer applies.

---

### Problem 3 — Hardcoded Fallback PIN `"1234"`

In [`LoginViewModel.cs` line 121](file:///E:/Quality%20diagnostics%20center/LabSystem.UI/ViewModels/LoginViewModel.cs#L121):

```csharp
isPinValid = (Pin == "1234");
```

Any account without a PIN set bypasses BCrypt and accepts `1234`. This undermines the lockout mechanism.

**Fix:** Replace with a forced PIN enrollment flow. If `PinHash` is null/empty, show a "Set up your PIN" screen instead of granting access.

---

### Problem 4 — Hardcoded Default Staff BCrypt Hash in Source

In [`DatabaseInitializer.cs` line 37](file:///E:/Quality%20diagnostics%20center/LabSystem.Data/DatabaseInitializer.cs#L37):

```csharp
"INSERT INTO Staff (FullName, PinHash) VALUES ('Lab Technician', '$2a$11$kqAe...');"
```

A known BCrypt hash is committed to the repository. Anyone with the source code can attempt offline cracking.

**Fix:** Seed the staff row **without** a PinHash. When the app starts and detects no PinHash on any account, redirect to a first-run PIN setup screen.

---

### Problem 5 — `appsettings.json` Not in `.gitignore`

The root `appsettings.json` contains the API key placeholder. It's tracked by git. If you ever put real credentials in it, they'll be committed.

**Fix:** Add to `.gitignore`:

```gitignore
appsettings.json
appsettings.*.json
*.bak
tools/
InternalTrace*.log
```

Keep an `appsettings.example.json` in git showing what the structure should look like.

---

### Problem 6 — `RefreshDashboardStatsAsync` Loads All Results

In [`DashboardViewModel.cs` line 528](file:///E:/Quality%20diagnostics%20center/LabSystem.UI/ViewModels/DashboardViewModel.cs#L528):

```csharp
var results = await _resultRepo.GetAllAsync();
AbnormalResultsFlagged = results.Count(r => r.IsAbnormal);
```

Every dashboard refresh loads **every result** from the database into memory just to count abnormal ones. This will get slower as data grows.

**Fix:** Add a `CountAbnormalAsync()` method to `IResultRepository` that runs a SQL `COUNT` query directly.

---

## 📋 Recommended Development Workflow

Here's a clear, phased plan to clean up the project and make it easier to manage going forward.

---

### Phase 1 — Immediate Cleanup (1–2 hours)

These are safe deletions that simplify the project immediately:

- [ ] **Delete `LabSystem.Api/`** and remove it from `LabSystem.sln`
- [ ] **Delete `tools/`** directory entirely
- [ ] **Delete `System.Data.SQLite.dll.bak`** from the root
- [ ] **Delete `LabSystem.Tests/InternalTrace.23604.log`**
- [ ] **Delete `MigrationRunner.MigrateSqliteToPostgresAsync`** (the stub method)
- [ ] **Update `.gitignore`** to add: `appsettings.json`, `*.bak`, `tools/`, `InternalTrace*.log`
- [ ] **Move `Sample_Verification_Report.pdf`** into the `Sample reports/` folder

---

### Phase 2 — Security Fixes (2–4 hours)

These fix active security issues without changing application behavior:

- [ ] **Remove hardcoded default PIN fallback** (`"1234"`) from `LoginViewModel`; add first-run PIN enrollment screen
- [ ] **Remove hardcoded BCrypt hash** from `DatabaseInitializer`; seed staff with null PinHash, detect and redirect to setup
- [ ] **Replace `RNGCryptoServiceProvider`** with `RandomNumberGenerator.GetBytes(32)` in `SecureConfigurationManager`

---

### Phase 3 — Code Simplification (4–8 hours)

These reduce complexity without changing features:

- [ ] **Delete the four pass-through Tab ViewModels** (`PatientsTabViewModel`, `OrdersTabViewModel`, `LabTabViewModel`, `BillingTabViewModel`). Inject dependencies directly into `DashboardViewModel`.
- [ ] **Add `CountAbnormalAsync()`** to `IResultRepository` / `ResultRepository` to replace the in-memory count in dashboard stats
- [ ] **Move `ExportToCsvAsync`** from `MigrationRunner` into `SqliteBackupService` (optional feature, wire to backup button)
- [ ] **Add `appsettings.example.json`** to document what config keys are needed, then gitignore the real file

---

### Phase 4 — Ongoing Workflow Rules

To keep the project clean going forward:

1. **One commit per feature.** Don't commit debug scripts or `.log` files. Run `git status` before committing.
2. **Test before committing.** Run `dotnet test` — the test suite is already good.
3. **Backup is automatic on exit.** Don't manually copy `lab.db`. Let the backup service do its job.
4. **No new projects without a use case.** Before adding `LabSystem.SomeNewProject`, ask: is it actually used? The API project is the lesson here.
5. **Keep `DashboardViewModel` partials scoped.** Each `.cs` file should only touch its own fields and services. Don't let `DashboardViewModel.Orders.cs` call billing methods.

---

## Summary Table

| Category | Item | Action |
| -------- | ---- | ------ |
| Redundant | `LabSystem.Api/` (entire project) | 🗑️ Delete |
| Redundant | 4 pass-through Tab ViewModels | 🗑️ Delete & consolidate |
| Redundant | `tools/` directory | 🗑️ Delete from git |
| Redundant | `System.Data.SQLite.dll.bak` | 🗑️ Delete |
| Redundant | `MigrationRunner.MigrateSqliteToPostgresAsync` | 🗑️ Delete stub |
| Redundant | `InternalTrace.23604.log` | 🗑️ Delete |
| Security | Fallback PIN `"1234"` | 🔧 Replace with enrollment |
| Security | Hardcoded BCrypt hash | 🔧 Remove from seed |
| ~~Security~~ | ~~SMS keys not wired~~ | ✅ SMS removed |
| Security | `RNGCryptoServiceProvider` deprecated | 🔧 Update API |
| Security | `appsettings.json` not gitignored | 🔧 Update `.gitignore` |
| Performance | `GetAllAsync()` for counting abnormals | 🔧 Add COUNT query |
| Keep | Layered architecture | ✅ |
| Keep | BCrypt PIN hashing | ✅ |
| Keep | DPAPI DB encryption | ✅ |
| Keep | Auto-backup on exit | ✅ |
| Keep | WorkflowService transaction pattern | ✅ |
| Keep | NUnit test suite | ✅ |
