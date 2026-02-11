# MeatSpeak Client

A Discord-like desktop IRC client built with [Avalonia UI](https://avaloniaui.net/). Connects to multiple servers simultaneously — both standard IRC servers (Libera Chat, OFTC, UnrealIRCd, etc.) and MeatSpeak servers with voice and Ed25519 authentication.

![.NET 9.0](https://img.shields.io/badge/.NET-9.0-purple) ![Avalonia 11.2](https://img.shields.io/badge/Avalonia-11.2-blue) ![License](https://img.shields.io/badge/license-proprietary-red)

## Features

- **Multi-server** — Connect to multiple IRC servers at once, switch between them in a Discord-style sidebar
- **Standard IRC compatible** — Works with any RFC 2812 / IRCv3 server out of the box
- **MeatSpeak extensions** — Ed25519 mutual authentication, voice channels, E2E encryption (auto-detected via CAP negotiation)
- **SASL authentication** — SASL PLAIN support for NickServ on standard IRC servers
- **IRCv3 support** — CAP negotiation, server-time tags, ISUPPORT (005) feature detection
- **CTCP** — Responds to VERSION, PING, TIME; handles ACTION (/me) messages
- **Dark and light themes** — Discord-inspired color palette with theme switching
- **Local persistence** — SQLite database for server profiles, message cache, TOFU key pins, and user preferences
- **Slash commands** — `/join`, `/part`, `/nick`, `/me`, `/msg`, `/topic`, `/kick`, `/mode`, `/whois`, `/list`, `/invite`, `/quit`, `/raw`
- **Voice UI shell** — Voice channel interface scaffolded and ready for audio backend integration

## Architecture

```
MeatSpeak.Client                 Avalonia UI application (views, viewmodels, themes)
├── MeatSpeak.Client.Core        Business logic, no UI dependencies
│   ├── Connection/              TCP/TLS connection, line buffering, message send/receive
│   ├── Handlers/                15 IRC message handlers (registration, channels, messaging, etc.)
│   ├── State/                   Reactive state models (servers, channels, users, messages)
│   ├── Identity/                Ed25519 keypair management, TOFU verification
│   ├── Data/                    SQLite persistence (profiles, message cache, preferences)
│   └── Helpers/                 Nick coloring, IRC text formatting
└── MeatSpeak.Client.Audio       Voice interfaces + null stubs (audio implementation deferred)
```

### Message Flow

```
TCP Stream → IrcLineBuffer → IrcMessageReceiver → MessageDispatcher → Handler → State Update → ViewModel → UI
```

All state objects implement `INotifyPropertyChanged` via CommunityToolkit.Mvvm, so the UI updates reactively when handlers modify server state.

## UI Layout

```
┌──────────────┬──────────────────┬──────────────────────────────────┐
│ Server Icons │ Channel List     │ Chat + Members                   │
│              │                  │                                  │
│  [S1] ←      │ TEXT CHANNELS    │ #general                         │
│  [S2]       │   # general  ←   │                                  │
│  [S3]       │   # random       │ [alice] hey everyone              │
│              │   # dev          │ [bob] yo!                        │
│              │                  │                                  │
│  [+] Add    │ VOICE CHANNELS   │ ────────────                     │
│  [⚙] Settings│ (MeatSpeak only) │ Members (3)                     │
│              │ DMs              │   @alice · +bob · charlie        │
│              │   bob            │                                  │
│              │   charlie        │ [Message input...              ] │
├──────────────┴──────────────────┴──────────────────────────────────┤
│ 🔊 Voice status bar (shown when in voice on MeatSpeak server)     │
└───────────────────────────────────────────────────────────────────┘
```

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Sibling repositories (for project references):
  - `meatSpeak-server` at `../meatSpeak-server/` — provides `MeatSpeak.Protocol`
  - `meatSpeak` at `../meatSpeak/` — provides `MeatSpeak.Identity`

Expected directory layout:

```
parent/
├── meatSpeak/                  # MeatSpeak.Identity library
├── meatSpeak-server/           # MeatSpeak.Protocol library
└── meatSpeak-client/           # This repository
```

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run --project src/MeatSpeak.Client
```

## Testing

```bash
dotnet test
```

70 tests across two test projects covering connection handling, IRC message parsing, handler behavior, state management, ISUPPORT parsing, TOFU verification, and voice connection framing.

## Project Structure

```
src/
├── MeatSpeak.Client/                    Avalonia desktop application
│   ├── Views/                           AXAML views (10 views)
│   │   ├── MainWindow                   3-panel shell layout
│   │   ├── ServerListView               Vertical server icon strip
│   │   ├── ChannelListView              Channel/DM tree for selected server
│   │   ├── ChatView                     Virtualized message list
│   │   ├── MemberListView               Channel member sidebar
│   │   ├── MessageInputView             Text input with slash commands
│   │   ├── MessageBubble                Individual message template
│   │   ├── ServerAddDialog              Add/edit server connection
│   │   ├── SettingsView                 User preferences
│   │   └── VoiceStatusBar               Voice connection info bar
│   ├── ViewModels/                      MVVM viewmodels (10 viewmodels)
│   ├── Controls/                        Custom controls (ServerIcon, ChannelTreeItem, UserBadge)
│   ├── Converters/                      Value converters (visibility, timestamps, nick colors)
│   ├── Services/                        Navigation, notifications, themes, dialogs, clipboard
│   └── Themes/                          Dark theme, light theme, shared color palette
│
├── MeatSpeak.Client.Core/              Business logic (no UI dependencies)
│   ├── Connection/
│   │   ├── ServerConnection             TCP/TLS lifecycle, registration, auto-reconnect
│   │   ├── ConnectionManager            Multi-server hub
│   │   ├── IrcLineBuffer                PipeReader-based async line framing
│   │   ├── IrcMessageSender             Thread-safe IRC line sending
│   │   └── IrcMessageReceiver           Background read loop with events
│   ├── Handlers/                        IRC message handlers (15 total)
│   │   ├── RegistrationHandler          NICK+USER+CAP → Connected
│   │   ├── CapNegotiationHandler        CAP LS/REQ/ACK/END, MeatSpeak detection
│   │   ├── PingPongHandler              Auto PONG
│   │   ├── NumericHandler               001-005, MOTD, ISUPPORT, NAMES, TOPIC
│   │   ├── ChannelHandler               JOIN, PART, KICK, TOPIC
│   │   ├── MessageHandler               PRIVMSG, NOTICE
│   │   ├── NickHandler                  Nick changes
│   │   ├── ModeHandler                  Channel/user modes
│   │   ├── QuitPartHandler              QUIT removal
│   │   ├── CtcpHandler                  VERSION, PING, TIME, ACTION
│   │   ├── ErrorHandler                 ERROR + numeric errors
│   │   ├── SaslHandler                  SASL PLAIN authentication
│   │   ├── VoiceStateHandler            VOICESTATE broadcasts (MeatSpeak)
│   │   └── VoiceNumericHandler          900-905 voice numerics (MeatSpeak)
│   ├── State/                           Reactive state models
│   │   ├── ClientState                  Root: collection of ServerStates
│   │   ├── ServerState                  Per-server: channels, PMs, nick, capabilities
│   │   ├── ChannelState                 Per-channel: messages, members, topic, modes
│   │   ├── PrivateMessageState          Per-PM conversation
│   │   ├── UserState                    Per-user: nick, prefix, away status
│   │   ├── VoiceChannelState            Voice members, mute/deaf/speaking
│   │   ├── ChatMessage                  Message model with type enum
│   │   └── IsupportTokens               Parsed ISUPPORT (CHANTYPES, PREFIX, NETWORK, etc.)
│   ├── Identity/
│   │   ├── IdentityManager              Ed25519 keypair load/generate/store
│   │   ├── AuthenticationService         Mutual auth flow (wraps MeatSpeak.Identity)
│   │   └── TofuStore                    Trust-on-first-use key pin storage
│   ├── Data/
│   │   ├── ClientDatabase               SQLite schema + CRUD operations
│   │   ├── ServerProfile                Saved server configuration
│   │   ├── MessageCacheEntry            Offline message cache
│   │   └── UserPreferences              Theme, notification, display settings
│   └── Helpers/
│       ├── NickColorGenerator            Deterministic color from nick hash
│       └── IrcTextFormatter              Strip mIRC formatting, ACTION handling
│
└── MeatSpeak.Client.Audio/              Voice (interfaces + stubs)
    ├── IAudioCapture                     Microphone capture interface
    ├── IAudioPlayback                    Speaker playback interface
    ├── IAudioMixer                       Multi-stream audio mixer
    ├── IOpusCodec                        Opus encode/decode interface
    ├── IAudioDeviceEnumerator            Audio device listing
    ├── VoiceEngine                       Capture → encode → send orchestrator
    ├── VoiceConnection                   UDP voice packet transport
    └── Stubs/                            No-op implementations
        ├── NullAudioCapture
        ├── NullAudioPlayback
        └── NullOpusCodec

tests/
├── MeatSpeak.Client.Core.Tests/         64 tests
│   ├── Connection/                      IrcLineBuffer, ServerConnection
│   ├── Handlers/                        All handler tests
│   ├── State/                           ServerState, ChannelState, IsupportTokens
│   └── Identity/                        TofuStore
└── MeatSpeak.Client.Audio.Tests/        6 tests
    └── VoiceConnectionTests
```

## Dependencies

| Package | Version | Used In | Purpose |
|---------|---------|---------|---------|
| Avalonia | 11.2.3 | Client | Cross-platform UI framework |
| Avalonia.Desktop | 11.2.3 | Client | Desktop windowing |
| Avalonia.Themes.Fluent | 11.2.3 | Client | Fluent design theme |
| Avalonia.Fonts.Inter | 11.2.3 | Client | Inter font family |
| CommunityToolkit.Mvvm | 8.4.0 | Client, Core | ObservableObject, RelayCommand |
| Microsoft.Extensions.DependencyInjection | 9.0.0 | Client | DI container |
| Microsoft.Data.Sqlite | 9.0.0 | Core | Local SQLite database |
| System.Reactive | 6.0.1 | Core | Reactive extensions |
| Sodium.Core | 1.4.0 | Audio | XChaCha20-Poly1305 voice encryption |
| MeatSpeak.Protocol | project ref | Core, Audio | IRC message parsing and building |
| MeatSpeak.Identity | project ref | Core | Ed25519 identity and authentication |

## Standard IRC vs MeatSpeak

The client auto-detects server type during CAP negotiation. No special configuration needed.

| Feature | Standard IRC | MeatSpeak |
|---------|:---:|:---:|
| Text channels | Yes | Yes |
| Private messages | Yes | Yes |
| Nick/mode/topic/kick/ban | Yes | Yes |
| IRCv3 CAP negotiation | Yes | Yes |
| CTCP (VERSION, PING, ACTION) | Yes | Yes |
| SSL/TLS | Yes | Yes |
| SASL authentication | Yes | -- |
| Ed25519 mutual auth | -- | Yes |
| Voice channels | -- | Yes |
| E2E encryption | -- | Yes |

## Roadmap

- [ ] Audio backend implementation (NAudio/PortAudio/OpenAL)
- [ ] Opus codec integration
- [ ] Voice activity detection (VAD)
- [ ] Jitter buffer for incoming audio
- [ ] Markdown rendering in chat
- [ ] Desktop notifications
- [ ] System tray with unread counts
- [ ] Auto-reconnect with exponential backoff
- [ ] Nick tab-completion
- [ ] Channel search (Ctrl+K)

## Related Repositories

- [meatSpeak](https://github.com/Biztactix-Ryan/meatSpeak) — Identity library (Ed25519 keypairs, mutual auth, TOFU, DNS resolution)
- [meatSpeak-server](https://github.com/Biztactix-Ryan/meatSpeak-server) — IRC + Voice server with protocol library

## License

Proprietary. All rights reserved.
