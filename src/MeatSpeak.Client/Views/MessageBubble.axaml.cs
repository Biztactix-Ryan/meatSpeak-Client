using Avalonia.Controls;
using MeatSpeak.Client.Core.State;
using MeatSpeak.Client.Helpers;

namespace MeatSpeak.Client.Views;

public partial class MessageBubble : UserControl
{
    public MessageBubble()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is ChatMessage message)
        {
            ContentTextBlock.Inlines?.Clear();
            var inlines = IrcTextRenderer.RenderMessage(message.Content, message.Type);
            foreach (var inline in inlines)
                ContentTextBlock.Inlines?.Add(inline);
        }
    }
}
