using System;
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

                await LoadDataAsync();
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

            var dialogResult = MessageBox.Show($"Are you sure you want to delete department '{SelectedDepartment.Name}'? This will delete all tests belonging to this department.", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (dialogResult == MessageBoxResult.No)
            {
                return;
            }

            try
            {
                int deptId = SelectedDepartment.DepartmentId;
                await _departmentRepo.DeleteAsync(deptId);
                Log.Information("Deleted department ID: {DepartmentId}", deptId);

                SelectedDepartment = null;

                await LoadDataAsync();
                MessageBox.Show("Department and its tests deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete department.");
                MessageBox.Show("Error deleting department.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteDeleteCatalogTestAsync()
        {
            if (SelectedCatalogTest == null)
            {
                MessageBox.Show("Please select a test from the grid to delete.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialogResult = MessageBox.Show($"Are you sure you want to delete test '{SelectedCatalogTest.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (dialogResult == MessageBoxResult.No)
            {
                return;
            }

            try
            {
                int testId = SelectedCatalogTest.TypeId;
                await _testTypeRepo.DeleteAsync(testId);
                Log.Information("Deleted test type ID: {TypeId}", testId);

                SelectedCatalogTest = null;

                await LoadDataAsync();
                MessageBox.Show("Test deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete test.");
                MessageBox.Show("Error deleting test from database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
