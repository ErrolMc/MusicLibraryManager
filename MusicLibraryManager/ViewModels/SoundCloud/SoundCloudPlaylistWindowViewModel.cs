using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using MusicLibraryManager.Models;
using MusicLibraryManager.Services;

namespace MusicLibraryManager.ViewModels.SoundCloud;

public partial class SoundCloudPlaylistWindowViewModel : ObservableObject
{
    private readonly ISoundCloudAuthService _soundCloudAuthService;
    private readonly ISoundCloudService _soundCloudService;
    private readonly SyncPlaylistPanelViewModel _syncPlaylistPanelViewModel;
    private readonly ILogger<SoundCloudPlaylistWindowViewModel> _logger;

    [ObservableProperty]
    private bool isLoading = true;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string? errorMessage;

    public ObservableCollection<SoundCloudPlaylist> Playlists { get; } = [];

    public event EventHandler? CloseRequested;

    public SoundCloudPlaylistWindowViewModel(
        ISoundCloudAuthService soundCloudAuthService,
        ISoundCloudService soundCloudService,
        SyncPlaylistPanelViewModel syncPlaylistPanelViewModel,
        ILogger<SoundCloudPlaylistWindowViewModel> logger)
    {
        _soundCloudAuthService = soundCloudAuthService;
        _soundCloudService = soundCloudService;
        _syncPlaylistPanelViewModel = syncPlaylistPanelViewModel;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = null;

        try
        {
            var isAuthenticated = _soundCloudAuthService.IsAuthenticated || await _soundCloudAuthService.TryLoadStoredTokensAsync();
            if (!isAuthenticated)
            {
                var authenticated = await _soundCloudAuthService.StartOAuthFlowAsync();
                if (!authenticated)
                {
                    HasError = true;
                    ErrorMessage = "Could not authenticate with SoundCloud.";
                    return;
                }
            }

            await LoadPlaylistsAsync();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = null;

        try
        {
            await LoadPlaylistsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SelectPlaylistAsync(SoundCloudPlaylist? playlist)
    {
        if (playlist is null)
        {
            return;
        }

        _logger.LogInformation("[PlaylistWindow] SelectPlaylistAsync started. PlaylistId={PlaylistId}, Title={Title}", playlist.Id, playlist.Title);

        _syncPlaylistPanelViewModel.SelectedPlaylistTitle = playlist.Title;
        _syncPlaylistPanelViewModel.SelectedPlaylistId = playlist.Id;
        _syncPlaylistPanelViewModel.SyncSoundCloudTrackListViewModel.SelectedPlaylistTitle = playlist.Title;
        _syncPlaylistPanelViewModel.SyncSoundCloudTrackListViewModel.SelectedPlaylistId = playlist.Id;

        IsLoading = true;
        HasError = false;
        ErrorMessage = null;

        try
        {
            var tracks = await _soundCloudService.GetPlaylistTracksAsync(playlist.Id);
            _logger.LogInformation("[PlaylistWindow] GetPlaylistTracksAsync returned {TrackCount} tracks.", tracks.Count);
            await _syncPlaylistPanelViewModel.SyncSoundCloudTrackListViewModel.LoadPlaylistTracksAsync(tracks);
            _logger.LogInformation("[PlaylistWindow] LoadPlaylistTracksAsync completed. SearchResults.Count={Count}", _syncPlaylistPanelViewModel.SyncSoundCloudTrackListViewModel.SearchResults.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PlaylistWindow] Exception in SelectPlaylistAsync.");
            HasError = true;
            ErrorMessage = ex.Message;
            return;
        }
        finally
        {
            IsLoading = false;
        }
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task LoadPlaylistsAsync()
    {
        var playlists = await _soundCloudService.GetMyPlaylistsAsync();
        Playlists.Clear();

        foreach (var playlist in playlists)
        {
            Playlists.Add(playlist);
        }
    }
}
