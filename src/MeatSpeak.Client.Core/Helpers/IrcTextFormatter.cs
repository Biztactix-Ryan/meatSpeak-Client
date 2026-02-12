using System.Text;

namespace MeatSpeak.Client.Core.Helpers;

public sealed record FormattedSegment
{
    public string Text { get; init; } = string.Empty;
    public bool IsBold { get; init; }
    public bool IsItalic { get; init; }
    public bool IsUnderline { get; init; }
    public bool IsStrikethrough { get; init; }
    public bool IsMonospace { get; init; }
    public int? ForegroundColor { get; init; }
    public int? BackgroundColor { get; init; }
    public bool IsUrl { get; init; }
}

public static class IrcTextFormatter
{
    // mIRC color codes
    private const char Bold = '\x02';
    private const char Color = '\x03';
    private const char Italic = '\x1D';
    private const char Underline = '\x1F';
    private const char Strikethrough = '\x1E';
    private const char Monospace = '\x11';
    private const char Reset = '\x0F';
    private const char Reverse = '\x16';

    public static List<FormattedSegment> ParseFormatted(string input)
    {
        var segments = new List<FormattedSegment>();
        var textBuf = new StringBuilder();

        bool bold = false, italic = false, underline = false, strikethrough = false, monospace = false;
        int? fg = null, bg = null;

        void Flush()
        {
            if (textBuf.Length == 0) return;
            segments.Add(new FormattedSegment
            {
                Text = textBuf.ToString(),
                IsBold = bold,
                IsItalic = italic,
                IsUnderline = underline,
                IsStrikethrough = strikethrough,
                IsMonospace = monospace,
                ForegroundColor = fg,
                BackgroundColor = bg,
            });
            textBuf.Clear();
        }

        int i = 0;
        while (i < input.Length)
        {
            var c = input[i];
            switch (c)
            {
                case Bold:
                    Flush();
                    bold = !bold;
                    i++;
                    break;
                case Italic:
                    Flush();
                    italic = !italic;
                    i++;
                    break;
                case Underline:
                    Flush();
                    underline = !underline;
                    i++;
                    break;
                case Strikethrough:
                    Flush();
                    strikethrough = !strikethrough;
                    i++;
                    break;
                case Monospace:
                    Flush();
                    monospace = !monospace;
                    i++;
                    break;
                case Reverse:
                    Flush();
                    (fg, bg) = (bg, fg);
                    i++;
                    break;
                case Reset:
                    Flush();
                    bold = italic = underline = strikethrough = monospace = false;
                    fg = bg = null;
                    i++;
                    break;
                case Color:
                    Flush();
                    i++;
                    if (i < input.Length && char.IsDigit(input[i]))
                    {
                        int fgVal = input[i] - '0';
                        i++;
                        if (i < input.Length && char.IsDigit(input[i]))
                        {
                            fgVal = fgVal * 10 + (input[i] - '0');
                            i++;
                        }
                        fg = fgVal % 16;

                        if (i < input.Length && input[i] == ',')
                        {
                            i++;
                            if (i < input.Length && char.IsDigit(input[i]))
                            {
                                int bgVal = input[i] - '0';
                                i++;
                                if (i < input.Length && char.IsDigit(input[i]))
                                {
                                    bgVal = bgVal * 10 + (input[i] - '0');
                                    i++;
                                }
                                bg = bgVal % 16;
                            }
                        }
                    }
                    else
                    {
                        fg = bg = null;
                    }
                    break;
                default:
                    textBuf.Append(c);
                    i++;
                    break;
            }
        }

        Flush();
        return segments;
    }

    public static string StripFormatting(string input)
    {
        var sb = new StringBuilder(input.Length);
        int i = 0;

        while (i < input.Length)
        {
            var c = input[i];

            switch (c)
            {
                case Bold:
                case Italic:
                case Underline:
                case Strikethrough:
                case Monospace:
                case Reset:
                case Reverse:
                    i++;
                    break;

                case Color:
                    i++;
                    // Skip foreground color digits (up to 2)
                    if (i < input.Length && char.IsDigit(input[i]))
                    {
                        i++;
                        if (i < input.Length && char.IsDigit(input[i])) i++;

                        // Skip optional background color
                        if (i < input.Length && input[i] == ',')
                        {
                            i++;
                            if (i < input.Length && char.IsDigit(input[i]))
                            {
                                i++;
                                if (i < input.Length && char.IsDigit(input[i])) i++;
                            }
                        }
                    }
                    break;

                default:
                    sb.Append(c);
                    i++;
                    break;
            }
        }

        return sb.ToString();
    }

    public static bool IsActionMessage(string content) =>
        content.StartsWith("\u0001ACTION ") && content.EndsWith('\u0001');

    public static string ExtractAction(string content)
    {
        if (!IsActionMessage(content)) return content;
        return content[8..^1]; // Strip \u0001ACTION  and trailing \u0001
    }

    public static string FormatAction(string text) =>
        $"\u0001ACTION {text}\u0001";
}
