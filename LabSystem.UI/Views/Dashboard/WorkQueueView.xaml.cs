using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LabSystem.Core.Models;
using Serilog;

namespace LabSystem.UI.Views.Dashboard
{
    public partial class WorkQueueView : UserControl
    {
        public WorkQueueView()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize WorkQueueView");
                throw;
            }
        }

        private void PendingTestsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var vm = DataContext as ViewModels.DashboardViewModel;
            if (vm != null && vm.SelectedOrder != null)
            {
                vm.WorkQueueTabIndex = 1;
            }
        }

        private void ResultsEntryGrid_PreviewKeyDown(object sender, KeyEventArgs e)
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
    }
}
