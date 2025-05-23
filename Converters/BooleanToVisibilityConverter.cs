using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace better_saving.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool bValue = false;
            if (value is bool)
            {
                bValue = (bool)value;
            }
            else if (value is bool?)
            {
                bool? bNullable = (bool?)value;
                bValue = bNullable.HasValue ? bNullable.Value : false;
            }

            // Invert visibility if "invert" parameter is passed
            if (parameter?.ToString()?.Equals("invert", StringComparison.CurrentCultureIgnoreCase) == true)
            {
                bValue = !bValue;
            }

            return bValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
            {
                bool bValue = (Visibility)value == Visibility.Visible;
                if (parameter?.ToString()?.ToLower() == "invert")
                {
                    bValue = !bValue;
                }
                return bValue;
            }
            return false;
        }
    }
}
