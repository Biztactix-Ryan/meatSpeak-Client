using System.Globalization;
using Avalonia.Data.Converters;

namespace MeatSpeak.Client.Converters;

public class TimestampConverter : IValueConverter
{
    public static readonly TimestampConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTimeOffset dto)
        {
            var local = dto.ToLocalTime();
            var format = parameter as string ?? "HH:mm";
            return local.ToString(format);
        }
        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
