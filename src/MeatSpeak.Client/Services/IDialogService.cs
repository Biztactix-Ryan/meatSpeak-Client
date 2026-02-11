namespace MeatSpeak.Client.Services;

public interface IDialogService
{
    Task<bool> ShowConfirmAsync(string title, string message);
    Task ShowAlertAsync(string title, string message);
}
