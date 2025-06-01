using System;
using System.Globalization;
using System.Windows.Data;

namespace better_saving.Converters
{
    public class BooleanToConnectionTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "DISCONNECT" : "CONNECT";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 