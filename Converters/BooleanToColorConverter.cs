using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
// Explicitly use System.Windows.Media for Brush and Brushes
using SWMedia = System.Windows.Media;

namespace better_saving.Converters
{
    public class BooleanToColorConverter : IValueConverter
    {
        public SWMedia.Brush? TrueColor { get; set; }
        public SWMedia.Brush? FalseColor { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (parameter is string resourceKeys)
                {
                    string[] keys = resourceKeys.Split(';');
                    if (keys.Length == 2)
                    {
                        // Use System.Windows.Application explicitly
                        var trueBrush = System.Windows.Application.Current.TryFindResource(keys[0]) as SWMedia.Brush;
                        var falseBrush = System.Windows.Application.Current.TryFindResource(keys[1]) as SWMedia.Brush;
                        // Use System.Windows.Media.Brushes explicitly
                        return boolValue ? (trueBrush ?? TrueColor ?? SWMedia.Brushes.Black) : (falseBrush ?? FalseColor ?? SWMedia.Brushes.Black);
                    }
                }
                // Fallback to properties if parameter is not correctly formatted or not provided
                // Use System.Windows.Media.Brushes explicitly
                return boolValue ? (TrueColor ?? SWMedia.Brushes.Black) : (FalseColor ?? SWMedia.Brushes.Black);
            }
            // Use System.Windows.Media.Brushes explicitly
            return SWMedia.Brushes.Black; // Default or error case
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
