# MusicLibraryManager

A cross-platform desktop music library manager built with [Uno Platform](https://platform.uno/). Browse your local music collection, edit metadata, manage album art, and stream tracks from SoundCloud — all from one app.

## Features

### Local Music Library
- **Folder browsing** — open any folder and view all audio tracks in a sortable list
- **Metadata editing** — edit title, artist, album, album artist, composer, genre, year, track number, and comments (ID3 tags)
- **Album art management** — view, copy, paste, replace, or remove cover art
- **Audio playback** — play/pause, seek, and volume control with persistent settings
- **Change detection** — warns before navigating away from unsaved edits

### SoundCloud Integration
- **OAuth sign-in** — authenticate with your SoundCloud account
- **Search** — find tracks across SoundCloud's catalog
- **Streaming playback** — stream tracks directly with full playback controls

## Tech Stack

| Layer | Technology |
|---|---|
| UI Framework | [Uno Platform](https://platform.uno/) (WinUI / XAML) |
| Runtime | .NET 10 |
| MVVM | [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) |
| Audio Playback | [NAudio](https://github.com/naudio/NAudio) |
| Metadata | [TagLibSharp](https://github.com/mono/taglib-sharp) |
| Rendering | Uno Skia Renderer |
| Design System | Material Design (Uno Toolkit) |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Uno Platform tooling — run `dotnet tool install -g uno.check && uno-check` to verify your environment

## Getting Started

```bash
git clone https://github.com/<your-username>/MusicLibraryManager.git
cd MusicLibraryManager

dotnet restore
dotnet build

# Run on desktop
dotnet run -f net10.0-desktop --project MusicLibraryManager/MusicLibraryManager.csproj
```

## Project Structure

```
MusicLibraryManager/
├── Presentation/          XAML pages and user controls
│   └── SoundCloud/        SoundCloud-specific UI
├── ViewModels/            MVVM ViewModels (ObservableObject, RelayCommand)
│   └── SoundCloud/        SoundCloud-specific ViewModels
├── Services/              Business logic interfaces
│   └── Concrete/          Service implementations
├── Models/                Data models (Track, SoundCloudTrack, etc.)
├── Converters/            XAML value converters
├── Styles/                Color palette and theme overrides
├── Assets/                App icons and splash screens
└── Platforms/Desktop/     Desktop entry point
```

## Architecture

The app follows **MVVM** with constructor-based **dependency injection** via `Microsoft.Extensions.DependencyInjection`. All services and ViewModels are registered at startup in `App.xaml.cs` and resolved through constructor injection.

### MVVM Layer

ViewModels inherit from `ObservableObject` (CommunityToolkit.Mvvm) and use source-generated `[ObservableProperty]` attributes for bindable properties and `[RelayCommand]` for commands. XAML views bind directly to ViewModel properties — no code-behind logic beyond pointer event forwarding for the seek slider.

### Playback

Playback is abstracted behind two interfaces:

- **`IPlaybackService`** — generic playback contract (load, play, pause, stop, seek, volume, position/duration events)
- **`ISoundCloudPlaybackService`** — extends the above with `LoadAndPlayAsync(trackId)` for fetching stream URLs and playing them

Both are backed by NAudio's `MediaFoundationReader` (supports local file paths and HTTP URLs) and `WaveOutEvent`. A shared **`PlaybackViewModel`** sits on top and manages all UI state — elapsed/total time, seek dragging, volume persistence, and play/pause toggling. Each track info panel sets a `LoadRequestedAsync` callback on this ViewModel for lazy-loading, keeping the playback logic decoupled from the source.

### Services

| Service | Responsibility |
|---|---|
| `IMusicLibraryService` | Scan folders for audio files, read/write ID3 metadata via TagLibSharp |
| `IImageService` | Album art display, clipboard copy/paste, file replacement |
| `ISoundCloudAuthService` | OAuth 2.0 flow with local HTTP callback server, token storage and refresh |
| `ISoundCloudService` | SoundCloud API client (search, track details, stream URLs) |
| `IOverlayService` | Loading indicators and confirmation dialogs |
| `IPlaybackService` | Local file audio playback |
| `ISoundCloudPlaybackService` | SoundCloud streaming playback |

### UI Structure

The shell hosts a `MenuBar` for navigation and an overlay system for loading/confirmation states. The main content is a resizable split-panel layout — track list on the left, detail/editor panel on the right. Local and SoundCloud views each have their own split-panel page, sharing the same playback controls component (`PlaybackView`).

## License

This project is licensed under the [MIT License](LICENSE) with the [Commons Clause](https://commonsclause.com/) condition. You are free to use, fork, and contribute, but you may not sell the software or forks of it without permission. See the [LICENSE](LICENSE) file for details.
