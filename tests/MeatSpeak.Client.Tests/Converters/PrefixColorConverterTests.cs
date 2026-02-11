using System.Globalization;
using Avalonia.Media;
using MeatSpeak.Client.Converters;

namespace MeatSpeak.Client.Tests.Converters;

public class PrefixColorConverterTests
{
    private readonly PrefixColorConverter _converter = new();

    [Fact]
    public void Convert_OperatorPrefix_ReturnsGold()
    {
        var result = _converter.Convert("@", typeof(IBrush), null, CultureInfo.InvariantCulture);

        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(Color.Parse("#FAA61A"), brush.Color);
    }

    [Fact]
    public void Convert_VoicedPrefix_ReturnsGreen()
    {
        var result = _converter.Convert("+", typeof(IBrush), null, CultureInfo.InvariantCulture);

        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(Color.Parse("#3BA55D"), brush.Color);
    }

    [Fact]
    public void Convert_EmptyPrefix_ReturnsMuted()
    {
        var result = _converter.Convert("", typeof(IBrush), null, CultureInfo.InvariantCulture);

        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(Color.Parse("#72767D"), brush.Color);
    }

    [Fact]
    public void Convert_NullValue_ReturnsMuted()
    {
        var result = _converter.Convert(null, typeof(IBrush), null, CultureInfo.InvariantCulture);

        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(Color.Parse("#72767D"), brush.Color);
    }

    [Fact]
    public void Convert_OperatorPlusVoicedPrefix_ReturnsGold()
    {
        // @ takes priority over +
        var result = _converter.Convert("@+", typeof(IBrush), null, CultureInfo.InvariantCulture);

        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(Color.Parse("#FAA61A"), brush.Color);
    }
}
