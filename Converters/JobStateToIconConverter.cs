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
        {            
            // Check if the parameter is IsPausing flag
            bool isPaused = parameter != null && parameter is bool isPausingParam && isPausingParam;

            if (value is JobStates state)
            {
                // If the job is in Stopped state and isPaused is true, use job-idle.svg
                if (state == JobStates.Stopped && isPaused)
                {
                    return new Uri("pack://application:,,,/Assets/Icons/job-idle.svg#svgColor=#ffffff", UriKind.Absolute);
                }

                return state switch
                {
                    JobStates.Working => new Uri("pack://application:,,,/Assets/Icons/job-working.svg#svgColor=#ffffff", UriKind.Absolute), // Green
                    JobStates.Finished => new Uri("pack://application:,,,/Assets/Icons/job-finished.svg#svgColor=#ffffff", UriKind.Absolute), // Blue
                    JobStates.Stopped => new Uri("pack://application:,,,/Assets/Icons/job-idle.svg#svgColor=#ffffff", UriKind.Absolute), // Changed from pause.svg to job-idle.svg
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
