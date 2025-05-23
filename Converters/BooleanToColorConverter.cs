using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace better_saving.Converters
{
    public class BooleanToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string colorParams)
            {
                string[] colors = colorParams.Split(';');
                if (colors.Length == 2)
                {
                    string trueColor = colors[0];
                    string falseColor = colors[1];
                    
                    if (boolValue)
                    {
                        var brush = new BrushConverter().ConvertFrom(trueColor);
                        return brush ?? new SolidColorBrush(Colors.Black);
                    }
                    else
                    {
                        var brush = new BrushConverter().ConvertFrom(falseColor);
                        return brush ?? new SolidColorBrush(Colors.Black);
                    }
                }
            }
            var defaultBrush = new BrushConverter().ConvertFrom("#22272A");
            return defaultBrush ?? new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
