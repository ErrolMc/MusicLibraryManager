using MusicLibraryManager.Services;

namespace MusicLibraryManager.ViewModels.SoundCloud;

public partial class SoundCloudWindowViewModel : ObservableObject
{
    private readonly ISoundCloudAuthService _authService;

    public SoundCloudSignInViewModel SignInViewModel { get; }
    public SoundCloudSplitPanelViewModel SplitPanelViewModel { get; }

    public event EventHandler? CloseRequested;

    [ObservableProperty]
    private bool isAuthenticated;

    [ObservableProperty]
    private bool isLoading = true;

    public SoundCloudWindowViewModel(
        ISoundCloudAuthService authService,
        SoundCloudSignInViewModel signInViewModel,
        SoundCloudSplitPanelViewModel splitPanelViewModel)
    {
        _authService = authService;
        SignInViewModel = signInViewModel;
        SplitPanelViewModel = splitPanelViewModel;

        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
        SignInViewModel.SignInCompleted += OnSignInCompleted;
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;

        // Try to load stored tokens
        var hasStoredTokens = await _authService.TryLoadStoredTokensAsync();
        IsAuthenticated = hasStoredTokens;

        IsLoading = false;
    }

    public void OnClosed()
    {
        // Clear search results and track info to free memory
        SplitPanelViewModel.SoundCloudSearchListViewModel.Clear();
    }

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        IsAuthenticated = isAuthenticated;

        if (!isAuthenticated)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnSignInCompleted(object? sender, EventArgs e)
    {
        IsAuthenticated = true;
    }
}
