using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace better_saving.Converters
{
    /// <summary>
    /// Converts a progress value to the appropriate width for the progress indicator
    /// </summary>
    public class ValueToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 4 ||
                values[0] == null || values[1] == null || values[2] == null || values[3] == null ||
                !double.TryParse(values[0].ToString(), out double value) ||
                !double.TryParse(values[1].ToString(), out double minimum) ||
                !double.TryParse(values[2].ToString(), out double maximum) ||
                !double.TryParse(values[3].ToString(), out double actualWidth))
            {
                return 0.0;
            }

            double percent;
            if (maximum - minimum == 0)
                percent = 0;
            else
                percent = (value - minimum) / (maximum - minimum);

            return actualWidth * percent;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
