using MeatSpeak.Client.Core.Helpers;

namespace MeatSpeak.Client.Core.Tests.Helpers;

public class IrcTextFormatterTests
{
    // Use \u escapes (exactly 4 hex digits) to avoid C# \x greedily consuming hex chars
    private const char Bold = '\u0002';
    private const char Color = '\u0003';
    private const char Italic = '\u001D';
    private const char Underline = '\u001F';
    private const char Strikethrough = '\u001E';
    private const char Monospace = '\u0011';
    private const char Reset = '\u000F';
    private const char Reverse = '\u0016';

    [Fact]
    public void ParseFormatted_PlainText_SingleSegment()
    {
        var result = IrcTextFormatter.ParseFormatted("Hello world");

        Assert.Single(result);
        Assert.Equal("Hello world", result[0].Text);
        Assert.False(result[0].IsBold);
        Assert.False(result[0].IsItalic);
        Assert.False(result[0].IsUnderline);
        Assert.Null(result[0].ForegroundColor);
    }

    [Fact]
    public void ParseFormatted_Bold_TogglesCorrectly()
    {
        var input = $"normal{Bold}bold{Bold}normal";
        var result = IrcTextFormatter.ParseFormatted(input);

        Assert.Equal(3, result.Count);
        Assert.Equal("normal", result[0].Text);
        Assert.False(result[0].IsBold);
        Assert.Equal("bold", result[1].Text);
        Assert.True(result[1].IsBold);
        Assert.Equal("normal", result[2].Text);
        Assert.False(result[2].IsBold);
    }

    [Fact]
    public void ParseFormatted_Italic_TogglesCorrectly()
    {
        var input = $"normal{Italic}italic{Italic}normal";
        var result = IrcTextFormatter.ParseFormatted(input);

        Assert.Equal(3, result.Count);
        Assert.False(result[0].IsItalic);
        Assert.True(result[1].IsItalic);
        Assert.False(result[2].IsItalic);
    }

    [Fact]
    public void ParseFormatted_Underline_TogglesCorrectly()
    {
        var input = $"normal{Underline}underline{Underline}normal";
        var result = IrcTextFormatter.ParseFormatted(input);

        Assert.Equal(3, result.Count);
        Assert.False(result[0].IsUnderline);
        Assert.True(result[1].IsUnderline);
        Assert.False(result[2].IsUnderline);
    }

    [Fact]
    public void ParseFormatted_Strikethrough_TogglesCorrectly()
    {
        var input = $"normal{Strikethrough}strike{Strikethrough}normal";
        var result = IrcTextFormatter.ParseFormatted(input);

        Assert.Equal(3, result.Count);
        Assert.False(result[0].IsStrikethrough);
        Assert.True(result[1].IsStrikethrough);
        Assert.False(result[2].IsStrikethrough);
    }

    [Fact]
    public void ParseFormatted_ColorFgOnly_SingleDigit()
    {
        var input = $"{Color}4red text";
        var result = IrcTextFormatter.ParseFormatted(input);

        Assert.Single(result);
        Assert.Equal("red text", result[0].Text);
        Assert.Equal(4, result[0].ForegroundColor);
        Assert.Null(result[0].BackgroundColor);
    }

    [Fact]
    public void ParseFormatted_ColorFgOnly_TwoDigits()
    {
        var input = $"{Color}12blue text";
        var result = IrcTextFormatter.ParseFormatted(input);

        Assert.Single(result);
        Assert.Equal("blue text", result[0].Text);
        Assert.Equal(12, result[0].ForegroundColor);
    }

    [Fact]
    public void ParseFormatted_ColorFgAndBg()
    {
        var input = $"{Color}4,2red on navy";
        var result = IrcTextFormatter.ParseFormatted(input);

        Assert.Single(result);
        Assert.Equal("red on navy", result[0].Text);
        Assert.Equal(4, result[0].ForegroundColor);
        Assert.Equal(2, result[0].BackgroundColor);
    }

    [Fact]
    public void ParseFormatted_ColorReset_BareControlC()
    {
        var input = $"{Color}4red{Color}normal";
        var result = IrcTextFormatter.ParseFormatted(input);

        Assert.Equal(2, result.Count);
        Assert.Equal("red", result[0].Text);
        Assert.Equal(4, result[0].ForegroundColor);
        Assert.Equal("normal", result[1].Text);
        Assert.Null(result[1].ForegroundColor);
    }

    [Fact]
    public void ParseFormatted_Reset_ClearsAllFormatting()
    {
        var input = $"{Bold}{Italic}{Color}4bold italic red{Reset}normal";
        var result = IrcTextFormatter.ParseFormatted(input);

        Assert.Equal(2, result.Count);
        Assert.True(result[0].IsBold);
        Assert.True(result[0].IsItalic);
        Assert.Equal(4, result[0].ForegroundColor);
        Assert.False(result[1].IsBold);
        Assert.False(result[1].IsItalic);
        Assert.Null(result[1].ForegroundColor);
    }

