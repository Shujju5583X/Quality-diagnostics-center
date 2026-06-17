using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using LabSystem.Core.Models;
using Serilog;

namespace LabSystem.UI.Views.Dashboard
{
    public partial class TestCatalogView : UserControl
    {
        public TestCatalogView()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize TestCatalogView");
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
                    var testType = obj as TestType;
                    if (testType != null)
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
