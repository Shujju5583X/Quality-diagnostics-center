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
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                    if (OnSelectionChanged != null)
                    {
                        OnSelectionChanged();
                    }
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
        public int ResultId { get; set; }
        public ResultInputType InputType { get; set; }
        public string TestName { get; set; }
        public string Unit { get; set; }
        public double? Low { get; set; }
        public double? High { get; set; }
        public double? LowMale { get; set; }
        public double? HighMale { get; set; }
        public double? LowFemale { get; set; }
        public double? HighFemale { get; set; }
        public bool HasGenderSpecificRange
        {
            get { return LowMale.HasValue || HighMale.HasValue || LowFemale.HasValue || HighFemale.HasValue; }
        }

        public string RefRangeDisplay
        {
            get
            {
                if (HasGenderSpecificRange)
                {
                    string male = FormatRange(LowMale, HighMale);
                    string female = FormatRange(LowFemale, HighFemale);
                    return "M: " + male + " | F: " + female;
                }
                return FormatRange(Low, High);
            }
        }

        private string FormatRange(double? low, double? high)
        {
            if (low.HasValue && high.HasValue) return low.Value + " - " + high.Value;
            if (low.HasValue) return ">= " + low.Value;
            if (high.HasValue) return "< " + high.Value;
            return "N/A";
        }

        private string _valueText;
        public string ValueText
        {
            get { return _valueText; }
            set 
            { 
                _valueText = value; 
                OnPropertyChanged(); 
                OnPropertyChanged("DisplayValue");
                if (HasOptions && (_selectedOption == null || _selectedOption.Value != value))
                {
                    double parsedOVal;
                    double parsedVVal;
                    SelectedOption = Options.FirstOrDefault(o =>
                        o.Value == value ||
                        (double.TryParse(o.Value, out parsedOVal) && double.TryParse(value, out parsedVVal) && Math.Abs(parsedOVal - parsedVVal) < 0.001));
                }
            }
        }

        private bool _isAbnormal;
        public bool IsAbnormal
        {
            get { return _isAbnormal; }
            set { _isAbnormal = value; OnPropertyChanged(); }
        }

        private bool _isReadOnly;
        public bool IsReadOnly
        {
            get { return _isReadOnly; }
            set { _isReadOnly = value; OnPropertyChanged(); }
        }

        private bool _isAmendmentMode;
        public bool IsAmendmentMode
        {
            get { return _isAmendmentMode; }
            set { _isAmendmentMode = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ResultOption> Options { get; private set; }
        public bool HasOptions
        {
            get { return Options.Count > 0; }
        }

        public ResultInput()
        {
            Options = new ObservableCollection<ResultOption>();
        }

        private ResultOption _selectedOption;
        public ResultOption SelectedOption
        {
            get { return _selectedOption; }
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
                    double parsedOVal;
                    double parsedVVal;
                    var opt = Options.FirstOrDefault(o =>
                        o.Value == ValueText ||
                        (double.TryParse(o.Value, out parsedOVal) && double.TryParse(ValueText, out parsedVVal) && Math.Abs(parsedOVal - parsedVVal) < 0.001));
                    if (opt != null) return opt.Display;
                }
                return ValueText;
            }
        }

        public void NotifyRefRangeChanged()
        {
            OnPropertyChanged("RefRangeDisplay");
        }
    }
}
