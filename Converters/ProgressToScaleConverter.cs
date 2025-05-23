using System;
using System.Globalization;
using System.Windows.Data;

namespace better_saving.Converters
{
    /// <summary>
    /// Converts a progress value (0-100) to a scale value (0-1) for the progress bar
    /// </summary>
    public class ProgressToScaleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {            // Default minimum width (twice the corner radius = 20px)
            const double MIN_WIDTH = 20.0;

            if (values.Length >= 2)
            {
                double progressValue;
                
                // Handle different numeric types (float or double)
                if (values[0] is float floatValue)
                {
                    progressValue = (double)floatValue;
                }
                else if (values[0] is double doubleValue)
                {
                    progressValue = doubleValue;
                }
                else if (values[0] != null && double.TryParse(values[0].ToString(), out double parsedValue))
                {
                    progressValue = parsedValue;
                }
                else
                {
                    return 0.0;
                }
                
                // Get the track width
                double trackWidth;
                if (values[1] is double width)
                {
                    trackWidth = width;
                }
                else if (values[1] != null && double.TryParse(values[1].ToString(), out double parsedWidth))
                {
                    trackWidth = parsedWidth;
                }
                else
                {
                    return 0.0;
                }

                // Ensure value is within bounds
                if (progressValue < 0)
                    progressValue = 0;
                if (progressValue > 100)
                    progressValue = 100;

                // Calculate the actual width based on percentage
                double calculatedWidth = (progressValue / 100.0) * trackWidth;

                // Ensure minimum width when progress > 0
                if (progressValue > 0 && calculatedWidth < MIN_WIDTH)
                    return MIN_WIDTH;

                return calculatedWidth;
            }

            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
