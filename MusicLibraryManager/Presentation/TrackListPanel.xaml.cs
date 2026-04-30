using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using MusicLibraryManager.ViewModels;

namespace MusicLibraryManager.Presentation;

public sealed partial class TrackListPanel : UserControl
{
    private InputCursor? _originalCursor;
    private bool _isDragging;
    private bool _isHovering;

    private const double MinFileNameWidth = 80;
    private const double MinTitleWidth = 80;
    private const double MinArtistWidth = 80;
    private const double MinYearWidth = 50;
    private const double SplitterWidth = 16;
    private const double Padding = 56; // Border padding + margins
    private ScrollViewer? _listScrollViewer;

    public event EventHandler<double>? VerticalOffsetChanged;
    public double ScrollableHeight => _listScrollViewer?.ScrollableHeight ?? 0;

    public TrackListPanel()
    {
        this.InitializeComponent();
        this.Loaded += TrackListPanel_Loaded;
    }

    private void TrackListPanel_Loaded(object sender, RoutedEventArgs e)
    {
        _listScrollViewer = FindDescendantScrollViewer(TracksListView);
        if (_listScrollViewer is not null)
        {
            _listScrollViewer.ViewChanged += ListScrollViewer_ViewChanged;
        }
    }

    private void ListScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_listScrollViewer is not null)
        {
            VerticalOffsetChanged?.Invoke(this, _listScrollViewer.VerticalOffset);
        }
    }

    public void SetVerticalOffset(double offset)
    {
        _listScrollViewer?.ChangeView(null, offset, null, disableAnimation: true);
    }

    private static ScrollViewer? FindDescendantScrollViewer(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is ScrollViewer scrollViewer)
            {
                return scrollViewer;
            }

            var nested = FindDescendantScrollViewer(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private TrackListPanelViewModel? ViewModel => DataContext as TrackListPanelViewModel;

    private double AvailableWidth => ActualWidth - Padding;

    private void FileNameSplitter_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        if (ViewModel == null) return;

        var newWidth = ViewModel.FileNameColumnWidth.Value + e.Delta.Translation.X;

        // Calculate max width: available space minus title min, artist, year columns, and splitters
        var maxWidth = AvailableWidth - MinTitleWidth - ViewModel.ArtistColumnWidth.Value - ViewModel.YearColumnWidth.Value - (SplitterWidth * 3);
        newWidth = Math.Clamp(newWidth, MinFileNameWidth, Math.Max(MinFileNameWidth, maxWidth));

        ViewModel.FileNameColumnWidth = new GridLength(newWidth);
    }

    private void ArtistSplitter_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        if (ViewModel == null) return;

        var newWidth = ViewModel.ArtistColumnWidth.Value - e.Delta.Translation.X;
        
        // Calculate max width: available space minus filename, title min, year column, and splitters
        var maxWidth = AvailableWidth - ViewModel.FileNameColumnWidth.Value - MinTitleWidth - ViewModel.YearColumnWidth.Value - (SplitterWidth * 3);
        newWidth = Math.Clamp(newWidth, MinArtistWidth, Math.Max(MinArtistWidth, maxWidth));
        
        ViewModel.ArtistColumnWidth = new GridLength(newWidth);
    }

    private void YearSplitter_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        if (ViewModel == null) return;

        var newWidth = ViewModel.YearColumnWidth.Value - e.Delta.Translation.X;
        
        // Calculate max width: available space minus filename, title min, artist column, and splitters
        var maxWidth = AvailableWidth - ViewModel.FileNameColumnWidth.Value - MinTitleWidth - ViewModel.ArtistColumnWidth.Value - (SplitterWidth * 3);
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