    [Fact]
    public void ParseFormatted_Reverse_SwapsColors()
    {
        var input = $"{Color}4,2red on navy{Reverse}reversed";
        var result = IrcTextFormatter.ParseFormatted(input);

        Assert.Equal(2, result.Count);
        Assert.Equal(4, result[0].ForegroundColor);
        Assert.Equal(2, result[0].BackgroundColor);
        Assert.Equal(2, result[1].ForegroundColor);
        Assert.Equal(4, result[1].BackgroundColor);
    }

    [Fact]
    public void ParseFormatted_CombinedFormats()
    {
        var input = $"{Bold}{Italic}{Underline}formatted{Reset}";
        var result = IrcTextFormatter.ParseFormatted(input);

        Assert.Single(result);
        Assert.True(result[0].IsBold);
        Assert.True(result[0].IsItalic);
        Assert.True(result[0].IsUnderline);
    }

    [Fact]
    public void ParseFormatted_ColorClamping()
    {
        // 99 % 16 = 3
        var input = $"{Color}99text";
        var result = IrcTextFormatter.ParseFormatted(input);

        Assert.Single(result);
        Assert.Equal(3, result[0].ForegroundColor);
    }

    [Fact]
    public void ParseFormatted_EmptyInput_ReturnsEmpty()
    {
        var result = IrcTextFormatter.ParseFormatted("");
        Assert.Empty(result);
    }

    [Fact]
    public void ParseFormatted_OnlyFormatCodes_ReturnsEmpty()
    {
        var input = $"{Bold}{Italic}{Reset}";
        var result = IrcTextFormatter.ParseFormatted(input);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseFormatted_Monospace_TogglesCorrectly()
    {
        var input = $"normal{Monospace}mono{Monospace}normal";
        var result = IrcTextFormatter.ParseFormatted(input);

        Assert.Equal(3, result.Count);
        Assert.False(result[0].IsMonospace);
        Assert.True(result[1].IsMonospace);
        Assert.False(result[2].IsMonospace);
    }

    [Fact]
    public void ParseFormatted_ColorFgCommaNoBackground_SetsOnlyFg()
    {
        // \x034, followed by non-digit — comma consumed but no bg digits
        var input = $"{Color}4,text";
        var result = IrcTextFormatter.ParseFormatted(input);

        Assert.Single(result);
        Assert.Equal(4, result[0].ForegroundColor);
        // Comma consumed but no bg digits found, bg stays null
        Assert.Null(result[0].BackgroundColor);
    }

    [Fact]
    public void ParseFormatted_MultipleColorChanges()
    {
        // red text, then blue text, then reset to normal
        var input = $"{Color}4red {Color}12blue {Color}normal";
        var result = IrcTextFormatter.ParseFormatted(input);

        Assert.Equal(3, result.Count);
        Assert.Equal("red ", result[0].Text);
        Assert.Equal(4, result[0].ForegroundColor);
        Assert.Equal("blue ", result[1].Text);
        Assert.Equal(12, result[1].ForegroundColor);
        Assert.Equal("normal", result[2].Text);
        Assert.Null(result[2].ForegroundColor);
    }

    [Fact]
    public void ParseFormatted_Reverse_WithNoColorsSet_SwapsNulls()
    {
        var input = $"{Reverse}text";
        var result = IrcTextFormatter.ParseFormatted(input);

        Assert.Single(result);
        Assert.Equal("text", result[0].Text);
        // Both were null, swapping nulls still gives nulls
        Assert.Null(result[0].ForegroundColor);
        Assert.Null(result[0].BackgroundColor);
    }

    [Fact]
    public void ParseFormatted_Reverse_WithOnlyFgSet_MovesToBg()
    {
        // Set fg=4, then reverse → fg=null, bg=4
        var input = $"{Color}4{Reverse}text";
        var result = IrcTextFormatter.ParseFormatted(input);

        Assert.Single(result);
        Assert.Null(result[0].ForegroundColor);
        Assert.Equal(4, result[0].BackgroundColor);
    }

    [Fact]
    public void ParseFormatted_BgColorClamping()
    {
        // fg=4, bg=99 → bg = 99 % 16 = 3
        var input = $"{Color}4,99text";
        var result = IrcTextFormatter.ParseFormatted(input);

        Assert.Single(result);
        Assert.Equal(4, result[0].ForegroundColor);
        Assert.Equal(3, result[0].BackgroundColor);
    }

    [Fact]
    public void ParseFormatted_FormattingPersistsAcrossText()
    {
        // Bold on, then color, bold stays on
        var input = $"{Bold}bold {Color}4bold and red";
        var result = IrcTextFormatter.ParseFormatted(input);

        Assert.Equal(2, result.Count);
        Assert.Equal("bold ", result[0].Text);
        Assert.True(result[0].IsBold);
        Assert.Null(result[0].ForegroundColor);
        Assert.Equal("bold and red", result[1].Text);
        Assert.True(result[1].IsBold);
        Assert.Equal(4, result[1].ForegroundColor);
    }

    [Fact]
    public void ParseFormatted_ConsecutiveToggles_CancelOut()
    {
        // Bold on then immediately off before any text
        var input = $"{Bold}{Bold}normal";
        var result = IrcTextFormatter.ParseFormatted(input);

        Assert.Single(result);
        Assert.Equal("normal", result[0].Text);
        Assert.False(result[0].IsBold);
    }
}
