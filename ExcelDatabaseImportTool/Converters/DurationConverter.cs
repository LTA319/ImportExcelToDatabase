using System.Globalization;
using System.Windows.Data;

namespace ExcelDatabaseImportTool.Converters
{
    /// <summary>
    /// Converts start and end times to a duration TimeSpan
    /// </summary>
    public class DurationConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is DateTime endTime && values[1] is DateTime startTime)
            {
                return endTime - startTime;
            }
            return TimeSpan.Zero;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
