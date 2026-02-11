namespace MeatSpeak.Client.Core.Data;

public sealed class UserPreferences
{
    private readonly ClientDatabase _db;

    public UserPreferences(ClientDatabase db)
    {
        _db = db;
    }

    public string Theme
    {
        get => _db.GetPreference("theme") ?? "Dark";
        set => _db.SetPreference("theme", value);
    }

    public bool ShowTimestamps
    {
        get => _db.GetPreference("show_timestamps") != "false";
        set => _db.SetPreference("show_timestamps", value.ToString().ToLowerInvariant());
    }

    public bool DesktopNotifications
    {
        get => _db.GetPreference("desktop_notifications") != "false";
        set => _db.SetPreference("desktop_notifications", value.ToString().ToLowerInvariant());
    }

    public bool NotifyOnMention
    {
        get => _db.GetPreference("notify_on_mention") != "false";
        set => _db.SetPreference("notify_on_mention", value.ToString().ToLowerInvariant());
    }

    public bool NotifyOnPm
    {
        get => _db.GetPreference("notify_on_pm") != "false";
        set => _db.SetPreference("notify_on_pm", value.ToString().ToLowerInvariant());
    }

    public int MaxMessagesPerChannel
    {
        get => int.TryParse(_db.GetPreference("max_messages_per_channel"), out var v) ? v : 500;
        set => _db.SetPreference("max_messages_per_channel", value.ToString());
    }

    public string TimestampFormat
    {
        get => _db.GetPreference("timestamp_format") ?? "HH:mm";
        set => _db.SetPreference("timestamp_format", value);
    }
}
