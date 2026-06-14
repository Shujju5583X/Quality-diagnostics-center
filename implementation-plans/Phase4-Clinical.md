# Phase 4 — Clinical Features

## Objective
Add report amendment workflow UI, patient history/trends view, and a quality control module.

## Prerequisites
- Phase 1 and Phase 2 completed
- `ResultService.AmendResultAsync` already implemented (backend ready)
- `Result.IsAmended`, `AmendmentReason`, `AmendedAt` fields exist on the model

---

## Feature 1: Report Amendment Workflow UI

### What
Allow the operator to amend a previously finalized result with a mandatory reason, creating an audit trail.

### Files to Modify

| Layer | File | Change |
|-------|------|--------|
| UI | `LabSystem.UI/ViewModels/DashboardViewModel.Orders.cs` | Add `AmendResultCommand`, `AmendResultAsync` method |
| UI | `LabSystem.UI/ViewModels/DashboardViewModel.Helpers.cs` | Add `IsAmendmentMode` property to `ResultInput` |
| UI | `LabSystem.UI/Views/DashboardView.xaml` | Add "Amend" button and reason input to Work Queue results section |

### ViewModel Changes (DashboardViewModel.Orders.cs)
```csharp
// Add to partial class:
public ICommand AmendResultCommand { get; }

// In constructor (DashboardViewModel.cs):
AmendResultCommand = new AsyncRelayCommand(async o => await ExecuteAmendResultAsync(o));

private async Task ExecuteAmendResultAsync(object parameter)
{
    if (parameter is not ResultInput ri) return;

    var reasonDialog = new Views.AmendmentReasonDialog();
    if (reasonDialog.ShowDialog() != true) return;

    string reason = reasonDialog.Reason;
    if (string.IsNullOrWhiteSpace(reason))
    {
        MessageBox.Show("Amendment reason is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }

    try
    {
        double? newValue = double.TryParse(ri.ValueText, out var v) ? v : (double?)null;
        await _resultService.AmendResultAsync(ri.TypeId, newValue, ri.ValueText, reason, App.AuthenticatedStaffId);

        // Update UI state
        ri.IsAbnormal = ReferenceRangeEvaluator.IsAbnormal(newValue,
            await _testTypeRepo.GetByIdAsync(ri.TypeId), SelectedOrder?.Patient);

        MessageBox.Show("Result amended successfully.", "Amended", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to amend result.");
        MessageBox.Show($"Amendment failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

### New Dialog: AmendmentReasonDialog.xaml
Create a simple modal dialog with a TextBox for reason and OK/Cancel buttons.

```xml
<Window x:Class="LabSystem.UI.Views.AmendmentReasonDialog"
        Title="Amendment Reason" Width="400" Height="200" WindowStartupLocation="CenterOwner">
    <Grid Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <TextBlock Grid.Row="0" Text="Enter reason for amending this result:" FontWeight="Bold" Margin="0 0 0 8"/>
        <TextBox Grid.Row="1" x:Name="ReasonTextBox" AcceptsReturn="True" TextWrapping="Wrap" VerticalScrollBarVisibility="Auto"/>
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0 8 0 0">
            <Button Content="OK" IsDefault="True" Click="OkButton_Click" Width="80" Margin="0 0 8 0"/>
            <Button Content="Cancel" IsCancel="True" Width="80"/>
        </StackPanel>
    </Grid>
</Window>
```

```csharp
// AmendmentReasonDialog.xaml.cs
public string Reason => ReasonTextBox.Text;
private void OkButton_Click(object sender, RoutedEventArgs e) { DialogResult = true; }
```

### XAML Changes (Work Queue detail section)
Add an "Amend" button next to each result row when the order is complete:
```xml
<Button Command="{Binding DataContext.AmendResultCommand, 
    RelativeSource={RelativeSource AncestorType=Window}}"
    CommandParameter="{Binding}" Content="AMEND" FontSize="10"
    Style="{StaticResource MaterialDesignFlatButton}" Foreground="#FF6F00"
    Visibility="{Binding IsReadOnly, Converter={StaticResource InvertBoolConverter}}"/>
