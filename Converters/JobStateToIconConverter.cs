using System;
using System.Globalization;
using System.Windows.Data;
using better_saving.Models; // Assuming JobStates is in this namespace
using System.Windows.Media.Imaging;
using System.IO;

namespace better_saving.Converters
{
    public class JobStateToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {            if (value is JobStates state)
            {
                return state switch
                {
                    JobStates.Working => new Uri("pack://application:,,,/Assets/Icons/job-working.svg#svgColor=#ffffff", UriKind.Absolute), // Green
                    JobStates.Finished => new Uri("pack://application:,,,/Assets/Icons/job-finished.svg#svgColor=#ffffff", UriKind.Absolute), // Blue
                    JobStates.Stopped => new Uri("pack://application:,,,/Assets/Icons/pause.svg#svgColor=#ffffff", UriKind.Absolute), // Amber
                    JobStates.Failed => new Uri("pack://application:,,,/Assets/Icons/job-error.svg#svgColor=#ffffff", UriKind.Absolute), // Red
                    JobStates.Idle => new Uri("pack://application:,,,/Assets/Icons/job-idle.svg#svgColor=#ffffff", UriKind.Absolute), // Gray
                    _ => new Uri("pack://application:,,,/Assets/Icons/job-idle.svg#svgColor=#ffffff", UriKind.Absolute),
                };
            }
            return new Uri("pack://application:,,,/Assets/Icons/job-idle.svg#svgColor=#ffffff", UriKind.Absolute); // Default icon if conversion fails
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
