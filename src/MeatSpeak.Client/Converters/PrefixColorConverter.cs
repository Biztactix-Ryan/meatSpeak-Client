using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MeatSpeak.Client.Converters;

public class PrefixColorConverter : IValueConverter
{
    public static readonly PrefixColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string prefix)
        {
            if (prefix.Contains('@'))
                return SolidColorBrush.Parse("#FAA61A");
            if (prefix.Contains('+'))
                return SolidColorBrush.Parse("#3BA55D");
        }
        return SolidColorBrush.Parse("#72767D");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
