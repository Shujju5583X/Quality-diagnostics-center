using System;
using System.Collections.ObjectModel;
using System.Linq;
using LabSystem.Core.Enums;

namespace LabSystem.UI.ViewModels
{
    // Helper classes for lists and bindings
    public class TestTypeSelection : ViewModelBase
    {
        public int TypeId { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public double? Low { get; set; }
        public double? High { get; set; }
        public string GroupName { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public string RefRangeMale { get; set; }
        public string RefRangeFemale { get; set; }
        public Action OnSelectionChanged { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                    OnSelectionChanged?.Invoke();
                }
            }
        }
    }

    public class ResultOption
    {
        public string Display { get; set; }
        public string Value { get; set; }
    }

    public class ResultInput : ViewModelBase
    {
        public int TypeId { get; set; }
        public ResultInputType InputType { get; set; }
        public string TestName { get; set; }
        public string Unit { get; set; }
        public double? Low { get; set; }
        public double? High { get; set; }

        private string _valueText;
        public string ValueText
        {
            get => _valueText;
            set 
            { 
                _valueText = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(DisplayValue));
                if (HasOptions && (_selectedOption == null || _selectedOption.Value != value))
                {
                    SelectedOption = Options.FirstOrDefault(o => o.Value == value || Math.Abs((double.TryParse(o.Value, out var v1) ? v1 : -1) - (double.TryParse(value, out var v2) ? v2 : -2)) < 0.001);
                }
            }
        }

        private bool _isAbnormal;
        public bool IsAbnormal
        {
            get => _isAbnormal;
            set { _isAbnormal = value; OnPropertyChanged(); }
        }

        private bool _isReadOnly;
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set { _isReadOnly = value; OnPropertyChanged(); }
        }

        private bool _isAmendmentMode;
        public bool IsAmendmentMode
        {
            get => _isAmendmentMode;
            set { _isAmendmentMode = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ResultOption> Options { get; } = new ObservableCollection<ResultOption>();
        public bool HasOptions => Options.Count > 0;

        private ResultOption _selectedOption;
        public ResultOption SelectedOption
        {
            get => _selectedOption;
            set
            {
                _selectedOption = value;
                OnPropertyChanged();
                if (_selectedOption != null && ValueText != _selectedOption.Value)
                {
                    ValueText = _selectedOption.Value;
                }
            }
        }

        public string DisplayValue
        {
            get
            {
                if (HasOptions)
                {
                    var opt = Options.FirstOrDefault(o => o.Value == ValueText || Math.Abs((double.TryParse(o.Value, out var v1) ? v1 : -1) - (double.TryParse(ValueText, out var v2) ? v2 : -2)) < 0.001);
                    if (opt != null) return opt.Display;
                }
                return ValueText;
            }
        }
    }
}
