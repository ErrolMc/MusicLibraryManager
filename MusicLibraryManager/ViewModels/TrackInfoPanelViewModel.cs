using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Media.Imaging;
using MusicLibraryManager.Services;
using Windows.Storage.Pickers;

namespace MusicLibraryManager.ViewModels;

public partial class TrackInfoPanelViewModel : ObservableObject
{
    private readonly IMusicLibraryService _musicLibraryService;
    private readonly IImageService _imageService;
    private Track? _currentTrack;
    private Action? _refreshTrackItemCallback;

    // Original values for change detection
    private string? _originalFileNameWithoutExtension;
    private string? _originalTitle;
    private string? _originalArtist;
    private string? _originalAlbum;
    private string? _originalAlbumArtist;
    private string? _originalComposer;
    private string? _originalGenre;
    private string? _originalComment;
    private string? _originalYear;
    private string? _originalTrack;

    // Album cover change tracking
    private byte[]? _newAlbumCoverData;
    private string? _newAlbumCoverMimeType;
    private bool _albumCoverChanged;

    [ObservableProperty]
    private string? fileNameWithoutExtension;

    private string? _fileExtension;
    public string? FileExtension => _fileExtension;

    public string FullFileName => (FileNameWithoutExtension ?? "") + (FileExtension ?? "");

    [ObservableProperty]
    private string? title;

    [ObservableProperty]
    private string? artist;

    [ObservableProperty]
    private string? album;

    [ObservableProperty]
    private string? year;

    [ObservableProperty]
    private string? genre;

    [ObservableProperty]
    private string? comment;

    [ObservableProperty]
    private string? composer;

    [ObservableProperty]
    private string? albumArtist;

    [ObservableProperty]
    private string? track;

    [ObservableProperty]
    private BitmapImage? albumCover;

    [ObservableProperty]
    private bool hasChanges;

    [ObservableProperty]
    private bool hasTrackLoaded;

    public TrackInfoPanelViewModel(IMusicLibraryService musicLibraryService, IImageService imageService)
    {
        _musicLibraryService = musicLibraryService;
        _imageService = imageService;
    }

    partial void OnFileNameWithoutExtensionChanged(string? value) => UpdateHasChanges();
    partial void OnTitleChanged(string? value) => UpdateHasChanges();
    partial void OnArtistChanged(string? value) => UpdateHasChanges();
    partial void OnAlbumChanged(string? value) => UpdateHasChanges();
    partial void OnAlbumArtistChanged(string? value) => UpdateHasChanges();
    partial void OnComposerChanged(string? value) => UpdateHasChanges();
    partial void OnGenreChanged(string? value) => UpdateHasChanges();
    partial void OnCommentChanged(string? value) => UpdateHasChanges();
    partial void OnYearChanged(string? value) => UpdateHasChanges();
    partial void OnTrackChanged(string? value) => UpdateHasChanges();

    private void UpdateHasChanges()
    {
        if (_currentTrack == null)
        {
            HasChanges = false;
            return;
        }

        HasChanges = 
        _albumCoverChanged ||
        FileNameWithoutExtension != _originalFileNameWithoutExtension ||
        Title != _originalTitle ||
        Artist != _originalArtist ||
        Album != _originalAlbum ||
        AlbumArtist != _originalAlbumArtist ||
        Composer != _originalComposer ||
        Genre != _originalGenre ||
        Comment != _originalComment ||
        Year != _originalYear ||
        Track != _originalTrack;
    }

    public void SetInfoFromSong(Track track, System.Action refreshTrackItemCallback)
    {
        _currentTrack = track;
        _refreshTrackItemCallback = refreshTrackItemCallback;

        var fullFileName = Path.GetFileName(track.FilePath);
        _originalFileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullFileName);
        _fileExtension = Path.GetExtension(fullFileName);
        
        _originalTitle = track.Title;
        _originalArtist = track.Artist;
        _originalAlbum = track.Album;
        _originalYear = (track.Year ?? 0).ToString();
        _originalGenre = track.Genre;
        _originalComment = track.Tag.Comment ?? "";
        _originalComposer = track.Tag.FirstComposer ?? "";
        _originalAlbumArtist = track.Tag.FirstAlbumArtist ?? "";
        _originalTrack = (track.TrackNumber ?? 0).ToString();

        // Set current values
        FileNameWithoutExtension = _originalFileNameWithoutExtension;
        OnPropertyChanged(nameof(FileExtension));
        Title = _originalTitle;
        Artist = _originalArtist;
        Album = _originalAlbum;
        Year = _originalYear;
        Genre = _originalGenre;
        Comment = _originalComment;
        Composer = _originalComposer;
        AlbumArtist = _originalAlbumArtist;
        Track = _originalTrack;
        AlbumCover = GetAlbumCover(track);

        // Reset album cover change tracking
        _newAlbumCoverData = null;
        _newAlbumCoverMimeType = null;
        _albumCoverChanged = false;

