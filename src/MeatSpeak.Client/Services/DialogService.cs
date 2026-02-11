using Avalonia.Controls;

namespace MeatSpeak.Client.Services;

public sealed class DialogService : IDialogService
{
    public Task<bool> ShowConfirmAsync(string title, string message)
    {
        // Simple implementation — in production, show a dialog window
        return Task.FromResult(true);
    }

    public Task ShowAlertAsync(string title, string message)
    {
        return Task.CompletedTask;
    }
}
