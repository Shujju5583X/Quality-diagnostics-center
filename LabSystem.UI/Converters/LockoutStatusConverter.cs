using System;
using System.Globalization;
using System.Windows.Data;

namespace LabSystem.UI.Converters
{
    public class LockoutStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime)
            {
                var lockoutEnd = (DateTime)value;
                if (lockoutEnd > DateTime.UtcNow)
                {
                    return "Locked";
                }
            }
            return "Active";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
