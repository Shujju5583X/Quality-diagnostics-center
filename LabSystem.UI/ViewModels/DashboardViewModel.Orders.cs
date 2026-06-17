using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LabSystem.Core.Models;
using LabSystem.Core.Enums;
using Serilog;

namespace LabSystem.UI.ViewModels
{
    public partial class DashboardViewModel
    {
        public string OrderNotes
        {
            get { return _orderNotes; }
            set { _orderNotes = value; OnPropertyChanged(); }
        }

        // Free-text referral field with autocomplete from ReferredByHistory
        public string OrderReferredBy
        {
            get { return _orderReferredBy; }
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

                // Sync doctor selection to ReferredBy field
                string referredBy = "SELF";
                if (SelectedDoctorForOrder != null && SelectedDoctorForOrder.DoctorId > 0)
                {
                    referredBy = SelectedDoctorForOrder.FullName;
                }
                else if (!string.IsNullOrWhiteSpace(OrderReferredBy))
                {
                    referredBy = OrderReferredBy.Trim();
                }

                var order = new TestOrder
                {
                    PatientId = SelectedPatient.PatientId,
                    StatusEnum = OrderStatus.Pending,
                    Notes = OrderNotes ?? "",
                    ReferredBy = referredBy,
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
                SelectedDoctorForOrder = DoctorsForOrder != null ? DoctorsForOrder.FirstOrDefault() : null;

                await LoadDataAsync();
                MessageBox.Show("Test order created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create order.");
                MessageBox.Show("Error creating test order.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
