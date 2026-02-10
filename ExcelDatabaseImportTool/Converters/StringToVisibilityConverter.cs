using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ExcelDatabaseImportTool.Converters
{
    /// <summary>
    /// Converts string values to Visibility (Visible if not null/empty, Collapsed otherwise)
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return string.IsNullOrWhiteSpace(stringValue) ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
