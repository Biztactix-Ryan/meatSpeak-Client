using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MeatSpeak.Client.Audio;
using MeatSpeak.Client.Audio.Stubs;
using MeatSpeak.Client.Core.Connection;
using MeatSpeak.Client.Core.Data;
using MeatSpeak.Client.Core.Handlers;
using MeatSpeak.Client.Core.Identity;
using MeatSpeak.Client.Core.State;
using MeatSpeak.Client.Services;
using MeatSpeak.Client.ViewModels;
using MeatSpeak.Client.Views;
using MeatSpeak.Identity.Trust;
using Microsoft.Extensions.DependencyInjection;

namespace MeatSpeak.Client;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MeatSpeak");
        Directory.CreateDirectory(dataDir);

        // Data layer
        var db = new ClientDatabase(Path.Combine(dataDir, "meatspeak.db"));
        services.AddSingleton(db);
        services.AddSingleton(new UserPreferences(db));

        // Identity
        var identityDir = Path.Combine(dataDir, "identity");
        var identityManager = new IdentityManager(identityDir);
        services.AddSingleton(identityManager);

        var tofuStore = new TofuStore(new FileTofuStore(Path.Combine(dataDir, "known_hosts.json")));
        services.AddSingleton(tofuStore);
        services.AddSingleton(new AuthenticationService(identityManager, tofuStore));

        // Audio (stubs for now)
        services.AddSingleton<IAudioCapture, NullAudioCapture>();
        services.AddSingleton<IAudioPlayback, NullAudioPlayback>();
        services.AddSingleton<IOpusCodec, NullOpusCodec>();
        services.AddSingleton<VoiceEngine>();

        // Connection
        services.AddSingleton<ClientState>();
        services.AddSingleton<ConnectionManager>(sp =>
            new ConnectionManager(CreateDispatcher));

        // Services
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ClipboardService>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<ServerListViewModel>();
        services.AddTransient<ChannelListViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<MemberListViewModel>();
        services.AddTransient<MessageInputViewModel>();
        services.AddTransient<ServerAddViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<VoiceStatusBarViewModel>();
    }

    private static MessageDispatcher CreateDispatcher()
    {
        var dispatcher = new MessageDispatcher();
        dispatcher.Register(new PingPongHandler());
        dispatcher.Register(new RegistrationHandler());
        dispatcher.Register(new CapNegotiationHandler());
        dispatcher.Register(new NumericHandler());
        dispatcher.Register(new ChannelHandler());
        dispatcher.Register(new MessageHandler());
        dispatcher.Register(new NickHandler());
        dispatcher.Register(new ModeHandler());
        dispatcher.Register(new QuitPartHandler());
        dispatcher.Register(new CtcpHandler());
        dispatcher.Register(new ErrorHandler());
        dispatcher.Register(new SaslHandler());
        dispatcher.Register(new VoiceStateHandler());
        dispatcher.Register(new VoiceNumericHandler());
        return dispatcher;
    }
}
