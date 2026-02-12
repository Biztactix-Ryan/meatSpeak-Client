using System.Diagnostics;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using MeatSpeak.Client.Core.Helpers;
using MeatSpeak.Client.Core.State;

namespace MeatSpeak.Client.Helpers;

public static partial class IrcTextRenderer
{
    private static readonly IBrush[] MircPalette =
    [
        new SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF")), // 0  White
        new SolidColorBrush(Avalonia.Media.Color.Parse("#000000")), // 1  Black
        new SolidColorBrush(Avalonia.Media.Color.Parse("#00007F")), // 2  Navy
        new SolidColorBrush(Avalonia.Media.Color.Parse("#009300")), // 3  Green
        new SolidColorBrush(Avalonia.Media.Color.Parse("#FF0000")), // 4  Red
        new SolidColorBrush(Avalonia.Media.Color.Parse("#7F0000")), // 5  Brown
        new SolidColorBrush(Avalonia.Media.Color.Parse("#9C009C")), // 6  Purple
        new SolidColorBrush(Avalonia.Media.Color.Parse("#FC7F00")), // 7  Orange
        new SolidColorBrush(Avalonia.Media.Color.Parse("#FFFF00")), // 8  Yellow
        new SolidColorBrush(Avalonia.Media.Color.Parse("#00FC00")), // 9  LightGreen
        new SolidColorBrush(Avalonia.Media.Color.Parse("#009393")), // 10 Teal
        new SolidColorBrush(Avalonia.Media.Color.Parse("#00FFFF")), // 11 Cyan
        new SolidColorBrush(Avalonia.Media.Color.Parse("#0000FC")), // 12 Blue
        new SolidColorBrush(Avalonia.Media.Color.Parse("#FF00FF")), // 13 Pink
        new SolidColorBrush(Avalonia.Media.Color.Parse("#7F7F7F")), // 14 Grey
        new SolidColorBrush(Avalonia.Media.Color.Parse("#D2D2D2")), // 15 LightGrey
    ];

    private static readonly IBrush LinkBrush = new SolidColorBrush(Avalonia.Media.Color.Parse("#00AFF4"));
    private static readonly IBrush MutedBrush = new SolidColorBrush(Avalonia.Media.Color.Parse("#72767D"));

    [GeneratedRegex(@"https?://[^\s<>""{}|\\^\[\]`]+")]
    private static partial Regex UrlRegex();

    public static List<Inline> RenderMessage(string content, ChatMessageType type)
    {
        var inlines = new List<Inline>();

        switch (type)
        {
            case ChatMessageType.System:
            case ChatMessageType.Join:
            case ChatMessageType.Part:
            case ChatMessageType.Quit:
            case ChatMessageType.Kick:
            case ChatMessageType.NickChange:
            case ChatMessageType.TopicChange:
            case ChatMessageType.ModeChange:
                var stripped = IrcTextFormatter.StripFormatting(content);
                AddUrlAwareRun(inlines, stripped, foreground: MutedBrush);
                return inlines;
        }

        bool isAction = type == ChatMessageType.Action;
        var segments = IrcTextFormatter.ParseFormatted(content);

        foreach (var segment in segments)
        {
            AddSegmentInlines(inlines, segment, isAction);
        }

        return inlines;
    }

    private static void AddSegmentInlines(List<Inline> inlines, FormattedSegment segment, bool isAction)
    {
        var urlMatches = UrlRegex().Matches(segment.Text);
        if (urlMatches.Count == 0)
        {
            inlines.Add(CreateRun(segment.Text, segment, isAction));
            return;
        }

        int lastEnd = 0;
        foreach (Match match in urlMatches)
        {
            if (match.Index > lastEnd)
            {
                inlines.Add(CreateRun(segment.Text[lastEnd..match.Index], segment, isAction));
            }

            inlines.Add(CreateUrlInline(match.Value, segment, isAction));
            lastEnd = match.Index + match.Length;
        }

        if (lastEnd < segment.Text.Length)
        {
            inlines.Add(CreateRun(segment.Text[lastEnd..], segment, isAction));
        }
    }

    private static Run CreateRun(string text, FormattedSegment segment, bool isAction)
    {
        var run = new Run(text);

        if (segment.IsBold)
            run.FontWeight = FontWeight.Bold;

        if (segment.IsItalic || isAction)
            run.FontStyle = FontStyle.Italic;

        if (segment.IsUnderline && segment.IsStrikethrough)
            run.TextDecorations = [..TextDecorations.Underline, ..TextDecorations.Strikethrough];
        else if (segment.IsUnderline)
            run.TextDecorations = TextDecorations.Underline;
        else if (segment.IsStrikethrough)
            run.TextDecorations = TextDecorations.Strikethrough;

        if (segment.IsMonospace)
            run.FontFamily = new FontFamily("Consolas, Courier New, monospace");

        if (segment.ForegroundColor is { } fgIdx)
            run.Foreground = MircPalette[fgIdx];

        if (segment.BackgroundColor is { } bgIdx)
            run.Background = MircPalette[bgIdx];

        return run;
    }

    private static InlineUIContainer CreateUrlInline(string url, FormattedSegment segment, bool isAction)
    {
        var textBlock = new TextBlock
        {
            Text = url,
            Foreground = LinkBrush,
            TextDecorations = TextDecorations.Underline,
            Cursor = new Cursor(StandardCursorType.Hand),
            FontSize = 14,
        };

        if (segment.IsBold)
            textBlock.FontWeight = FontWeight.Bold;
        if (segment.IsItalic || isAction)
            textBlock.FontStyle = FontStyle.Italic;
        if (segment.IsMonospace)
            textBlock.FontFamily = new FontFamily("Consolas, Courier New, monospace");

        textBlock.PointerPressed += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch
            {
                // Ignore failures opening browser
            }
        };

        return new InlineUIContainer(textBlock);
    }

    private static void AddUrlAwareRun(List<Inline> inlines, string text, IBrush? foreground = null)
    {
        var urlMatches = UrlRegex().Matches(text);
        if (urlMatches.Count == 0)
        {
            var run = new Run(text);
            if (foreground is not null) run.Foreground = foreground;
            inlines.Add(run);
            return;
        }

        int lastEnd = 0;
        foreach (Match match in urlMatches)
        {
            if (match.Index > lastEnd)
            {
                var run = new Run(text[lastEnd..match.Index]);
                if (foreground is not null) run.Foreground = foreground;
                inlines.Add(run);
            }

            var segment = new FormattedSegment();
            inlines.Add(CreateUrlInline(match.Value, segment, false));
            lastEnd = match.Index + match.Length;
        }

        if (lastEnd < text.Length)
        {
            var run = new Run(text[lastEnd..]);
            if (foreground is not null) run.Foreground = foreground;
            inlines.Add(run);
        }
    }
}
