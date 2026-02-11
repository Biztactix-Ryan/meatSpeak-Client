using Avalonia.Controls;
using Avalonia.Input;
using MeatSpeak.Client.ViewModels;

namespace MeatSpeak.Client.Views;

public partial class MessageInputView : UserControl
{
    public MessageInputView()
    {
        InitializeComponent();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            if (DataContext is MessageInputViewModel vm)
            {
                vm.SendMessageCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
