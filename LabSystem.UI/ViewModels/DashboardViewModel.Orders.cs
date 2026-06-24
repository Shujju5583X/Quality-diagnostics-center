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

        private async Task LoadOrdersAsync()
        {
            try
            {
                int totalCount = await _orderRepo.GetCountAsync();
                OrderTotalCount = totalCount;
                OrderTotalPages = (int)Math.Ceiling((double)totalCount / PageSize);
                if (OrderCurrentPage < 1) OrderCurrentPage = 1;
                if (OrderCurrentPage > OrderTotalPages) OrderCurrentPage = OrderTotalPages;

                var pagedOrders = await _orderRepo.GetPagedAsync(OrderCurrentPage, PageSize);
                Orders.Clear();
                foreach (var o in pagedOrders)
                {
                    Orders.Add(o);
                }
                OnPropertyChanged("PendingOrdersFiltered");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load paginated orders.");
            }
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

            TestOrder order = null;
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

                order = new TestOrder
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
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create order.");
                string detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MessageBox.Show("Error creating test order:\n\n" + detail, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                await _billingService.GenerateInvoiceAsync(order.OrderId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to generate invoice for order {OrderId}.", order.OrderId);
                string detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MessageBox.Show("Order created but invoice generation failed:\n\n" + detail + "\n\nOrder ID: " + order.OrderId, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

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

        // Order Edit mode properties
        private bool _isOrderEditMode;
        public bool IsOrderEditMode
        {
            get { return _isOrderEditMode; }
            set { _isOrderEditMode = value; OnPropertyChanged(); }
        }

        private string _editingOrderNotes;
        public string EditingOrderNotes
        {
            get { return _editingOrderNotes; }
            set { _editingOrderNotes = value; OnPropertyChanged(); }
        }

        private string _editingOrderReferredBy;
        public string EditingOrderReferredBy
        {
            get { return _editingOrderReferredBy; }
            set { _editingOrderReferredBy = value; OnPropertyChanged(); }
        }

        private TestOrder _editingOrder;
        public TestOrder EditingOrder
        {
            get { return _editingOrder; }
            set { _editingOrder = value; OnPropertyChanged(); }
        }

        private async Task ExecuteEditOrderAsync(object obj)
        {
            var order = obj as TestOrder ?? SelectedOrder;
            if (order == null)
            {
                MessageBox.Show("Please select an order to edit.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (order.StatusEnum != OrderStatus.Pending)
            {
                MessageBox.Show("Only pending orders can be edited.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            EditingOrder = order;
            EditingOrderNotes = order.Notes ?? "";
            EditingOrderReferredBy = order.ReferredBy ?? "SELF";
            IsOrderEditMode = true;
        }

        private async Task ExecuteSaveOrderEditAsync(object obj)
        {
            if (EditingOrder == null) return;

            try
            {
                await _orderService.UpdateOrderAsync(EditingOrder.OrderId, EditingOrderNotes, EditingOrderReferredBy);
                Log.Information("Updated order {OrderId}", EditingOrder.OrderId);
                IsOrderEditMode = false;
                EditingOrder = null;
                await LoadDataAsync();
                MessageBox.Show("Order updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to update order.");
                MessageBox.Show("Error updating order: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteCancelOrderEditAsync(object obj)
        {
            IsOrderEditMode = false;
            EditingOrder = null;
        }

        private async Task ExecuteVoidOrderAsync(object obj)
        {
            var order = obj as TestOrder ?? SelectedOrder;
            if (order == null)
            {
                MessageBox.Show("Please select an order to void.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (order.StatusEnum == OrderStatus.Voided)
            {
                MessageBox.Show("This order is already voided.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialogResult = MessageBox.Show(
                "Are you sure you want to void order #" + order.OrderId + " for patient '" + (order.Patient != null ? order.Patient.FullName : "Unknown") + "'?\n\nThis will mark the order as voided and void any associated invoice.",
                "Confirm Void Order", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (dialogResult != MessageBoxResult.Yes) return;

            try
            {
                await _orderService.VoidOrderAsync(order.OrderId);

                // Also void the associated invoice
                var invoice = await _billingService.GetInvoiceForOrderAsync(order.OrderId);
                if (invoice != null && !invoice.IsPaid)
                {
                    invoice.Status = "Voided";
                    invoice.UpdatedAt = DateTime.UtcNow;
                    await _invoiceRepo.UpdateAsync(invoice);
                    Log.Information("Voided invoice {InvoiceId} for order {OrderId}", invoice.InvoiceId, order.OrderId);
                }

                Log.Information("Voided order {OrderId}", order.OrderId);
                await LoadDataAsync();
                MessageBox.Show("Order voided successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to void order.");
                MessageBox.Show("Error voiding order: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
