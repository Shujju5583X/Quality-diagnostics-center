using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using LabSystem.Core.Models;
using Serilog;

namespace LabSystem.UI.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            try
            {
                InitializeComponent();
                Log.Information("DashboardView XAML loaded successfully.");
            }
            catch (Exception ex)
            {
                var crashFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_crash.log");
                File.AppendAllText(crashFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} DASHBOARDVIEW XAML PARSE ERROR: {ex}\r\n");
                Log.Fatal(ex, "Failed to parse DashboardView XAML.");
                throw;
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
    }
}
