using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using MeatSpeak.Client.Core.Helpers;

namespace MeatSpeak.Client.Converters;

public class NickColorConverter : IValueConverter
{
    public static readonly NickColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string nick && !string.IsNullOrEmpty(nick))
        {
            var hex = NickColorGenerator.GetColor(nick);
            return SolidColorBrush.Parse(hex);
        }
        return Brushes.White;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
