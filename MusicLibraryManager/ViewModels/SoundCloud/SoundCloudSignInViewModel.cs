using MusicLibraryManager.Services;

namespace MusicLibraryManager.ViewModels.SoundCloud;

public partial class SoundCloudSignInViewModel : ObservableObject
{
    private readonly ISoundCloudAuthService _authService;

    private CancellationTokenSource? _signInCts;

    [ObservableProperty]
    private bool isSigningIn;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string? errorMessage;

    public event EventHandler? SignInCompleted;

    public SoundCloudSignInViewModel(ISoundCloudAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        HasError = false;
        ErrorMessage = null;
        IsSigningIn = true;

        _signInCts = new CancellationTokenSource();

        try
        {
            var success = await _authService.StartOAuthFlowAsync(_signInCts.Token);

            if (success)
            {
                SignInCompleted?.Invoke(this, EventArgs.Empty);
            }
            else if (!_signInCts.IsCancellationRequested)
            {
                HasError = true;
                ErrorMessage = "Authentication failed. Please try again.";
            }
        }
        catch (OperationCanceledException)
        {
            // User cancelled - no error message needed
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Failed to sign in: {ex.Message}";
        }
        finally
        {
            IsSigningIn = false;
            _signInCts?.Dispose();
            _signInCts = null;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _signInCts?.Cancel();
        IsSigningIn = false;
        HasError = false;
        ErrorMessage = null;
    }
}
