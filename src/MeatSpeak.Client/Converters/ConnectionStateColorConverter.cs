using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using MeatSpeak.Client.Core.Connection;

namespace MeatSpeak.Client.Converters;

public class ConnectionStateColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ConnectionState state)
        {
            return state switch
            {
                ConnectionState.Connected or ConnectionState.Authenticated => SolidColorBrush.Parse("#3BA55D"),
                ConnectionState.Connecting or ConnectionState.Registering or ConnectionState.Reconnecting => SolidColorBrush.Parse("#FAA61A"),
                ConnectionState.Error => SolidColorBrush.Parse("#ED4245"),
                _ => SolidColorBrush.Parse("#747F8D"),
            };
        }
        return SolidColorBrush.Parse("#747F8D");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
