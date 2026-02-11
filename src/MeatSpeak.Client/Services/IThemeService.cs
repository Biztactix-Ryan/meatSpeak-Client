using Avalonia.Styling;

namespace MeatSpeak.Client.Services;

public interface IThemeService
{
    ThemeVariant CurrentTheme { get; }
    void SetTheme(ThemeVariant theme);
    void ToggleTheme();
}
