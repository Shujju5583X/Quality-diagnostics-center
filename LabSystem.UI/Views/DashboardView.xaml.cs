using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using System.Threading.Tasks;
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
                Loaded += DashboardView_Loaded;
            }
            catch (Exception ex)
            {
                var crashFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_crash.log");
                File.AppendAllText(crashFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " DASHBOARDVIEW XAML PARSE ERROR: " + ex + "\r\n");
                Log.Fatal(ex, "Failed to parse DashboardView XAML.");
                throw;
            }
        }

        private void DashboardView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var vm = DataContext as ViewModels.DashboardViewModel;
            if (vm != null)
            {
                SidebarBorder.Width = vm.IsSidebarPinned ? 220 : 60;
            }
        }

        private void SidebarBorder_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var vm = DataContext as ViewModels.DashboardViewModel;
            if (vm != null && vm.IsSidebarPinned)
                return;
            AnimateSidebarWidth(220);
        }

        private void SidebarBorder_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var vm = DataContext as ViewModels.DashboardViewModel;
            if (vm != null && vm.IsSidebarPinned)
                return;
            AnimateSidebarWidth(60);
        }

        private void PinButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var vm = DataContext as ViewModels.DashboardViewModel;
            if (vm != null)
            {
                vm.IsSidebarPinned = !vm.IsSidebarPinned;
                if (vm.IsSidebarPinned)
                {
                    AnimateSidebarWidth(220);
                }
                else
                {
                    if (SidebarBorder.IsMouseOver)
                    {
                        AnimateSidebarWidth(220);
                    }
                    else
                    {
                        AnimateSidebarWidth(60);
                    }
                }
            }
        }

        private void AnimateSidebarWidth(double targetWidth)
        {
            var animation = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromSeconds(0.2),
                DecelerationRatio = 0.9
            };
            SidebarBorder.BeginAnimation(WidthProperty, animation);
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

        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            T parent = parentObject as T;
            if (parent != null) return parent;
            return FindVisualParent<T>(parentObject);
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T) return (T)child;
                T childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null) return childOfChild;
            }
            return null;
        }

        private void ResultsEntryGrid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var grid = sender as DataGrid;
            if (grid == null) return;

            var currentTextBox = e.OriginalSource as TextBox;
            if (currentTextBox == null) return;

            if (e.Key != Key.Enter && e.Key != Key.Up && e.Key != Key.Down && e.Key != Key.Tab)
                return;

            var row = FindVisualParent<DataGridRow>(currentTextBox);
            if (row == null) return;

            int rowIndex = grid.ItemContainerGenerator.IndexFromContainer(row);
            int nextRowIndex = rowIndex;

            if (e.Key == Key.Enter || e.Key == Key.Down)
            {
                nextRowIndex++;
            }
            else if (e.Key == Key.Up)
            {
                nextRowIndex--;
            }
            else if (e.Key == Key.Tab)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    // Shift+Tab: move to previous cell
                    var cell = FindVisualParent<DataGridCell>(currentTextBox);
                    if (cell != null)
                    {
                        e.Handled = true;
                        var columnIndex = cell.Column.DisplayIndex;
                        if (columnIndex > 0)
                        {
                            var cellInfo = new DataGridCellInfo(grid.Items[rowIndex], grid.Columns[columnIndex - 1]);
                            grid.CurrentCell = cellInfo;
                            grid.BeginEdit();
                        }
                        return;
                    }
                }
                else
                {
                    // Tab: move to next cell
                    var cell = FindVisualParent<DataGridCell>(currentTextBox);
                    if (cell != null)
                    {
                        e.Handled = true;
                        var columnIndex = cell.Column.DisplayIndex;
                        if (columnIndex < grid.Columns.Count - 1)
                        {
                            var cellInfo = new DataGridCellInfo(grid.Items[rowIndex], grid.Columns[columnIndex + 1]);
                            grid.CurrentCell = cellInfo;
                            grid.BeginEdit();
                        }
                        else if (rowIndex < grid.Items.Count - 1)
                        {
                            var cellInfo = new DataGridCellInfo(grid.Items[rowIndex + 1], grid.Columns[1]);
                            grid.CurrentCell = cellInfo;
                            grid.BeginEdit();
                        }
                        return;
                    }
                }
            }

            if (nextRowIndex >= 0 && nextRowIndex < grid.Items.Count)
            {
                e.Handled = true;
                grid.UpdateLayout();
                var nextRow = grid.ItemContainerGenerator.ContainerFromIndex(nextRowIndex) as DataGridRow;
                if (nextRow != null)
                {
                    var textBox = FindVisualChild<TextBox>(nextRow);
                    if (textBox != null)
                    {
                        textBox.Focus();
                        textBox.SelectAll();
                    }
                }
            }
        }

        // Report Generation Preview Helpers
        private string _tempDashboardPdfPath;

        private async void CompletedOrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await UpdateReportPreviewAsync();
        }

        private async void ReportLetterheadComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await UpdateReportPreviewAsync();
        }

        private async Task UpdateReportPreviewAsync()
        {
            var vm = DataContext as ViewModels.DashboardViewModel;
            if (vm != null && vm.SelectedReportOrder != null)
            {
                try
                {
                    bool includeLetterhead = (ReportLetterheadComboBox.SelectedIndex == 0); // 0 = With, 1 = Without
                    string oldPath = _tempDashboardPdfPath;

                    _tempDashboardPdfPath = await vm.ReportService.GenerateReportAsync(vm.SelectedReportOrder, includeLetterhead);

                    if (oldPath != null && oldPath != _tempDashboardPdfPath && File.Exists(oldPath))
                    {
                        try { File.Delete(oldPath); } catch { }
                    }

                    try
                    {
                        ReportWebViewer.Navigate(new Uri(_tempDashboardPdfPath).AbsoluteUri);
                    }
                    catch
                    {
                        Process.Start(_tempDashboardPdfPath);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error updating Report preview in DashboardView.");
                }
            }
        }

        private void SaveReportPdf_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as ViewModels.DashboardViewModel;
            if (vm != null && vm.SelectedReportOrder != null)
            {
                try
                {
                    var sfd = new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "PDF Documents (*.pdf)|*.pdf",
                        FileName = "Report_" + vm.SelectedReportOrder.OrderId + ".pdf"
                    };
                    if (sfd.ShowDialog() == true)
                    {
                        File.Copy(_tempDashboardPdfPath, sfd.FileName, true);
                        MessageBox.Show("Report PDF saved successfully.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error saving PDF: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PrintReportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_tempDashboardPdfPath) || !File.Exists(_tempDashboardPdfPath)) return;

            try
            {
                var p = new Process();
                p.StartInfo = new ProcessStartInfo()
                {
                    CreateNoWindow = true,
                    Verb = "print",
                    FileName = _tempDashboardPdfPath,
                    UseShellExecute = true
                };
                p.Start();
                MessageBox.Show("Print command sent successfully.", "Printing", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error printing report PDF");
                MessageBox.Show("Error printing report: " + ex.Message, "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DoctorsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DoctorsTabControl != null)
            {
                DoctorsTabControl.SelectedIndex = 0; // Details tab
            }
        }

        private void PendingTestsList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var vm = DataContext as ViewModels.DashboardViewModel;
            if (vm != null && vm.SelectedOrder != null)
            {
                vm.WorkQueueTabIndex = 1;
            }
        }
    }
}