```

### Tests
- Test amend with empty reason — should throw `ArgumentException`
- Test amend with valid reason — `IsAmended` flag set, `AmendmentReason` stored
- Test amend re-evaluates abnormality with new value

---

## Feature 2: Patient History View

### What
Show a patient's historical results across all orders, allowing the operator to see how values have changed over time.

### Files to Create/Modify

| Layer | File | Change |
|-------|------|--------|
| Core | `LabSystem.Core/Interfaces/IPatientRepository.cs` | Add `GetPatientOrdersAsync(int patientId)` |
| Data | `LabSystem.Data/Repositories/PatientRepository.cs` | Implement `GetPatientOrdersAsync` |
| UI | `LabSystem.UI/ViewModels/DashboardViewModel.cs` | Add `PatientHistory` collection, `LoadPatientHistoryCommand` |
| UI | `LabSystem.UI/Views/DashboardView.xaml` | Add "History" button on Patients tab, history popup/window |

### Repository Method (PatientRepository.cs)
```csharp
public async Task<IEnumerable<TestOrder>> GetPatientOrdersAsync(int patientId, CancellationToken cancellationToken = default)
{
    return await _context.TestOrders
        .AsNoTracking()
        .Include(o => o.TestTypes)
        .Include(o => o.Specimens)
        .Where(o => o.PatientId == patientId)
        .OrderByDescending(o => o.OrderedAt)
        .ToListAsync(cancellationToken);
}
```

### ViewModel Properties (DashboardViewModel.cs)
```csharp
public ObservableCollection<PatientHistoryEntry> PatientHistory { get; } = new ObservableCollection<PatientHistoryEntry>();
public ICommand LoadPatientHistoryCommand { get; }

// In constructor:
LoadPatientHistoryCommand = new AsyncRelayCommand(async o => await ExecuteLoadPatientHistoryAsync());

private async Task ExecuteLoadPatientHistoryAsync()
{
    if (SelectedPatient == null) return;

    PatientHistory.Clear();
    var orders = await _patientRepo.GetPatientOrdersAsync(SelectedPatient.PatientId);
    foreach (var order in orders)
    {
        var results = await _resultRepo.GetResultsForOrderAsync(order.OrderId);
        foreach (var result in results)
        {
            PatientHistory.Add(new PatientHistoryEntry
            {
                OrderDate = order.OrderedAt,
                TestName = result.TestType?.Name ?? "Unknown",
                Value = result.Value,
                ValueText = result.ValueText,
                Unit = result.TestType?.Unit ?? "",
                IsAbnormal = result.IsAbnormal,
                ReferenceLow = result.TestType?.ReferenceRangeLow,
                ReferenceHigh = result.TestType?.ReferenceRangeHigh
            });
        }
    }
}
```

### New Model: PatientHistoryEntry.cs (in LabSystem.Core/Models)
```csharp
public class PatientHistoryEntry
{
    public DateTime OrderDate { get; set; }
    public string TestName { get; set; }
    public double? Value { get; set; }
    public string ValueText { get; set; }
    public string Unit { get; set; }
    public bool IsAbnormal { get; set; }
    public double? ReferenceLow { get; set; }
    public double? ReferenceHigh { get; set; }
}
```

### XAML: History Button on Patients Tab
```xml
<Button Command="{Binding LoadPatientHistoryCommand}" Content="VIEW HISTORY"
        Style="{StaticResource MaterialDesignRaisedButton}" Height="36" Margin="8 0 0 0"/>
