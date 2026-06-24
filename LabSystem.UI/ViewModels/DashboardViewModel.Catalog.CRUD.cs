using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LabSystem.Core.Models;
using Serilog;

namespace LabSystem.UI.ViewModels
{
    public partial class DashboardViewModel
    {
        private async Task ExecuteAddDepartmentAsync()
        {
            if (string.IsNullOrWhiteSpace(NewDepartmentName))
            {
                MessageBox.Show("Please enter a department name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dept = new Department
                {
                    Name = NewDepartmentName.Trim()
                };

                await _departmentRepo.AddAsync(dept);
                Log.Information("Added department: {DepartmentName}", dept.Name);
                NewDepartmentName = string.Empty;

                await RefreshCatalogStateAsync(dept.DepartmentId, null);
                MessageBox.Show("Department added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add department.");
                MessageBox.Show("Error adding department. Make sure the name is unique.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteDeleteDepartmentAsync()
        {
            if (SelectedDepartment == null)
            {
                MessageBox.Show("Please select a department to delete.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialogResult = MessageBox.Show("Are you sure you want to delete department '" + SelectedDepartment.Name + "'? This will delete all tests belonging to this department.", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (dialogResult == MessageBoxResult.No)
            {
                return;
            }

            try
            {
                int deptId = SelectedDepartment.DepartmentId;
                await _departmentRepo.DeleteAsync(deptId);
                Log.Information("Deleted department ID: {DepartmentId}", deptId);

                _selectedDepartment = null;

                await RefreshCatalogStateAsync(null, null);
                MessageBox.Show("Department and its tests deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete department.");
                MessageBox.Show("Error deleting department.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteRenameDepartmentAsync()
        {
            if (SelectedDepartment == null)
            {
                MessageBox.Show("Please select a department to rename.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(NewDepartmentName))
            {
                MessageBox.Show("Please enter a new name for the department.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dept = await _departmentRepo.GetByIdAsync(SelectedDepartment.DepartmentId);
                if (dept != null)
                {
                    dept.Name = NewDepartmentName.Trim();
                    await _departmentRepo.UpdateAsync(dept);
                    Log.Information("Renamed department ID {DepartmentId} to: {NewName}", dept.DepartmentId, dept.Name);

                    int deptId = dept.DepartmentId;
                    NewDepartmentName = string.Empty;
                    await RefreshCatalogStateAsync(deptId, null);
                    MessageBox.Show("Department renamed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to rename department.");
                MessageBox.Show("Error renaming department. Make sure the name is unique.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteDeleteCatalogTestAsync()
        {
            if (SelectedCatalogTest == null)
            {
                MessageBox.Show("Please select a test from the grid to delete.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialogResult = MessageBox.Show("Are you sure you want to delete test '" + SelectedCatalogTest.Name + "'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (dialogResult == MessageBoxResult.No)
            {
                return;
            }

            try
            {
                int testId = SelectedCatalogTest.TypeId;
                int? deptId = SelectedCatalogTest.DepartmentId;
                await _testTypeRepo.DeleteAsync(testId);
                Log.Information("Deleted test type ID: {TypeId}", testId);

                SelectedCatalogTest = null;

                await RefreshCatalogStateAsync(deptId, null);
                MessageBox.Show("Test deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete test.");
                MessageBox.Show("Error deleting test from database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════ TEST PANEL / PACKAGE CRUD ═══════════════

        private TestPanel _editingTestPanel;
        public TestPanel EditingTestPanel
        {
            get { return _editingTestPanel; }
            set
            {
                _editingTestPanel = value;
                OnPropertyChanged();
                OnPropertyChanged("IsEditingTestPanel");
                if (value != null)
                {
                    NewTestPanelName = value.Name;
                    NewTestPanelDescription = value.Description ?? "";
                    NewTestPanelPrice = value.Price;
                }
                else
                {
                    NewTestPanelName = "";
                    NewTestPanelDescription = "";
                    NewTestPanelPrice = 0;
                }
            }
        }

        public bool IsEditingTestPanel
        {
            get { return EditingTestPanel != null; }
        }

        private string _newTestPanelName;
        public string NewTestPanelName
        {
            get { return _newTestPanelName; }
            set { _newTestPanelName = value; OnPropertyChanged(); }
        }

        private string _newTestPanelDescription;
        public string NewTestPanelDescription
        {
            get { return _newTestPanelDescription; }
            set { _newTestPanelDescription = value; OnPropertyChanged(); }
        }

        private decimal _newTestPanelPrice;
        public decimal NewTestPanelPrice
        {
            get { return _newTestPanelPrice; }
            set { _newTestPanelPrice = value; OnPropertyChanged(); }
        }

        public ObservableCollection<TestPanel> TestPanelsCatalog { get; private set; }

        private async Task ExecuteAddTestPanelAsync()
        {
            if (string.IsNullOrWhiteSpace(NewTestPanelName))
            {
                MessageBox.Show("Please enter a package name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NewTestPanelPrice <= 0)
            {
                MessageBox.Show("Please enter a valid package price.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var panel = new TestPanel
                {
                    Name = NewTestPanelName.Trim(),
                    Description = NewTestPanelDescription ?? "",
                    Price = NewTestPanelPrice
                };

                await _testPanelRepo.AddAsync(panel);
                Log.Information("Added test panel: {PanelName}", panel.Name);

                NewTestPanelName = "";
                NewTestPanelDescription = "";
                NewTestPanelPrice = 0;

                await LoadTestPanelsCatalogAsync();
                MessageBox.Show("Package added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add test panel.");
                MessageBox.Show("Error adding package.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteUpdateTestPanelAsync()
        {
            if (EditingTestPanel == null)
            {
                MessageBox.Show("Please select a package to update.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(NewTestPanelName))
            {
                MessageBox.Show("Please enter a package name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var panel = await _testPanelRepo.GetByIdAsync(EditingTestPanel.PanelId);
                if (panel != null)
                {
                    panel.Name = NewTestPanelName.Trim();
                    panel.Description = NewTestPanelDescription ?? "";
                    panel.Price = NewTestPanelPrice;
                    await _testPanelRepo.UpdateAsync(panel);
                    Log.Information("Updated test panel: {PanelName} (ID: {PanelId})", panel.Name, panel.PanelId);
                }

                EditingTestPanel = null;
                NewTestPanelName = "";
                NewTestPanelDescription = "";
                NewTestPanelPrice = 0;

                await LoadTestPanelsCatalogAsync();
                MessageBox.Show("Package updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to update test panel.");
                MessageBox.Show("Error updating package.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteDeleteTestPanelAsync()
        {
            if (EditingTestPanel == null)
            {
                MessageBox.Show("Please select a package to delete.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialogResult = MessageBox.Show(
                "Are you sure you want to delete package '" + EditingTestPanel.Name + "'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (dialogResult != MessageBoxResult.Yes) return;

            try
            {
                int panelId = EditingTestPanel.PanelId;
                await _testPanelRepo.DeleteAsync(panelId);
                Log.Information("Deleted test panel ID: {PanelId}", panelId);

                EditingTestPanel = null;
                NewTestPanelName = "";
                NewTestPanelDescription = "";
                NewTestPanelPrice = 0;

                await LoadTestPanelsCatalogAsync();
                MessageBox.Show("Package deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete test panel.");
                MessageBox.Show("Error deleting package.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteEditTestPanelAsync(object obj)
        {
            var panel = obj as TestPanel;
            if (panel != null)
            {
                EditingTestPanel = panel;
            }
        }

        private async Task LoadTestPanelsCatalogAsync()
        {
            try
            {
                var panels = await _testPanelRepo.GetAllAsync();
                TestPanelsCatalog.Clear();
                foreach (var p in panels.OrderBy(p => p.Name))
                {
                    TestPanelsCatalog.Add(p);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load test panels for catalog.");
            }
        }
    }
}
