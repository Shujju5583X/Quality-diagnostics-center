using System;
using System.Collections.Generic;
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
        public string OrderNotes
        {
            get => _orderNotes;
            set { _orderNotes = value; OnPropertyChanged(); }
        }

        // Free-text referral field with autocomplete from ReferredByHistory
        public string OrderReferredBy
        {
            get => _orderReferredBy;
            set { _orderReferredBy = value; OnPropertyChanged(); }
        }

        private async Task ExecuteCreateOrderAsync(object obj)
        {
            if (SelectedPatient == null)
            {
                MessageBox.Show("Please select a patient from the grid on the left.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedTests = TestTypes.Where(t => t.IsSelected).ToList();
            if (!selectedTests.Any())
            {
                MessageBox.Show("Please select at least one test to order.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var testIds = selectedTests.Select(t => t.TypeId).ToList();

                var order = new TestOrder
                {
                    PatientId = SelectedPatient.PatientId,
                    StatusEnum = OrderStatus.Pending,
                    Notes = OrderNotes ?? "",
                    ReferredBy = string.IsNullOrWhiteSpace(OrderReferredBy) ? "SELF" : OrderReferredBy.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _orderService.CreateOrderAsync(order, testIds, App.AuthenticatedStaffId);
                Log.Information("Created test order ID {OrderId} for Patient ID {PatientId}", order.OrderId, SelectedPatient.PatientId);

                // Generate Invoice automatically
                await _billingService.GenerateInvoiceAsync(order.OrderId);

                // Unselect test checkboxes
                foreach (var t in TestTypes)
                {
                    t.IsSelected = false;
                }

                OrderReferredBy = "SELF";
                OrderNotes = string.Empty;
                SelectedTestPanel = null;

                await LoadDataAsync();
                MessageBox.Show("Test order created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create order.");
                MessageBox.Show("Error creating test order.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    // Pull tests directly from the many-to-many relationship loaded on the order
                    if (SelectedOrder.TestTypes != null)
                    {
                        foreach (var testType in SelectedOrder.TestTypes)
                        {
                            bool isRejected = false;
                            if (SelectedOrder.Specimens != null)
                            {
                                var spec = SelectedOrder.Specimens.FirstOrDefault(s => string.Equals(s.SampleType, testType.SampleType, StringComparison.OrdinalIgnoreCase));
                                if (spec != null && spec.StatusEnum == SpecimenStatus.Rejected)
                                {
                                    isRejected = true;
                                }
                            }

                            var ri = new ResultInput
                            {
                                TypeId = testType.TypeId,
                                InputType = testType.InputType,
                                TestName = testType.Name,
                                Unit = testType.Unit,
                                IsAbnormal = false,
                                IsReadOnly = isRejected,
                                ValueText = isRejected ? "Sample Rejected" : string.Empty
                            };
                            EvaluatePatientReferenceRange(ri, testType, SelectedOrder.Patient);
                            PopulateOptions(ri);
                            SelectedOrderResults.Add(ri);
                        }
                    }
                }
                else
                {
                    // Order is complete, load the actual saved results
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
                            InputType = r.TestType?.InputType ?? ResultInputType.Numeric,
                            TestName = r.TestType?.Name ?? "Unknown Test",
                            Unit = r.TestType?.Unit ?? "",
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
                ri.SelectedOption = ri.Options.FirstOrDefault(o => o.Value == ri.ValueText || Math.Abs((double.TryParse(o.Value, out var v1) ? v1 : -1) - (double.TryParse(ri.ValueText, out var v2) ? v2 : -2)) < 0.001);
            }
        }

        public ICommand AmendResultCommand { get; private set; }

        public void InitializeAmendResultCommand()
        {
            AmendResultCommand = new AsyncRelayCommand(async o => await ExecuteAmendResultAsync(o));
        }

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

                ri.IsAmendmentMode = false;
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
    }
}
