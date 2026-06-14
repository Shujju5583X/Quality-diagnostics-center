# Phase 3 — Financial Features

## Objective
Add discount/tax UI controls, partial payment support, and revenue reporting to the billing system.

## Prerequisites
- Phase 1 and Phase 2 completed
- `BillingService.UpdateInvoiceFinancialsAsync` already implemented (backend ready)
- `Payment` model and `AddPaymentAsync` already implemented (backend ready)
- `RevenueReportStats` model exists but is unused

---

## Feature 1: Discount/Tax UI Controls

### What
Allow the operator to apply discount (flat amount or percentage) and tax to an invoice before recording payment.

### Files to Modify

| Layer | File | Change |
|-------|------|--------|
| Core | `LabSystem.Core/Models/Invoice.cs` | Add `DiscountPercent`, `TaxPercent` properties (optional) |
| Data | `LabSystem.Data/DatabaseInitializer.cs` | Add ALTER TABLE migration for new columns |
| UI | `LabSystem.UI/ViewModels/DashboardViewModel.cs` | Add `DiscountAmount`, `TaxAmount` properties, `ApplyDiscountTaxCommand` |
| UI | `LabSystem.UI/Views/DashboardView.xaml` | Add discount/tax input fields in billing detail panel |

### Database Changes
```sql
-- Add to DatabaseInitializer.cs migrations array
new { Table = "Invoices", Column = "DiscountPercent", Type = "REAL DEFAULT 0" },
new { Table = "Invoices", Column = "TaxPercent", Type = "REAL DEFAULT 0" },
```

### ViewModel Changes (DashboardViewModel.cs)
```csharp
// Add properties
private decimal _discountAmount;
public decimal DiscountAmount
{
    get => _discountAmount;
    set { _discountAmount = value; OnPropertyChanged(); }
}

private decimal _taxAmount;
public decimal TaxAmount
{
    get => _taxAmount;
    set { _taxAmount = value; OnPropertyChanged(); }
}

public ICommand ApplyDiscountTaxCommand { get; }

// In constructor:
ApplyDiscountTaxCommand = new AsyncRelayCommand(async o => await ExecuteApplyDiscountTaxAsync());

// Add method:
private async Task ExecuteApplyDiscountTaxAsync()
{
    if (SelectedInvoice == null || SelectedInvoice.IsPaid) return;
    await _billingService.UpdateInvoiceFinancialsAsync(
        SelectedInvoice.InvoiceId, DiscountAmount, TaxAmount);
    await LoadInvoicesAsync();
    MessageBox.Show("Discount/tax applied.", "Updated", MessageBoxButton.OK, MessageBoxImage.Information);
}
```

### XAML Changes (DashboardView.xaml — billing detail panel, after line 673)
```xml
<StackPanel Orientation="Horizontal" Margin="0 8 0 0">
    <TextBlock Text="Discount: ₹" VerticalAlignment="Center" Width="80"/>
    <TextBox Text="{Binding DiscountAmount, UpdateSourceTrigger=PropertyChanged}" Width="100" Margin="0 0 16 0"/>
    <TextBlock Text="Tax: ₹" VerticalAlignment="Center" Width="60"/>
    <TextBox Text="{Binding TaxAmount, UpdateSourceTrigger=PropertyChanged}" Width="100" Margin="0 0 16 0"/>
    <Button Command="{Binding ApplyDiscountTaxCommand}" Content="APPLY" Style="{StaticResource MaterialDesignOutlinedButton}" Height="36"/>
</StackPanel>
```

### Tests
- Test `UpdateInvoiceFinancialsAsync` with negative discount (should increase total)
- Test `UpdateInvoiceFinancialsAsync` with discount > total (should result in negative grand total)
- Test that `IsPaid` recalculates after discount/tax change

---

## Feature 2: Partial Payment Support

### What
Allow paying a portion of an invoice amount instead of the full total.

### Files to Modify

