using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LabSystem.Core.Models;
using Serilog;

namespace LabSystem.UI.ViewModels
{
    public partial class DashboardViewModel
    {
        public TestType SelectedCatalogTest
        {
            get { return _selectedCatalogTest; }
            set
            {
                _selectedCatalogTest = value;
                OnPropertyChanged();
                PopulateCatalogEditFields();
            }
        }

        public string CatalogTestName
        {
            get { return _catalogTestName; }
            set { _catalogTestName = value; OnPropertyChanged(); }
        }

        public string CatalogTestUnit
        {
            get { return _catalogTestUnit; }
            set { _catalogTestUnit = value; OnPropertyChanged(); }
        }

        public double? CatalogTestLow
        {
            get { return _catalogTestLow; }
            set { _catalogTestLow = value; OnPropertyChanged(); }
        }

        public double? CatalogTestHigh
        {
            get { return _catalogTestHigh; }
            set { _catalogTestHigh = value; OnPropertyChanged(); }
        }

        public bool CatalogTestIsActive
        {
            get { return _catalogTestIsActive; }
            set { _catalogTestIsActive = value; OnPropertyChanged(); }
        }

        public string CatalogTestCategory
        {
            get { return _catalogTestCategory; }
            set { _catalogTestCategory = value; OnPropertyChanged(); }
        }

        public string CatalogTestGroupName
        {
            get { return _catalogTestGroupName; }
            set { _catalogTestGroupName = value; OnPropertyChanged(); }
        }

        public string CatalogTestMethod
        {
            get { return _catalogTestMethod; }
            set { _catalogTestMethod = value; OnPropertyChanged(); }
        }

        public string CatalogTestInterpretation
        {
            get { return _catalogTestInterpretation; }
            set { _catalogTestInterpretation = value; OnPropertyChanged(); }
        }

        public int CatalogTestSortOrder
        {
            get { return _catalogTestSortOrder; }
            set { _catalogTestSortOrder = value; OnPropertyChanged(); }
        }

        private decimal _catalogTestPrice;
        public decimal CatalogTestPrice
        {
            get { return _catalogTestPrice; }
            set { _catalogTestPrice = value; OnPropertyChanged(); }
        }

        private void PopulateCatalogEditFields()
        {
            if (SelectedCatalogTest == null)
            {
                CatalogTestName = string.Empty;
                CatalogTestUnit = string.Empty;
                CatalogTestLow = null;
                CatalogTestHigh = null;
                CatalogTestIsActive = true;
                CatalogTestCategory = string.Empty;
                CatalogTestGroupName = string.Empty;
                CatalogTestMethod = string.Empty;
                CatalogTestInterpretation = string.Empty;
                CatalogTestSortOrder = 0;
                CatalogTestPrice = 0;
                return;
            }

            CatalogTestName = SelectedCatalogTest.Name;
            CatalogTestUnit = SelectedCatalogTest.Unit;
            CatalogTestLow = SelectedCatalogTest.ReferenceRangeLow;
            CatalogTestHigh = SelectedCatalogTest.ReferenceRangeHigh;
            CatalogTestIsActive = SelectedCatalogTest.IsActive;
            CatalogTestCategory = SelectedCatalogTest.Category;
            CatalogTestGroupName = SelectedCatalogTest.GroupName;
            CatalogTestMethod = SelectedCatalogTest.Method;
            CatalogTestInterpretation = SelectedCatalogTest.Interpretation;
            CatalogTestSortOrder = SelectedCatalogTest.SortOrder;
            CatalogTestPrice = SelectedCatalogTest.Price;
        }

        private async Task ExecuteSaveCatalogTestAsync(object obj)
        {
            if (SelectedCatalogTest == null) return;
            if (string.IsNullOrWhiteSpace(CatalogTestName))
            {
                MessageBox.Show("Please enter a test name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var entityToUpdate = await _testTypeRepo.GetByIdAsync(SelectedCatalogTest.TypeId);
                if (entityToUpdate == null) return;

                entityToUpdate.Name = CatalogTestName;
                entityToUpdate.Unit = CatalogTestUnit;
                entityToUpdate.ReferenceRangeLow = CatalogTestLow;
                entityToUpdate.ReferenceRangeHigh = CatalogTestHigh;
                entityToUpdate.IsActive = CatalogTestIsActive;
                entityToUpdate.Category = CatalogTestCategory;
                entityToUpdate.GroupName = CatalogTestGroupName;
                entityToUpdate.Method = CatalogTestMethod;
                entityToUpdate.Interpretation = CatalogTestInterpretation;
                entityToUpdate.SortOrder = CatalogTestSortOrder;
                entityToUpdate.Price = CatalogTestPrice;

                await _testTypeRepo.UpdateAsync(entityToUpdate);

                SelectedCatalogTest.Name = CatalogTestName;
                SelectedCatalogTest.Unit = CatalogTestUnit;
                SelectedCatalogTest.ReferenceRangeLow = CatalogTestLow;
                SelectedCatalogTest.ReferenceRangeHigh = CatalogTestHigh;
                SelectedCatalogTest.IsActive = CatalogTestIsActive;
                SelectedCatalogTest.Category = CatalogTestCategory;
                SelectedCatalogTest.GroupName = CatalogTestGroupName;
                SelectedCatalogTest.Method = CatalogTestMethod;
                SelectedCatalogTest.Interpretation = CatalogTestInterpretation;
                SelectedCatalogTest.SortOrder = CatalogTestSortOrder;
                SelectedCatalogTest.Price = CatalogTestPrice;

                Log.Information("Admin updated TestType {TypeId}: {TestName}", SelectedCatalogTest.TypeId, CatalogTestName);

                MessageBox.Show("Test type saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save test type.");
                MessageBox.Show("Error saving test type.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteAddCatalogTestAsync(object obj)
        {
            if (string.IsNullOrWhiteSpace(CatalogTestName))
            {
                MessageBox.Show("Please enter a name for the new test type.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var newTest = new TestType
                {
                    Name = CatalogTestName,
                    Unit = CatalogTestUnit,
                    ReferenceRangeLow = CatalogTestLow,
                    ReferenceRangeHigh = CatalogTestHigh,
                    IsActive = CatalogTestIsActive,
                    Category = CatalogTestCategory,
                    GroupName = CatalogTestGroupName,
                    Method = CatalogTestMethod,
                    Interpretation = CatalogTestInterpretation,
                    SortOrder = CatalogTestSortOrder,
                    Price = CatalogTestPrice
                };

                await _testTypeRepo.AddAsync(newTest);
                Log.Information("Admin added new TestType: {TestName}", CatalogTestName);

                MessageBox.Show("New test type added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadDataAsync();
                SelectedCatalogTest = newTest;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add test type.");
                MessageBox.Show("Error adding test type.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadCatalogTestsForDepartmentAsync()
        {
            if (SelectedDepartment == null) return;
            try
            {
                var testTypes = await _testTypeRepo.GetAllAsync();
                CatalogTestTypes.Clear();
                var filtered = testTypes
                    .Where(t => t.DepartmentId == SelectedDepartment.DepartmentId)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Name);

                foreach (var t in filtered)
                {
                    CatalogTestTypes.Add(t);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load catalog tests for department.");
            }
        }
    }
}
