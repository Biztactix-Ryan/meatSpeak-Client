namespace MeatSpeak.Client.Services;

public interface INotificationService
{
    void ShowNotification(string title, string message);
    void ShowError(string message);
    void ShowInfo(string message);
}
