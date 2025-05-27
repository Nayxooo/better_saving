using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace better_saving.Converters
{
    public class BooleanToCommandConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isConnected = (bool)value;
            return isConnected ? parameter : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 