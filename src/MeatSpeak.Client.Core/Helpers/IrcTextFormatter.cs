using System.Text;

namespace MeatSpeak.Client.Core.Helpers;

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