```

### XAML: History Window (PatientHistoryWindow.xaml)
A new window showing a DataGrid of historical results grouped by test name, with a simple line chart for trending (optional — can use a basic DataVisualizer or just a sorted grid).

### Tests
- Test history for patient with no orders — should return empty
- Test history for patient with multiple orders — should return all results chronologically
- Test history includes test type details (Name, Unit)

---

## Feature 3: QC Module (Quality Control)

### What
Track QC control runs, plot Levey-Jennings charts, and apply Westgard rules to detect out-of-control conditions.

### Files to Create

| Layer | File | Change |
|-------|------|--------|
| Core | `LabSystem.Core/Models/QcRun.cs` | **NEW** — QC run model |
| Core | `LabSystem.Core/Models/QcResult.cs` | **NEW** — QC result model |
| Core | `LabSystem.Core/Interfaces/IQcRepository.cs` | **NEW** — QC data access |
| Data | `LabSystem.Data/Migrations/V1__init.sql` | Add `QcRuns` and `QcResults` tables |
| Data | `LabSystem.Data/Repositories/QcRepository.cs` | **NEW** — QC repository |
| Services | `LabSystem.Services/QcService.cs` | **NEW** — QC evaluation (Westgard rules) |
| UI | `LabSystem.UI/ViewModels/QcViewModel.cs` | **NEW** — QC tab ViewModel |
| UI | `LabSystem.UI/Views/QcView.xaml` | **NEW** — QC tab UserControl |

### Database Schema
```sql
CREATE TABLE IF NOT EXISTS QcRuns (
    QcRunId INTEGER PRIMARY KEY AUTOINCREMENT,
    TestTypeId INTEGER NOT NULL,
    ControlName TEXT NOT NULL,
    RunDate DATETIME NOT NULL,
    MeasuredValue REAL NOT lotNumber TEXT,
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
```

### Westgard Rules Implementation (QcService.cs)
```csharp
public static class WestgardRules
{
    // 1-2s: Warning — single control exceeds ±2 SD
    // 1-3s: Reject — single control exceeds ±3 SD
    // 2-2s: Reject — two consecutive controls exceed same ±2 SD
    // R-4s: Reject — range between two controls exceeds 4 SD
    // 4-1s: Reject — four consecutive controls exceed same ±1 SD
    // 10x:  Reject — ten consecutive controls on same side of mean

    public static List<string> Evaluate(List<QcResult> recentResults, QcLot lot)
    {
        var violations = new List<string>();
        if (recentResults == null || recentResults.Count == 0) return violations;

        var latest = recentResults.Last();
        double zScore = (latest.MeasuredValue - lot.TargetValue) / lot.SD;

        // 1-2s warning
        if (Math.Abs(zScore) > 2)
            violations.Add("WARNING: 1-2s rule violated");

        // 1-3s reject
        if (Math.Abs(zScore) > 3)
            violations.Add("REJECT: 1-3s rule violated");

        // 2-2s reject
        if (recentResults.Count >= 2)
        {
            var prev = recentResults[recentResults.Count - 2];
            double prevZ = (prev.MeasuredValue - lot.TargetValue) / lot.SD;
            if (Math.Abs(prevZ) > 2 && Math.Abs(zScore) > 2
                && Math.Sign(prevZ) == Math.Sign(zScore))
                violations.Add("REJECT: 2-2s rule violated");
        }

        // 10x reject
        if (recentResults.Count >= 10)
        {
            var last10 = recentResults.Skip(Math.Max(0, recentResults.Count - 10)).ToList();
            bool allAbove = last10.All(r => r.MeasuredValue > lot.TargetValue);
            bool allBelow = last10.All(r => r.MeasuredValue < lot.TargetValue);
            if (allAbove || allBelow)
                violations.Add("REJECT: 10x rule violated");
        }

        return violations;
    }
}
```

### QC ViewModel Structure
```csharp
public class QcViewModel : ViewModelBase
{
    public ObservableCollection<QcRun> QcRuns { get; }
    public ObservableCollection<QcLot> QcLots { get; }
    public ICommand RecordQcRunCommand { get; }
    public ICommand ViewLeveyJenningsCommand { get; }
    // Filter by TestType, date range
}
```

### QC Tab Layout
- Top: Filter bar (TestType dropdown, date range)
- Left: Recent QC runs DataGrid with status (Pass/Warning/Reject)
- Right: Levey-Jennings chart area (plot measured values vs target ± SD lines)
- Bottom: Violation alerts panel

### Tests
- Test Westgard 1-3s rule detection
- Test Westgard 2-2s rule detection
- Test Westgard 10x rule detection
- Test QC run recording with lot info
- Test Levey-Jennings data query by date range

---

## Effort Estimate

| Feature | Days |
|---------|------|
| Report Amendment UI | 1 |
| Patient History View | 1.5 |
| QC Module | 2 |
| Testing | 0.5 |
| **Total** | **5 days** |

## Verification

After completing Phase 4:
1. `dotnet build` — 0 errors
2. `dotnet test` — all tests pass
3. Manual: Open completed order → click Amend → enter reason → verify result updates with amendment flag
4. Manual: Select patient → click View History → verify all past results displayed
5. Manual: Navigate to QC tab → record control run → verify Levey-Jennings plot updates
