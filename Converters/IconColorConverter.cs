using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using better_saving.Models;

namespace better_saving.Converters
{
    // Converter that takes a JobState and returns a color for the icon
    public class IconColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {            if (value is JobStates state)
            {                return state switch
                {
                    JobStates.Working => (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1F547A"), // Working blue
                    JobStates.Finished => (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#009951"), // Finished green
                    JobStates.Paused => (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#22272A"), // Stopped/idle dark
                    JobStates.Failed => (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#7A201F"), // Failed red
                    JobStates.Stopped => (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#22272A"), // Idle dark
                    _ => (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#22272A"), // Default dark
                };
            }
            return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#22272A");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}