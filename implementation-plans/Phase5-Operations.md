# Phase 5 — Operations Features

## Objective
Add SMS notifications, an appointment booking system, and full staff management.

## Prerequisites
- Phase 3 and Phase 4 completed
- Staff model exists with `PinHash`, `Role` column in DB (unmapped to model)

---

## Feature 1: SMS Notification Service

### What
Send SMS notifications to patients for appointment reminders, result readiness, and payment due alerts.

### Files to Create/Modify

| Layer | File | Change |
|-------|------|--------|
| Core | `LabSystem.Core/Interfaces/ISmsService.cs` | **NEW** — SMS service interface |
| Core | `LabSystem.Core/Models/SmsLog.cs` | **NEW** — SMS delivery log |
| Data | `LabSystem.Data/Migrations/V1__init.sql` | Add `SmsLog` table |
| Services | `LabSystem.Services/SmsService.cs` | **NEW** — HTTP-based SMS gateway |
| UI | `LabSystem.UI/ViewModels/SettingsViewModel.cs` | **NEW** — SMS config settings |

### Interface (ISmsService.cs)
```csharp
public interface ISmsService
{
    Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
    Task<IEnumerable<SmsLog>> GetSmsLogAsync(int? patientId = null, CancellationToken cancellationToken = default);
}
```

### SMS Log Model (SmsLog.cs)
```csharp
public class SmsLog
{
    public int SmsLogId { get; set; }
    public int? PatientId { get; set; }
    public virtual Patient Patient { get; set; }
    public string PhoneNumber { get; set; }
    public string Message { get; set; }
    public string Status { get; set; } // Sent, Failed, Pending
    public string GatewayResponse { get; set; }
    public DateTime SentAt { get; set; }
}
```

### Database Schema
```sql
CREATE TABLE IF NOT EXISTS SmsLog (
    SmsLogId INTEGER PRIMARY KEY AUTOINCREMENT,
    PatientId INTEGER,
    PhoneNumber TEXT NOT NULL,
    Message TEXT NOT NULL,
    Status TEXT NOT NULL DEFAULT 'Pending',
    GatewayResponse TEXT,
    SentAt DATETIME NOT NULL,
    FOREIGN KEY(PatientId) REFERENCES Patients(PatientId)
);
```

### SMS Gateway Implementation (SmsService.cs)
```csharp
public class SmsService : ISmsService
{
    private readonly string _apiKey;
    private readonly string _senderId;
    private readonly HttpClient _httpClient;

    public SmsService(string apiKey, string senderId)
    {
        _apiKey = apiKey;
        _senderId = senderId;
        _httpClient = new HttpClient();
    }

    public async Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            // Example: MSG91, Twilio, or any bulk SMS API
            var url = $"https://api.smsprovider.com/send?apikey={_apiKey}&sender={_senderId}&phone={phoneNumber}&message={Uri.EscapeDataString(message)}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
```

### Message Templates
```csharp
public static class SmsTemplates
{
    public static string AppointmentReminder(string patientName, DateTime appointmentDate)
        => $"Dear {patientName}, your appointment at Quality Diagnostics Center is on {appointmentDate:dd-MMM-yyyy hh:mm tt}. Please arrive 15 minutes early.";

    public static string ResultReady(string patientName)
        => $"Dear {patientName}, your test results at Quality Diagnostics Center are ready for collection.";

    public static string PaymentDue(string patientName, decimal amount)
        => $"Dear {patientName}, a payment of ₹{amount:N2} is pending at Quality Diagnostics Center. Please settle at your earliest convenience.";
}
```

### Integration Points
- After result verification: send "Result Ready" SMS
- After appointment booking: send "Appointment Reminder" SMS
- After invoice creation: send "Payment Due" SMS (optional)

### Tests
- Test SMS log recording (sent/failed status)
- Test message template formatting
- Test null/empty phone number handling

---

## Feature 2: Appointment System

### What
Allow patients to book appointments with date/time slots, view upcoming appointments, and send reminders.

### Files to Create/Modify

| Layer | File | Change |
|-------|------|--------|
| Core | `LabSystem.Core/Models/Appointment.cs` | **NEW** — appointment model |
| Core | `LabSystem.Core/Interfaces/IAppointmentRepository.cs` | **NEW** — appointment repository |
| Data | `LabSystem.Data/Migrations/V1__init.sql` | Add `Appointments` table |
| Data | `LabSystem.Data/Repositories/AppointmentRepository.cs` | **NEW** |
| Services | `LabSystem.Services/AppointmentService.cs` | **NEW** — booking logic |
| UI | `LabSystem.UI/ViewModels/AppointmentsViewModel.cs` | **NEW** — appointments tab |
| UI | `LabSystem.UI/Views/AppointmentsView.xaml` | **NEW** — appointments tab UI |

### Database Schema
```sql
CREATE TABLE IF NOT EXISTS Appointments (
    AppointmentId INTEGER PRIMARY KEY AUTOINCREMENT,
    PatientId INTEGER NOT NULL,
    AppointmentDate DATETIME NOT NULL,
    DurationMinutes INTEGER NOT NULL DEFAULT 15,
    Purpose TEXT,
    Status TEXT NOT NULL DEFAULT 'Scheduled', -- Scheduled, Completed, Cancelled, NoShow
    Notes TEXT,
    CreatedAt DATETIME,
    UpdatedAt DATETIME,
    FOREIGN KEY(PatientId) REFERENCES Patients(PatientId)
);
```