        HasTrackLoaded = true;
        HasChanges = false;
    }

    public void CancelChanges()
    {
        if (_currentTrack == null) return;

        FileNameWithoutExtension = _originalFileNameWithoutExtension;
        Title = _originalTitle;
        Artist = _originalArtist;
        Album = _originalAlbum;
        Year = _originalYear;
        Genre = _originalGenre;
        Comment = _originalComment;
        Composer = _originalComposer;
        AlbumArtist = _originalAlbumArtist;
        Track = _originalTrack;

        // Reset album cover changes
        AlbumCover = GetAlbumCover(_currentTrack);
        _newAlbumCoverData = null;
        _newAlbumCoverMimeType = null;
        _albumCoverChanged = false;

        HasChanges = false;
    }

    public async Task<bool> SaveChangesAsync()
    {
        if (_currentTrack == null || !HasChanges) return true;

        uint.TryParse(Year, out var yearValue);
        uint.TryParse(Track, out var trackValue);

        var updateInfo = new TrackUpdateInfo(
            FileName: FileNameWithoutExtension != _originalFileNameWithoutExtension ? FileNameWithoutExtension : null,
            Title: Title != _originalTitle ? Title : null,
            Artist: Artist != _originalArtist ? Artist : null,
            Album: Album != _originalAlbum ? Album : null,
            AlbumArtist: AlbumArtist != _originalAlbumArtist ? AlbumArtist : null,
            Composer: Composer != _originalComposer ? Composer : null,
            Genre: Genre != _originalGenre ? Genre : null,
            Comment: Comment != _originalComment ? Comment : null,
            Year: Year != _originalYear ? yearValue : null,
            TrackNumber: Track != _originalTrack ? trackValue : null,
            AlbumCoverData: _albumCoverChanged ? _newAlbumCoverData : null,
            AlbumCoverMimeType: _albumCoverChanged ? _newAlbumCoverMimeType : null
        );

        var success = await _musicLibraryService.UpdateTrackAsync(_currentTrack, updateInfo);

        if (success)
        {
            // Update original values to current
            _originalFileNameWithoutExtension = FileNameWithoutExtension;
            _originalTitle = Title;
            _originalArtist = Artist;
            _originalAlbum = Album;
            _originalAlbumArtist = AlbumArtist;
            _originalComposer = Composer;
            _originalGenre = Genre;
            _originalComment = Comment;
            _originalYear = Year;
            _originalTrack = Track;

            // Reset album cover change tracking
            _newAlbumCoverData = null;
            _newAlbumCoverMimeType = null;
            _albumCoverChanged = false;

            HasChanges = false;
            _refreshTrackItemCallback?.Invoke();
        }

        return success;
    }

    private static BitmapImage? GetAlbumCover(Track track)
    {
        var pictures = track.Tag.Pictures;
        if (pictures == null || pictures.Length == 0)
            return null;

        var picture = pictures[0];
        var imageData = picture.Data.Data;

        var bitmap = new BitmapImage();
        using var stream = new MemoryStream(imageData);
        bitmap.SetSource(stream.AsRandomAccessStream());
        return bitmap;
    }

    [RelayCommand]
    private async Task ChangeAlbumCoverAsync()
    {
        if (_currentTrack == null) return;

        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".bmp");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Instance.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file == null) return;

        using var stream = await file.OpenStreamForReadAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();

        _newAlbumCoverData = bytes;
        _newAlbumCoverMimeType = file.ContentType;
        _albumCoverChanged = true;

        // Update the displayed album cover
        var bitmap = new BitmapImage();
        using var imageStream = new MemoryStream(bytes);
        bitmap.SetSource(imageStream.AsRandomAccessStream());
        AlbumCover = bitmap;

        UpdateHasChanges();
    }

    [RelayCommand]
    private void CopyAlbumCover()
    {
        if (_currentTrack == null) return;

        byte[]? imageData = _albumCoverChanged ? _newAlbumCoverData : _imageService.GetAlbumCoverData(_currentTrack);
        if (imageData == null) return;

        _imageService.CopyToClipboard(imageData);
    }

    [RelayCommand]
    private void RemoveAlbumCover()
    {
        if (_currentTrack == null) return;

        _newAlbumCoverData = null;
        _newAlbumCoverMimeType = null;
        _albumCoverChanged = true;
        AlbumCover = null;

        UpdateHasChanges();
    }

    [RelayCommand(CanExecute = nameof(CanPasteAlbumCover))]
    private void ReplaceAlbumCoverFromClipboard()
    {
        if (_currentTrack == null) return;

        var (imageData, mimeType) = _imageService.GetImageFromClipboard();
        if (imageData == null || mimeType == null) return;

        _newAlbumCoverData = imageData;
        _newAlbumCoverMimeType = mimeType;
        _albumCoverChanged = true;

        // Update the displayed album cover
        var bitmap = new BitmapImage();
        using var imageStream = new MemoryStream(imageData);
        bitmap.SetSource(imageStream.AsRandomAccessStream());
        AlbumCover = bitmap;

        UpdateHasChanges();
    }

    private bool CanPasteAlbumCover() => _currentTrack != null && _imageService.HasImageInClipboard();

    public void RefreshClipboardState() => ReplaceAlbumCoverFromClipboardCommand.NotifyCanExecuteChanged();
}
