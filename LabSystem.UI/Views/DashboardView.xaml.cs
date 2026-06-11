using System;
using System.Windows.Controls;
using System.Windows.Data;
using LabSystem.Core.Models;

namespace LabSystem.UI.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        private void PatientSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null || PatientsGrid == null || PatientsGrid.ItemsSource == null) return;

            var filterText = textBox.Text;
            var cv = CollectionViewSource.GetDefaultView(PatientsGrid.ItemsSource);
            if (cv == null) return;

            if (string.IsNullOrWhiteSpace(filterText))
            {
                cv.Filter = null;
            }
            else
            {
                cv.Filter = obj =>
                {
                    if (obj is Patient patient)
                    {
                        return (patient.FullName != null && patient.FullName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                               patient.PatientId.ToString().Contains(filterText) ||
                               (patient.ContactPhone != null && patient.ContactPhone.Contains(filterText)) ||
                               (patient.ContactEmail != null && patient.ContactEmail.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0);
                    }
                    return false;
                };
            }
        }

        private void OrderPatientSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null || OrderPatientsGrid == null || OrderPatientsGrid.ItemsSource == null) return;

            var filterText = textBox.Text;
            var cv = CollectionViewSource.GetDefaultView(OrderPatientsGrid.ItemsSource);
            if (cv == null) return;

            if (string.IsNullOrWhiteSpace(filterText))
            {
                cv.Filter = null;
            }
            else
            {
                cv.Filter = obj =>
                {
                    if (obj is Patient patient)
                    {
                        return (patient.FullName != null && patient.FullName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                               patient.PatientId.ToString().Contains(filterText);
                    }
                    return false;
                };
            }
        }

        private void OrdersSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null || OrdersGrid == null || OrdersGrid.ItemsSource == null) return;

            var filterText = textBox.Text;
            var cv = CollectionViewSource.GetDefaultView(OrdersGrid.ItemsSource);
            if (cv == null) return;

            if (string.IsNullOrWhiteSpace(filterText))
            {
                cv.Filter = null;
            }
            else
            {
                cv.Filter = obj =>
                {
                    if (obj is TestOrder order)
                    {
                        var patientName = order.Patient?.FullName ?? "";
                        return (patientName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                               order.OrderId.ToString().Contains(filterText) ||
                               (order.Status != null && order.Status.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0);
                    }
                    return false;
                };
            }
        }

        private void CatalogSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null || CatalogGrid == null || CatalogGrid.ItemsSource == null) return;

            var filterText = textBox.Text;
            var cv = CollectionViewSource.GetDefaultView(CatalogGrid.ItemsSource);
            if (cv == null) return;

            if (string.IsNullOrWhiteSpace(filterText))
            {
                cv.Filter = null;
            }
            else
            {
                cv.Filter = obj =>
                {
                    if (obj is TestType testType)
                    {
                        return (testType.Name != null && testType.Name.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                               (testType.Category != null && testType.Category.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                               (testType.GroupName != null && testType.GroupName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0);
                    }
                    return false;
                };
            }
        }

        private void AuditLogSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null || AuditGrid == null || AuditGrid.ItemsSource == null) return;

            var filterText = textBox.Text;
            var cv = CollectionViewSource.GetDefaultView(AuditGrid.ItemsSource);
            if (cv == null) return;

            if (string.IsNullOrWhiteSpace(filterText))
            {
                cv.Filter = null;
            }
            else
            {
                cv.Filter = obj =>
                {
                    if (obj is AuditLog log)
                    {
                        var userName = log.User?.FullName ?? "System";
                        return (log.Action != null && log.Action.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                               (log.Details != null && log.Details.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                               (userName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                               (log.EntityType != null && log.EntityType.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0);
                    }
                    return false;
                };
            }
        }
        private void InvoicesSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null || InvoicesGrid == null || InvoicesGrid.ItemsSource == null) return;

            var filterText = textBox.Text;
            var cv = CollectionViewSource.GetDefaultView(InvoicesGrid.ItemsSource);
            if (cv == null) return;

            if (string.IsNullOrWhiteSpace(filterText))
            {
                cv.Filter = null;
            }
            else
            {
                cv.Filter = obj =>
                {
                    if (obj is Invoice invoice)
                    {
                        var patientName = invoice.Order?.Patient?.FullName ?? "";
                        return (patientName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                               invoice.InvoiceId.ToString().Contains(filterText) ||
                               invoice.OrderId.ToString().Contains(filterText);
                    }
                    return false;
                };
            }
        }
    }
}