### Appointment Model
```csharp
public class Appointment
{
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public virtual Patient Patient { get; set; }
    public DateTime AppointmentDate { get; set; }
    public int DurationMinutes { get; set; } = 15;
    public string Purpose { get; set; }
    public string Status { get; set; } = "Scheduled";
    public string Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Appointment Service
```csharp
public class AppointmentService : IAppointmentService
{
    public async Task<Appointment> BookAppointmentAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        // Check for overlapping appointments
        var existing = await _appointmentRepo.GetByDateAsync(appointment.AppointmentDate.Date, cancellationToken);
        if (existing.Any(a => a.Status == "Scheduled"
            && a.AppointmentDate < appointment.AppointmentDate.AddMinutes(appointment.DurationMinutes)
            && appointment.AppointmentDate < a.AppointmentDate.AddMinutes(a.DurationMinutes)))
        {
            throw new InvalidOperationException("Time slot is already booked.");
        }

        appointment.CreatedAt = DateTime.UtcNow;
        appointment.UpdatedAt = DateTime.UtcNow;
        await _appointmentRepo.AddAsync(appointment, cancellationToken);
        return appointment;
    }
}
```

### UI Layout (AppointmentsView.xaml)
- Top: Date picker + "Book Appointment" button
- Left: Calendar/date list showing appointments for selected date
- Right: Booking form (patient selector, time slot, purpose, notes)
- Bottom: Upcoming appointments DataGrid

### Tests
- Test booking overlap detection
- Test cancel appointment (status change)
- Test no-show marking
- Test appointment list by date range

---

## Feature 3: Staff Management

### What
Full CRUD for staff members, role assignment, and PIN management.

### Files to Create/Modify

| Layer | File | Change |
|-------|------|--------|
| Core | `LabSystem.Core/Models/Staff.cs` | Add `Role` property (maps to existing DB column) |
| Core | `LabSystem.Core/Interfaces/IStaffRepository.cs` | Add `GetAllAsync`, `GetByIdAsync` |
| Data | `LabSystem.Data/Repositories/StaffRepository.cs` | Implement new methods |
| Services | `LabSystem.Services/StaffService.cs` | **NEW** — staff CRUD + PIN management |
| UI | `LabSystem.UI/ViewModels/StaffViewModel.cs` | **NEW** — staff management tab |
| UI | `LabSystem.UI/Views/StaffView.xaml` | **NEW** — staff management UI |

### Staff Model Update (Staff.cs)
```csharp
public class Staff
{
    public int StaffId { get; set; }
    public string FullName { get; set; }
    public string Role { get; set; } = "Technician"; // Admin, Technician, Receptionist
    public DateTime CreatedAt { get; set; }
    public string PinHash { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
}
```

### Staff Service
```csharp
public class StaffService : IStaffService
{
    public async Task<Staff> CreateStaffAsync(string fullName, string role, string pin, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Staff name is required.");
        if (string.IsNullOrWhiteSpace(pin) || pin.Length < 4)
            throw new ArgumentException("PIN must be at least 4 digits.");

        var staff = new Staff
        {
            FullName = fullName.Trim(),
            Role = role ?? "Technician",
            PinHash = BCrypt.Net.BCrypt.HashPassword(pin),
            CreatedAt = DateTime.UtcNow
        };
        await _staffRepo.AddAsync(staff, cancellationToken);
        return staff;
    }

    public async Task ResetPinAsync(int staffId, string newPin, CancellationToken cancellationToken = default)
    {
        var staff = await _staffRepo.GetByIdAsync(staffId, cancellationToken);
        if (staff == null) throw new InvalidOperationException("Staff not found.");
        staff.PinHash = BCrypt.Net.BCrypt.HashPassword(newPin);
        await _staffRepo.UpdateAsync(staff, cancellationToken);
    }

    public async Task ToggleLockoutAsync(int staffId, bool lockout, CancellationToken cancellationToken = default)
    {
        var staff = await _staffRepo.GetByIdAsync(staffId, cancellationToken);
        if (staff == null) throw new InvalidOperationException("Staff not found.");
        staff.LockoutEnd = lockout ? DateTime.UtcNow.AddMinutes(60) : (DateTime?)null;
        staff.FailedLoginAttempts = lockout ? 5 : 0;
        await _staffRepo.UpdateAsync(staff, cancellationToken);
    }
}
```

### UI Layout (StaffView.xaml)
- DataGrid listing all staff (Name, Role, Created, Lockout status)
- Add Staff form (Name, Role dropdown, PIN input)
- Edit Staff (change role, reset PIN)
- Lock/Unlock toggle button

### Tests
- Test staff creation with duplicate name
- Test PIN reset
- Test lockout toggle
- Test role assignment

---

## Effort Estimate

| Feature | Days |
|---------|------|
| SMS Notification Service | 1.5 |
| Appointment System | 2 |
| Staff Management | 1.5 |
| Testing | 0.5 |
| **Total** | **5.5 days** |

## Verification

After completing Phase 5:
1. `dotnet build` — 0 errors
2. `dotnet test` — all tests pass
3. Manual: Book appointment → verify it appears on calendar → mark as completed
4. Manual: Complete a result → verify SMS log entry created (mock gateway)
5. Manual: Add new staff member → assign role → reset PIN → verify login works
