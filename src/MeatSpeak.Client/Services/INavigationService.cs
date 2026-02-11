namespace MeatSpeak.Client.Services;

public interface INavigationService
{
    void NavigateToServer(string connectionId);
    void NavigateToChannel(string connectionId, string channelName);
    void NavigateToPm(string connectionId, string nick);
    void NavigateToSettings();
}
