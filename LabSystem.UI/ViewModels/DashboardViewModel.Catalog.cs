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
                OnPropertyChanged("IsEditingCatalogTest");
                OnPropertyChanged("IsNotEditingCatalogTest");
                OnPropertyChanged("CatalogFormTitle");
                PopulateCatalogEditFields();
            }
        }

        public bool IsEditingCatalogTest
        {
            get { return SelectedCatalogTest != null; }
        }

        public bool IsNotEditingCatalogTest
        {
            get { return !IsEditingCatalogTest; }
        }

        public string CatalogFormTitle
        {
            get { return IsEditingCatalogTest ? "Edit Test" : "Add New Test"; }
        }

        private Department _catalogTestDepartment;
        public Department CatalogTestDepartment
        {
            get { return _catalogTestDepartment; }
            set { _catalogTestDepartment = value; OnPropertyChanged(); }
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

        public string CatalogTestInstrument
        {
            get { return _catalogTestInstrument; }
            set { _catalogTestInstrument = value; OnPropertyChanged(); }
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

        private bool _catalogHasBesideRefRanges;
        public bool CatalogHasBesideRefRanges
        {
            get { return _catalogHasBesideRefRanges; }
            set { _catalogHasBesideRefRanges = value; OnPropertyChanged(); }
        }

        private bool _catalogHasTextRefRanges;
        public bool CatalogHasTextRefRanges
        {
            get { return _catalogHasTextRefRanges; }
            set { _catalogHasTextRefRanges = value; OnPropertyChanged(); }
        }

        private string _catalogTextReferenceString;
        public string CatalogTextReferenceString
        {
            get { return _catalogTextReferenceString; }
            set { _catalogTextReferenceString = value; OnPropertyChanged(); }
        }

        private string _catalogTextReferenceNormalValue;
        public string CatalogTextReferenceNormalValue
        {
            get { return _catalogTextReferenceNormalValue; }
            set { _catalogTextReferenceNormalValue = value; OnPropertyChanged(); }
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
                CatalogTestInstrument = string.Empty;
                CatalogTestInterpretation = string.Empty;
                CatalogTestSortOrder = 0;
                CatalogTestPrice = 0;
                CatalogHasBesideRefRanges = false;
                CatalogHasTextRefRanges = false;
                CatalogTextReferenceString = string.Empty;
                CatalogTextReferenceNormalValue = string.Empty;
                CatalogTestDepartment = SelectedDepartment;
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
            CatalogTestInstrument = SelectedCatalogTest.Instrument;
            CatalogTestInterpretation = SelectedCatalogTest.Interpretation;
            CatalogTestSortOrder = SelectedCatalogTest.SortOrder;
            CatalogTestPrice = SelectedCatalogTest.Price;
            CatalogHasBesideRefRanges = SelectedCatalogTest.HasBesideRefRanges;
            CatalogHasTextRefRanges = SelectedCatalogTest.HasTextRefRanges;
            CatalogTextReferenceString = SelectedCatalogTest.TextReferenceString;
            CatalogTextReferenceNormalValue = SelectedCatalogTest.TextReferenceNormalValue;
            CatalogTestDepartment = Departments.FirstOrDefault(d => d.DepartmentId == SelectedCatalogTest.DepartmentId);
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

                var targetDeptId = ResolveTargetDepartmentId();

                entityToUpdate.Name = CatalogTestName;
                entityToUpdate.Unit = CatalogTestUnit;
                entityToUpdate.ReferenceRangeLow = CatalogTestLow;
                entityToUpdate.ReferenceRangeHigh = CatalogTestHigh;
                entityToUpdate.IsActive = CatalogTestIsActive;
                entityToUpdate.Category = CatalogTestCategory;
                entityToUpdate.GroupName = CatalogTestGroupName;
                entityToUpdate.Method = CatalogTestMethod;
                entityToUpdate.Instrument = CatalogTestInstrument;
                entityToUpdate.Interpretation = CatalogTestInterpretation;
                entityToUpdate.SortOrder = CatalogTestSortOrder;
                entityToUpdate.Price = CatalogTestPrice;
                entityToUpdate.HasBesideRefRanges = CatalogHasBesideRefRanges;
                entityToUpdate.HasTextRefRanges = CatalogHasTextRefRanges;
                entityToUpdate.TextReferenceString = CatalogTextReferenceString;
                entityToUpdate.TextReferenceNormalValue = CatalogTextReferenceNormalValue;
                entityToUpdate.DepartmentId = targetDeptId;

                await _testTypeRepo.UpdateAsync(entityToUpdate);

                SelectedCatalogTest.Name = CatalogTestName;
                SelectedCatalogTest.Unit = CatalogTestUnit;
                SelectedCatalogTest.ReferenceRangeLow = CatalogTestLow;
                SelectedCatalogTest.ReferenceRangeHigh = CatalogTestHigh;
                SelectedCatalogTest.IsActive = CatalogTestIsActive;
                SelectedCatalogTest.Category = CatalogTestCategory;
                SelectedCatalogTest.GroupName = CatalogTestGroupName;
                SelectedCatalogTest.Method = CatalogTestMethod;
                SelectedCatalogTest.Instrument = CatalogTestInstrument;
                SelectedCatalogTest.Interpretation = CatalogTestInterpretation;
                SelectedCatalogTest.SortOrder = CatalogTestSortOrder;
                SelectedCatalogTest.Price = CatalogTestPrice;
                SelectedCatalogTest.HasBesideRefRanges = CatalogHasBesideRefRanges;
                SelectedCatalogTest.HasTextRefRanges = CatalogHasTextRefRanges;
                SelectedCatalogTest.TextReferenceString = CatalogTextReferenceString;
                SelectedCatalogTest.TextReferenceNormalValue = CatalogTextReferenceNormalValue;
                SelectedCatalogTest.DepartmentId = targetDeptId;

                Log.Information("Admin updated TestType {TypeId}: {TestName}", SelectedCatalogTest.TypeId, CatalogTestName);

                MessageBox.Show("Test type saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshCatalogStateAsync(targetDeptId, entityToUpdate.TypeId);
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
                var targetDeptId = ResolveTargetDepartmentId();

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
                    Instrument = CatalogTestInstrument,
                    Interpretation = CatalogTestInterpretation,
                    SortOrder = CatalogTestSortOrder,
                    Price = CatalogTestPrice,
                    HasBesideRefRanges = CatalogHasBesideRefRanges,
                    HasTextRefRanges = CatalogHasTextRefRanges,
                    TextReferenceString = CatalogTextReferenceString,
                    TextReferenceNormalValue = CatalogTextReferenceNormalValue,
                    DepartmentId = targetDeptId
                };

                await _testTypeRepo.AddAsync(newTest);
                Log.Information("Admin added new TestType: {TestName}", CatalogTestName);

                MessageBox.Show("New test type added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshCatalogStateAsync(targetDeptId, newTest.TypeId);
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

        private async Task RefreshCatalogStateAsync(int? selectDepartmentId, int? selectTestTypeId)
        {
            await LoadDataAsync();

            _selectedDepartment = selectDepartmentId.HasValue 
                ? Departments.FirstOrDefault(d => d.DepartmentId == selectDepartmentId.Value)
                : null;
            OnPropertyChanged("SelectedDepartment");

            if (_selectedDepartment != null)
            {
                await LoadCatalogTestsForDepartmentAsync();
            }
            else
            {
                var testTypes = await _testTypeRepo.GetAllAsync();
                CatalogTestTypes.Clear();
                foreach (var t in testTypes.OrderBy(x => x.SortOrder).ThenBy(x => x.Name))
                {
                    CatalogTestTypes.Add(t);
                }
            }

            SelectedCatalogTest = selectTestTypeId.HasValue
                ? CatalogTestTypes.FirstOrDefault(t => t.TypeId == selectTestTypeId.Value)
                : null;
        }

        private int? ResolveTargetDepartmentId()
        {
            if (CatalogTestDepartment != null)
            {
                CatalogTestCategory = CatalogTestDepartment.Name;
                return CatalogTestDepartment.DepartmentId;
            }
            return null;
        }
    }
}
