using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using MusicLibraryManager.ViewModels;

namespace MusicLibraryManager.Presentation;

public sealed partial class TrackListPanel : UserControl
{
    private InputCursor? _originalCursor;
    private bool _isDragging;
    private bool _isHovering;

    private const double MinTitleWidth = 80;
    private const double MinArtistWidth = 80;
    private const double MinYearWidth = 50;
    private const double SplitterWidth = 16;
    private const double Padding = 56; // Border padding + margins

    public TrackListPanel()
    {
        this.InitializeComponent();
    }

    private TrackListPanelViewModel? ViewModel => DataContext as TrackListPanelViewModel;

    private double AvailableWidth => ActualWidth - Padding;

    private void ArtistSplitter_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        if (ViewModel == null) return;

        var newWidth = ViewModel.ArtistColumnWidth.Value - e.Delta.Translation.X;
        
        // Calculate max width: available space minus title min, year column, and splitters
        var maxWidth = AvailableWidth - MinTitleWidth - ViewModel.YearColumnWidth.Value - (SplitterWidth * 2);
        newWidth = Math.Clamp(newWidth, MinArtistWidth, Math.Max(MinArtistWidth, maxWidth));
        
        ViewModel.ArtistColumnWidth = new GridLength(newWidth);
    }

    private void YearSplitter_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        if (ViewModel == null) return;

        var newWidth = ViewModel.YearColumnWidth.Value - e.Delta.Translation.X;
        
        // Calculate max width: available space minus title min, artist column, and splitters
        var maxWidth = AvailableWidth - MinTitleWidth - ViewModel.ArtistColumnWidth.Value - (SplitterWidth * 2);
        newWidth = Math.Clamp(newWidth, MinYearWidth, Math.Max(MinYearWidth, maxWidth));
        
        ViewModel.YearColumnWidth = new GridLength(newWidth);
    }

    private void Splitter_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
    {
        _isDragging = true;
    }

    private void Splitter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
    {
        _isDragging = false;
        
        // Only reset cursor if we're no longer hovering over a splitter
        if (!_isHovering)
        {
            ProtectedCursor = _originalCursor;
        }
    }

    private void Splitter_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isHovering = true;
        
        if (!_isDragging)
        {
            _originalCursor = ProtectedCursor;
        }
        
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }

    private void Splitter_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isHovering = false;
        
        if (!_isDragging)
        {
            ProtectedCursor = _originalCursor;
        }
    }
}
