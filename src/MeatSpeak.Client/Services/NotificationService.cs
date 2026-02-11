namespace MeatSpeak.Client.Services;

public sealed class NotificationService : INotificationService
{
    public event Action<string, string>? NotificationRaised;
    public event Action<string>? ErrorRaised;

    public void ShowNotification(string title, string message)
    {
        NotificationRaised?.Invoke(title, message);
    }

    public void ShowError(string message)
    {
        ErrorRaised?.Invoke(message);
    }

    public void ShowInfo(string message)
    {
        NotificationRaised?.Invoke("Info", message);
    }
}
