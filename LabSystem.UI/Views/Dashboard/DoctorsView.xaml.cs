using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LabSystem.UI.Views.Dashboard
{
    public partial class DoctorsView : UserControl
    {
        public DoctorsView()
        {
            InitializeComponent();
        }

        private void DoctorSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            var filterText = textBox != null ? textBox.Text : null;
            var cv = CollectionViewSource.GetDefaultView(DoctorsGrid.ItemsSource);
            if (cv == null) return;

            if (string.IsNullOrWhiteSpace(filterText))
            {
                cv.Filter = null;
            }
            else
            {
                cv.Filter = o =>
                {
                    var doc = o as LabSystem.Core.Models.Doctor;
                    if (doc == null) return false;
                    return (doc.FullName != null && doc.FullName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                           (doc.ContactPhone != null && doc.ContactPhone.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                           doc.DoctorId.ToString().Contains(filterText);
                };
            }
        }

        private void ClearDoctorSelection_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as ViewModels.DashboardViewModel;
            if (vm != null)
            {
                vm.SelectedDoctor = null;
            }
        }
    }
}
