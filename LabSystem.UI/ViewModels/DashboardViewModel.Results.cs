using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LabSystem.Core.Models;
using LabSystem.Core.Enums;
using LabSystem.Core.Services;
using Serilog;

namespace LabSystem.UI.ViewModels
{
    public partial class DashboardViewModel
    {
        private async Task ExecuteSaveResultsAsync(object obj)
        {
            if (SelectedOrder == null) return;

            if (SelectedOrder.StatusEnum != LabSystem.Core.Enums.OrderStatus.Pending)
            {
                MessageBox.Show("Results have already been verified and saved for this order.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Validate all inputs are numeric (bypass for rejected samples or qualitative options)
            foreach (var r in SelectedOrderResults)
            {
                if (r.ValueText == "Sample Rejected")
                {
                    continue;
                }
                if (string.IsNullOrWhiteSpace(r.ValueText))
                {
                    ResultErrorMessage = "Please enter a valid value for " + r.TestName + ".";
                    return;
                }
                double parsedVal;
                if (!r.HasOptions && !double.TryParse(r.ValueText, out parsedVal))
                {
                    ResultErrorMessage = "Please enter a valid numeric value for " + r.TestName + ".";
                    return;
                }
            }

            try
            {
                await _unitOfWork.RunInTransactionAsync(async () =>
                {
                    // Save each result
                    foreach (var r in SelectedOrderResults)
                    {
                        double parsedVal;
                        double? val = r.ValueText == "Sample Rejected" ? (double?)null : (double.TryParse(r.ValueText, out parsedVal) ? (double?)parsedVal : null);
                        var result = new Result
                        {
                            OrderId = SelectedOrder.OrderId,
                            TypeId = r.TypeId,
                            Value = val,
                            ValueText = r.ValueText,
                            TechnicianId = DefaultStaffId,
                            RecordedAt = DateTime.UtcNow
                        };

                        await _resultService.AddResultAsync(result);
                    }

                    int selectedOrderId = SelectedOrder.OrderId;

                    // Update order status to Complete
                    await _orderService.UpdateOrderStatusAsync(selectedOrderId, "Complete");
                    Log.Information("Verified and completed order {OrderId}", selectedOrderId);

                    // Reload
                    await LoadDataAsync();
                    
                    // Select the order again to refresh results grid as read-only
                    SelectedOrder = Orders.FirstOrDefault(o => o.OrderId == selectedOrderId);
                    
                    MessageBox.Show("Results verified and saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Generate and open the PDF report immediately
                    try
                    {
                        if (SelectedOrder != null)
                        {
                            var previewWindow = new Views.PdfPreviewWindow(SelectedOrder, _reportService);
                            previewWindow.ShowDialog();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to automatically generate PDF report after saving results.");
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save results.");
                MessageBox.Show("Error saving results to database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteGenerateReport(object obj)
        {
            if (SelectedOrder == null)
            {
                MessageBox.Show("Please select an order from the list.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedOrder.StatusEnum != LabSystem.Core.Enums.OrderStatus.Complete)
            {
                MessageBox.Show("Reports can only be generated for Complete orders.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var previewWindow = new Views.PdfPreviewWindow(SelectedOrder, _reportService);
                previewWindow.Owner = Application.Current.MainWindow;
                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to generate report.");
                MessageBox.Show("Error generating PDF report: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteEditResultsAsync(object obj)
        {
            if (SelectedOrder == null || SelectedOrder.StatusEnum != LabSystem.Core.Enums.OrderStatus.Complete) return;

            IsResultEditMode = true;
            foreach (var ri in SelectedOrderResults)
            {
                ri.IsReadOnly = false;
                ri.IsAmendmentMode = true;
            }
            OnPropertyChanged("SelectedOrderResults");
        }

        private async Task ExecuteSaveAmendmentAsync(object obj)
        {
            if (SelectedOrder == null || SelectedOrder.StatusEnum != LabSystem.Core.Enums.OrderStatus.Complete) return;

            var reasonDialog = new Views.AmendmentReasonDialog();
            if (reasonDialog.ShowDialog() != true)
            {
                await CancelEditModeAsync();
                return;
            }

            string reason = reasonDialog.Reason;
            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("Amendment reason is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                foreach (var ri in SelectedOrderResults)
                {
                    double parsedVal;
                    double? newValue = double.TryParse(ri.ValueText, out parsedVal) ? (double?)parsedVal : null;
                    await _resultService.AmendResultAsync(ri.ResultId, newValue, ri.ValueText, reason, App.AuthenticatedStaffId);
                }

                int selectedOrderId = SelectedOrder.OrderId;
                await LoadResultsForSelectedOrderAsync();
                IsResultEditMode = false;

                MessageBox.Show("Results amended successfully.", "Amended", MessageBoxButton.OK, MessageBoxImage.Information);

                SelectedOrder = Orders.FirstOrDefault(o => o.OrderId == selectedOrderId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save amendment.");
                MessageBox.Show("Amendment failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CancelEditModeAsync()
        {
            IsResultEditMode = false;
            if (SelectedOrder != null)
            {
                await LoadResultsForSelectedOrderAsync();
            }
        }

        private async Task LoadResultsForSelectedOrderAsync()
        {
            SelectedOrderResults.Clear();
            ResultErrorMessage = string.Empty;

            if (SelectedOrder == null) return;

            try
            {
                if (SelectedOrder.StatusEnum == OrderStatus.Pending)
                {
                    if (SelectedOrder.TestTypes != null)
                    {
                        foreach (var testType in SelectedOrder.TestTypes)
                        {
                            var ri = new ResultInput
                            {
                                TypeId = testType.TypeId,
                                InputType = testType.InputType,
                                TestName = testType.Name,
                                Unit = testType.Unit,
                                IsAbnormal = false,
                                IsReadOnly = false,
                                ValueText = string.Empty
                            };
                            EvaluatePatientReferenceRange(ri, testType, SelectedOrder.Patient);
                            PopulateOptions(ri);
                            SelectedOrderResults.Add(ri);
                        }
                    }
                }
                else
                {
                    var savedResults = await _resultRepo.GetResultsForOrderAsync(SelectedOrder.OrderId);
                    foreach (var r in savedResults)
                    {
                        if (r.TestType == null)
                        {
                            r.TestType = await _testTypeRepo.GetByIdAsync(r.TypeId);
                        }

                        var ri = new ResultInput
                        {
                            TypeId = r.TypeId,
                            ResultId = r.ResultId,
                            InputType = r.TestType != null ? r.TestType.InputType : ResultInputType.Numeric,
                            TestName = r.TestType != null ? r.TestType.Name ?? "Unknown Test" : "Unknown Test",
                            Unit = r.TestType != null ? r.TestType.Unit ?? "" : "",
                            ValueText = r.Value == null ? "Sample Rejected" : r.Value.ToString(),
                            IsAbnormal = r.IsAbnormal,
                            IsReadOnly = true
                        };
                        EvaluatePatientReferenceRange(ri, r.TestType, SelectedOrder.Patient);
                        PopulateOptions(ri);
                        SelectedOrderResults.Add(ri);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load order results.");
            }
        }

        private void PopulateOptions(ResultInput ri)
        {
            ri.Options.Clear();
            switch (ri.InputType)
            {
                case ResultInputType.BloodGroup:
                    ri.Options.Add(new ResultOption { Display = "A Rh Positive", Value = "1" });
                    ri.Options.Add(new ResultOption { Display = "A Rh Negative", Value = "2" });
                    ri.Options.Add(new ResultOption { Display = "B Rh Positive", Value = "3" });
                    ri.Options.Add(new ResultOption { Display = "B Rh Negative", Value = "4" });
                    ri.Options.Add(new ResultOption { Display = "O Rh Positive", Value = "5" });
                    ri.Options.Add(new ResultOption { Display = "O Rh Negative", Value = "6" });
                    ri.Options.Add(new ResultOption { Display = "AB Rh Positive", Value = "7" });
                    ri.Options.Add(new ResultOption { Display = "AB Rh Negative", Value = "8" });
                    break;
                case ResultInputType.Categorical:
                    if (ri.TestName.Contains("Rapid Malaria") || ri.TestName.Contains("HBsAg") || ri.TestName.Contains("HCV") || ri.TestName.Contains("VDRL") || ri.TestName.Contains("HIV"))
                    {
                        ri.Options.Add(new ResultOption { Display = "Negative", Value = "0" });
                        ri.Options.Add(new ResultOption { Display = "Positive", Value = "1" });
                    }
                    else
                    {
                        ri.Options.Add(new ResultOption { Display = "Not Detected", Value = "0" });
                        ri.Options.Add(new ResultOption { Display = "Detected", Value = "1" });
                    }
                    break;
                case ResultInputType.Qualitative:
                    ri.Options.Add(new ResultOption { Display = "Absent", Value = "0" });
                    ri.Options.Add(new ResultOption { Display = "Present", Value = "1" });
                    break;
                case ResultInputType.Numeric:
                default:
                    break;
            }

            if (ri.HasOptions && !string.IsNullOrEmpty(ri.ValueText))
            {
                double parsedOVal;
                double parsedRVal;
                ri.SelectedOption = ri.Options.FirstOrDefault(o =>
                    o.Value == ri.ValueText ||
                    (double.TryParse(o.Value, out parsedOVal) && double.TryParse(ri.ValueText, out parsedRVal) && Math.Abs(parsedOVal - parsedRVal) < 0.001));
            }
        }

        public ICommand AmendResultCommand { get; private set; }

        public void InitializeAmendResultCommand()
        {
            AmendResultCommand = new AsyncRelayCommand(async o => await ExecuteAmendResultAsync(o));
        }

        private async Task ExecuteAmendResultAsync(object parameter)
        {
            var ri = parameter as ResultInput;
            if (ri == null) return;

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
                double parsedVal;
                double? newValue = double.TryParse(ri.ValueText, out parsedVal) ? (double?)parsedVal : null;
                await _resultService.AmendResultAsync(ri.ResultId, newValue, ri.ValueText, reason, App.AuthenticatedStaffId);

                ri.IsAmendmentMode = false;
                ri.IsAbnormal = ReferenceRangeEvaluator.IsAbnormal(newValue,
                    await _testTypeRepo.GetByIdAsync(ri.TypeId), SelectedOrder != null ? SelectedOrder.Patient : null);

                MessageBox.Show("Result amended successfully.", "Amended", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to amend result.");
                MessageBox.Show("Amendment failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadResultsForSelectedOrderSafeAsync()
        {
            try
            {
                await LoadResultsForSelectedOrderAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load results for selected order.");
            }
        }
    }
}
