using System;
using System.Globalization;
using System.Windows.Data;

namespace PrometheusTaskbar.Converters;

public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            return false;
        }

        string enumValue = value.ToString() ?? string.Empty;
        string targetValue = parameter.ToString() ?? string.Empty;

        return enumValue.Equals(targetValue, StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            return Binding.DoNothing;
        }

        bool boolValue = (bool)value;
        string targetValue = parameter.ToString() ?? string.Empty;

        if (boolValue)
        {
            return Enum.Parse(targetType, targetValue);
        }

        return Binding.DoNothing;
    }
}
