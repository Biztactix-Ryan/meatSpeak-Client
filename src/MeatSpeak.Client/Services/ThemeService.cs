using Avalonia;
using Avalonia.Styling;
using MeatSpeak.Client.Core.Data;

namespace MeatSpeak.Client.Services;

public sealed class ThemeService : IThemeService
{
    private readonly UserPreferences _preferences;

    public ThemeVariant CurrentTheme { get; private set; } = ThemeVariant.Dark;

    public event Action<ThemeVariant>? ThemeChanged;

    public ThemeService(UserPreferences preferences)
    {
        _preferences = preferences;
        var saved = _preferences.Theme;
        CurrentTheme = saved == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
    }

    public void SetTheme(ThemeVariant theme)
    {
        CurrentTheme = theme;
        _preferences.Theme = theme == ThemeVariant.Light ? "Light" : "Dark";

        if (Application.Current is not null)
            Application.Current.RequestedThemeVariant = theme;

        ThemeChanged?.Invoke(theme);
    }

    public void ToggleTheme()
    {
        SetTheme(CurrentTheme == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark);
    }
}