| Layer | File | Change |
|-------|------|--------|
| UI | `LabSystem.UI/ViewModels/DashboardViewModel.cs` | Change `ExecuteAddPaymentAsync` to accept amount parameter |
| UI | `LabSystem.UI/Views/DashboardView.xaml` | Add amount input field next to payment buttons |

### ViewModel Changes
```csharp
// Add property
private decimal _paymentAmount;
public decimal PaymentAmount
{
    get => _paymentAmount;
    set { _paymentAmount = value; OnPropertyChanged(); }
}

// Update ExecuteAddPaymentAsync:
private async Task ExecuteAddPaymentAsync(string paymentMethod)
{
    if (SelectedInvoice == null || SelectedInvoice.IsPaid)
    {
        MessageBox.Show("Please select an unpaid invoice.", "No Invoice Selected",
            MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }

    if (PaymentAmount <= 0)
    {
        MessageBox.Show("Please enter a valid payment amount.", "Invalid Amount",
            MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }

    decimal grandTotal = SelectedInvoice.TotalAmount - SelectedInvoice.DiscountAmount + SelectedInvoice.TaxAmount;
    if (PaymentAmount > grandTotal)
    {
        MessageBox.Show($"Amount exceeds grand total of ₹{grandTotal:N2}.", "Invalid Amount",
            MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }

    await _billingService.AddPaymentAsync(SelectedInvoice.InvoiceId, PaymentAmount, paymentMethod);
    MessageBox.Show($"Payment of ₹{PaymentAmount:N2} recorded via {paymentMethod}.",
        "Payment Successful", MessageBoxButton.OK, MessageBoxImage.Information);

    PaymentAmount = 0;
    await LoadInvoicesAsync();
}
```

### XAML Changes (add above the ADD CASH / ADD UPI buttons)
```xml
<StackPanel Orientation="Horizontal" Margin="0 0 0 16">
    <TextBlock Text="Amount: ₹" VerticalAlignment="Center" FontWeight="Bold"/>
    <TextBox Text="{Binding PaymentAmount, UpdateSourceTrigger=PropertyChanged}" Width="120" Margin="8 0 0 0"
             Style="{StaticResource MaterialDesignTextBox}" materialDesign:HintAssist.Hint="Enter amount"/>
</StackPanel>
```

### Tests
- Test partial payment (pay 500 of 1000) — `IsPaid` should remain false
- Test multiple partial payments that sum to total — `IsPaid` should become true
- Test overpayment attempt — should be rejected
- Test payment with discount applied — grand total calculation correct

---

## Feature 3: Revenue Reports

### What
Generate daily/weekly/monthly revenue summaries showing total sales, collections by method, and outstanding amounts.

### Files to Create/Modify

| Layer | File | Change |
|-------|------|--------|
| Core | `LabSystem.Core/Interfaces/IBillingService.cs` | Add `GetRevenueReportAsync(DateTime start, DateTime end)` |
| Core | `LabSystem.Core/Models/RevenueReportStats.cs` | Already exists — verify properties match needs |
| Services | `LabSystem.Services/BillingService.cs` | Implement `GetRevenueReportAsync` with LINQ aggregation |
| UI | `LabSystem.UI/ViewModels/DashboardViewModel.cs` | Add revenue report properties and command |
| UI | `LabSystem.UI/Views/DashboardView.xaml` | Add "Revenue" sub-section to Billing tab or Settings tab |

