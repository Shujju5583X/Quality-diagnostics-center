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
