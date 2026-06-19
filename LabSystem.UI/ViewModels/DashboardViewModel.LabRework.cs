using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LabSystem.Core.Models;
using LabSystem.Core.Enums;
using LabSystem.Core.Services;
using LabSystem.Core.Interfaces;

namespace LabSystem.UI.ViewModels
{
    public class DepartmentSelection : ViewModelBase
    {
        public int DepartmentId { get; set; }
        public string Name { get; set; }

        private readonly Action<DepartmentSelection> _onSelectionChanged;
        public DepartmentSelection(Action<DepartmentSelection> onSelectionChanged = null)
        {
            _onSelectionChanged = onSelectionChanged;
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                    if (_onSelectionChanged != null)
                    {
                        _onSelectionChanged(this);
                    }
                }
            }
        }
    }

    public class PackageSelection : ViewModelBase
    {
        public int PanelId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public virtual ICollection<TestType> TestTypes { get; set; }

        private readonly Action<PackageSelection> _onSelectionChanged;
        public PackageSelection(Action<PackageSelection> onSelectionChanged = null)
        {
            _onSelectionChanged = onSelectionChanged;
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                    if (_onSelectionChanged != null)
                    {
                        _onSelectionChanged(this);
                    }
                }
            }
        }
    }

    public partial class DashboardViewModel
    {
        // 1. Doctors For Order Selection
        private Doctor _selectedDoctorForOrder;
        public Doctor SelectedDoctorForOrder
        {
            get { return _selectedDoctorForOrder; }
            set { _selectedDoctorForOrder = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Doctor> DoctorsForOrder { get; private set; }

        // 2. Select Tests Filter & Navigation
        private DepartmentSelection _selectedDepartmentForOrder;
        public DepartmentSelection SelectedDepartmentForOrder
        {
            get { return _selectedDepartmentForOrder; }
            set
            {
                _selectedDepartmentForOrder = value;
                OnPropertyChanged();
                if (value != null)
                {
                    SelectedPackageForOrder = null;
                }
                OnPropertyChanged("FilteredTestTypesForOrder");
                OnPropertyChanged("IsCategoryFilterActive");
            }
        }

        private PackageSelection _selectedPackageForOrder;
        public PackageSelection SelectedPackageForOrder
        {
            get { return _selectedPackageForOrder; }
            set
            {
                _selectedPackageForOrder = value;
                OnPropertyChanged();
                if (value != null)
                {
                    SelectedDepartmentForOrder = null;
                }
                OnPropertyChanged("FilteredTestTypesForOrder");
                OnPropertyChanged("IsCategoryFilterActive");
            }
        }

        private string _testSearchQuery;
        public string TestSearchQuery
        {
            get { return _testSearchQuery; }
            set
            {
                _testSearchQuery = value;
                OnPropertyChanged();
                OnPropertyChanged("FilteredTestTypesForOrder");
            }
        }

        public bool IsCategoryFilterActive
        {
            get { return SelectedDepartmentForOrder != null || SelectedPackageForOrder != null; }
        }

        public ICommand ClearCategoryFiltersCommand { get; private set; }

        public ObservableCollection<DepartmentSelection> DepartmentsForOrder { get; private set; }
        public ObservableCollection<PackageSelection> PackagesForOrder { get; private set; }

        public IEnumerable<TestTypeSelection> FilteredTestTypesForOrder
        {
            get
            {
                IEnumerable<TestTypeSelection> query = TestTypes;

                if (SelectedDepartmentForOrder != null)
                {
                    query = query.Where(t => string.Equals(t.Category, SelectedDepartmentForOrder.Name, StringComparison.OrdinalIgnoreCase));
                }
                else if (SelectedPackageForOrder != null && SelectedPackageForOrder.TestTypes != null)
                {
                    var typeIds = new HashSet<int>(SelectedPackageForOrder.TestTypes.Select(t => t.TypeId));
                    query = query.Where(t => typeIds.Contains(t.TypeId));
                }

                if (!string.IsNullOrWhiteSpace(TestSearchQuery))
                {
                    query = query.Where(t => t.Name != null && t.Name.IndexOf(TestSearchQuery, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                return query.ToList();
            }
        }

        // 3. Selection Summary and Totals
        public decimal OrderRunningTotal
        {
            get
            {
                var selectedTestTypeIds = new HashSet<int>(TestTypes.Where(t => t.IsSelected).Select(t => t.TypeId));
                decimal total = 0;
                var testTypesAppliedToPanels = new HashSet<int>();

                // Sort packages/panels by number of tests descending to match larger panels first
                foreach (var panel in TestPanels.OrderByDescending(p => p.TestTypes.Count))
                {
                    var panelTestTypeIds = panel.TestTypes.Select(t => t.TypeId).ToList();
                    if (panelTestTypeIds.Count > 0 && panelTestTypeIds.All(id => selectedTestTypeIds.Contains(id) && !testTypesAppliedToPanels.Contains(id)))
                    {
                        total += panel.Price;
                        foreach (var id in panelTestTypeIds)
                        {
                            testTypesAppliedToPanels.Add(id);
                        }
                    }
                }

                // Add remaining individual test type prices
                foreach (var t in TestTypes.Where(t => t.IsSelected))
                {
                    if (!testTypesAppliedToPanels.Contains(t.TypeId))
                    {
                        total += t.Price;
                    }
                }

                return total;
            }
        }

        public IEnumerable<TestTypeSelection> SelectedTestTypesSummary
        {
            get { return TestTypes.Where(t => t.IsSelected); }
        }

        // 4. Report Generation Tab Properties
        private string _reportSearchQuery;
        public string ReportSearchQuery
        {
            get { return _reportSearchQuery; }
            set
            {
                _reportSearchQuery = value;
                OnPropertyChanged();
                OnPropertyChanged("CompleteOrders");
            }
        }

        private TestOrder _selectedReportOrder;
        public TestOrder SelectedReportOrder
        {
            get { return _selectedReportOrder; }
            set { _selectedReportOrder = value; OnPropertyChanged(); }
        }

        public IEnumerable<TestOrder> CompleteOrders
        {
            get
            {
                var completeQuery = Orders.Where(o => o.StatusEnum == OrderStatus.Complete);
                if (string.IsNullOrWhiteSpace(ReportSearchQuery))
                    return completeQuery;

                return completeQuery.Where(o =>
                    (o.Patient.FullName != null && o.Patient.FullName.IndexOf(ReportSearchQuery, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    o.OrderId.ToString().Contains(ReportSearchQuery));
            }
        }

        // 5. IPdfReportService public getter
        public IPdfReportService ReportService
        {
            get { return _reportService; }
        }

        // 6. Synchronization logic
        private bool _updatingSelections = false;

        private void InitializeRework()
        {
            // Initial placeholder or setups
            DepartmentsForOrder = new ObservableCollection<DepartmentSelection>();
            PackagesForOrder = new ObservableCollection<PackageSelection>();
            DoctorsForOrder = new ObservableCollection<Doctor>();

            ClearCategoryFiltersCommand = new RelayCommand(o => ExecuteClearCategoryFilters());
        }

        private void ExecuteClearCategoryFilters()
        {
            SelectedDepartmentForOrder = null;
            SelectedPackageForOrder = null;
        }

        private void LoadDataRework(IEnumerable<Doctor> doctorsList, IEnumerable<Department> departmentsList, IEnumerable<TestPanel> panels)
        {
            // 1. Populate Doctors for Order
            DoctorsForOrder.Clear();
            DoctorsForOrder.Add(new Doctor { DoctorId = 0, FullName = "Self" });
            foreach (var doc in doctorsList.OrderBy(d => d.FullName))
            {
                DoctorsForOrder.Add(doc);
            }
            SelectedDoctorForOrder = DoctorsForOrder.FirstOrDefault();

            // 2. Populate Departments for Order
            _updatingSelections = true;
            try
            {
                DepartmentsForOrder.Clear();
                foreach (var dept in departmentsList.OrderBy(d => d.Name))
                {
                    if (DepartmentsForOrder.Any(d => string.Equals(d.Name, dept.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }
                    DepartmentsForOrder.Add(new DepartmentSelection(OnDepartmentSelectionChanged)
                    {
                        DepartmentId = dept.DepartmentId,
                        Name = dept.Name,
                        IsSelected = false
                    });
                }

                // 3. Populate Packages for Order
                PackagesForOrder.Clear();
                foreach (var p in panels.OrderBy(x => x.Name))
                {
                    if (PackagesForOrder.Any(pkg => string.Equals(pkg.Name, p.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }
                    PackagesForOrder.Add(new PackageSelection(OnPackageSelectionChanged)
                    {
                        PanelId = p.PanelId,
                        Name = p.Name,
                        Price = p.Price,
                        Description = p.Description,
                        TestTypes = p.TestTypes,
                        IsSelected = false
                    });
                }
            }
            finally
            {
                _updatingSelections = false;
            }

            // Sync test selection callbacks
            // We need to re-initialize TestTypes collection callbacks or subscribe to them
            // Wait, in DashboardViewModel.cs, we populated TestTypes as new TestTypeSelection
            // To ensure we get selection callbacks, we should wrap/re-populate TestTypes using our handler
            // But a cleaner way: we can just handle selection change on any property in TestTypeSelection, 
            // or modify TestTypeSelection to invoke an action. Since we added action support to TestTypeSelection,
            // we will modify the population loop in DashboardViewModel.cs to pass OnTestSelectionChanged!
        }

        public void OnTestSelectionChanged()
        {
            if (_updatingSelections) return;
            _updatingSelections = true;
            try
            {
                // Sync departments check state
                foreach (var deptSel in DepartmentsForOrder)
                {
                    var testsInDept = TestTypes.Where(t => t.Category == deptSel.Name).ToList();
                    if (testsInDept.Any())
                    {
                        deptSel.IsSelected = testsInDept.All(t => t.IsSelected);
                    }
                }

                // Sync packages check state
                foreach (var pkgSel in PackagesForOrder)
                {
                    if (pkgSel.TestTypes != null && pkgSel.TestTypes.Any())
                    {
                        var typeIds = new HashSet<int>(pkgSel.TestTypes.Select(t => t.TypeId));
                        var testsInPkg = TestTypes.Where(t => typeIds.Contains(t.TypeId)).ToList();
                        pkgSel.IsSelected = testsInPkg.All(t => t.IsSelected);
                    }
                }
            }
            finally
            {
                _updatingSelections = false;
                OnPropertyChanged("OrderRunningTotal");
                OnPropertyChanged("SelectedTestTypesSummary");
            }
        }

        private void OnDepartmentSelectionChanged(DepartmentSelection deptSel)
        {
            if (_updatingSelections) return;

            if (deptSel.IsSelected)
            {
                SelectedDepartmentForOrder = deptSel;
            }

            _updatingSelections = true;
            try
            {
                var testsInDept = TestTypes.Where(t => t.Category == deptSel.Name);
                foreach (var t in testsInDept)
                {
                    t.IsSelected = deptSel.IsSelected;
                }
            }
            finally
            {
                _updatingSelections = false;
                OnPropertyChanged("OrderRunningTotal");
                OnPropertyChanged("SelectedTestTypesSummary");
            }
        }

        private void OnPackageSelectionChanged(PackageSelection pkgSel)
        {
            if (_updatingSelections) return;

            if (pkgSel.IsSelected)
            {
                SelectedPackageForOrder = pkgSel;
            }

            _updatingSelections = true;
            try
            {
                if (pkgSel.TestTypes != null)
                {
                    var typeIds = new HashSet<int>(pkgSel.TestTypes.Select(t => t.TypeId));
                    foreach (var t in TestTypes)
                    {
                        if (typeIds.Contains(t.TypeId))
                        {
                            t.IsSelected = pkgSel.IsSelected;
                        }
                    }
                }
            }
            finally
            {
                _updatingSelections = false;
                OnPropertyChanged("OrderRunningTotal");
                OnPropertyChanged("SelectedTestTypesSummary");
            }
        }

        private string GetRangeString(TestType t, string gender)
        {
            var r = t.ReferenceRanges != null ? t.ReferenceRanges.FirstOrDefault(x => string.Equals(x.Gender, gender, StringComparison.OrdinalIgnoreCase)) : null;
            if (r != null)
            {
                if (r.RangeLow.HasValue && r.RangeHigh.HasValue) return r.RangeLow.Value + " - " + r.RangeHigh.Value + " " + t.Unit;
                if (r.RangeLow.HasValue) return ">= " + r.RangeLow.Value + " " + t.Unit;
                if (r.RangeHigh.HasValue) return "<" + r.RangeHigh.Value + " " + t.Unit;
            }
            if (t.ReferenceRangeLow.HasValue && t.ReferenceRangeHigh.HasValue) return t.ReferenceRangeLow.Value + " - " + t.ReferenceRangeHigh.Value + " " + t.Unit;
            if (t.ReferenceRangeLow.HasValue) return ">= " + t.ReferenceRangeLow.Value + " " + t.Unit;
            if (t.ReferenceRangeHigh.HasValue) return "<" + t.ReferenceRangeHigh.Value + " " + t.Unit;
            return "N/A";
        }
    }
}