### Service Implementation (BillingService.cs)
```csharp
public async Task<RevenueReportStats> GetRevenueReportAsync(DateTime start, DateTime end)
{
    var invoices = await _invoiceRepo.GetAllWithDetailsAsync();
    var filtered = invoices.Where(i => i.CreatedAt >= start && i.CreatedAt < end.AddDays(1));

    var stats = new RevenueReportStats
    {
        TotalRevenue = filtered.Sum(i => i.TotalAmount - i.DiscountAmount + i.TaxAmount),
        TotalCollected = filtered.Where(i => i.IsPaid).Sum(i => i.TotalAmount - i.DiscountAmount + i.TaxAmount),
        OutstandingAmount = filtered.Where(i => !i.IsPaid).Sum(i => i.TotalAmount - i.DiscountAmount + i.TaxAmount),
        CashCollected = filtered.Where(i => i.IsPaid && i.PaymentMethod == "Cash")
            .Sum(i => i.TotalAmount - i.DiscountAmount + i.TaxAmount),
        UpiCollected = filtered.Where(i => i.IsPaid && i.PaymentMethod == "UPI")
            .Sum(i => i.TotalAmount - i.DiscountAmount + i.TaxAmount)
    };
    return stats;
}
```

### ViewModel Properties
```csharp
private RevenueReportStats _revenueStats;
public RevenueReportStats RevenueStats
{
    get => _revenueStats;
    set { _revenueStats = value; OnPropertyChanged(); }
}

private DateTime _reportStartDate = DateTime.Today.AddDays(-30);
public DateTime ReportStartDate
{
    get => _reportStartDate;
    set { _reportStartDate = value; OnPropertyChanged(); }
}

private DateTime _reportEndDate = DateTime.Today;
public DateTime ReportEndDate
{
    get => _reportEndDate;
    set { _reportEndDate = value; OnPropertyChanged(); }
}

public ICommand GenerateRevenueReportCommand { get; }

// In constructor:
GenerateRevenueReportCommand = new AsyncRelayCommand(async o => await ExecuteGenerateRevenueReportAsync());

private async Task ExecuteGenerateRevenueReportAsync()
{
    RevenueStats = await _billingService.GetRevenueReportAsync(ReportStartDate, ReportEndDate);
}
```

### XAML Layout (add to Settings tab or Billing tab bottom)
```xml
<materialDesign:Card Padding="16" Margin="0 12 0 0">
    <StackPanel>
        <TextBlock Text="REVENUE REPORT" FontWeight="Bold" FontSize="14" Margin="0 0 0 12"/>
        <StackPanel Orientation="Horizontal" Margin="0 0 0 8">
            <TextBlock Text="From:" VerticalAlignment="Center" Width="50"/>
            <DatePicker SelectedDate="{Binding ReportStartDate}" Width="150" Margin="0 0 16 0"/>
            <TextBlock Text="To:" VerticalAlignment="Center" Width="30"/>
            <DatePicker SelectedDate="{Binding ReportEndDate}" Width="150" Margin="0 0 16 0"/>
            <Button Command="{Binding GenerateRevenueReportCommand}" Content="GENERATE"
                    Style="{StaticResource MaterialDesignRaisedButton}" Height="36"/>
        </StackPanel>
        <Grid DataContext="{Binding RevenueStats}" Margin="0 8 0 0">
            <!-- Stats cards: Total Revenue, Collected, Outstanding, Cash, UPI -->
        </Grid>
    </StackPanel>
</materialDesign:Card>
```

### Tests
- Test revenue report with no invoices (all zeros)
- Test revenue report with mixed paid/unpaid invoices
- Test date range filtering (invoices outside range excluded)
- Test payment method breakdown (Cash vs UPI)

---

## Effort Estimate

| Feature | Days |
|---------|------|
| Discount/Tax UI | 0.5 |
| Partial Payments | 0.5 |
| Revenue Reports | 1.5 |
| Testing | 0.5 |
| **Total** | **3 days** |

## Verification

After completing Phase 3:
1. `dotnet build` — 0 errors
2. `dotnet test` — all tests pass
3. Manual: Select invoice → enter discount ₹50 + tax ₹30 → Apply → verify total updates
4. Manual: Select invoice → enter partial amount ₹500 → Pay Cash → verify invoice stays "Pending"
5. Manual: Generate revenue report for last 30 days → verify totals match invoice data
