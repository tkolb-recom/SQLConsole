using System.Globalization;
using System.Windows.Data;

namespace Recom.SQLConsole.UI;

public class NullToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null; // Returns true for non-null, false for null
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }
}