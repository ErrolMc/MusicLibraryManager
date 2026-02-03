namespace MusicLibraryManager.ViewModels;

public partial class TrackListItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string fileNameWithoutExtension = string.Empty;

    private string _fileExtension = string.Empty;
    public string FileExtension => _fileExtension;

    public string FullFileName => FileNameWithoutExtension + FileExtension;

    [ObservableProperty]
    private string songName = string.Empty;

    [ObservableProperty]
    private string artist = string.Empty;

    [ObservableProperty]
    private string albumTitle = string.Empty;

    [ObservableProperty]
    private int year;

    private readonly Action<TrackListItemViewModel>? _onClickAction;
    private readonly Track? _track;

    public Track? Track => _track;
    public TrackListPanelViewModel Parent { get; }

    public ICommand ClickCommand { get; }

    public TrackListItemViewModel(Track track, Action<TrackListItemViewModel> onClickAction, TrackListPanelViewModel parent)
    {
        var fullFileName = Path.GetFileName(track.FilePath);
        FileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullFileName);
        _fileExtension = Path.GetExtension(fullFileName);

        _track = track;
        _onClickAction = onClickAction;
        Parent = parent;

        PopulateInfo();

        ClickCommand = new RelayCommand(OnClick);
    }

    public void PopulateInfo()
    {
        if (_track == null) return;

        SongName = _track.Title;
        Artist = _track.Artist;
        AlbumTitle = _track.Album;
        Year = _track.Year ?? 0;
    }

    private void OnClick()
    {
        _onClickAction?.Invoke(this);
    }
}
