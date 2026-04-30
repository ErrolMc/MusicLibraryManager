using MusicLibraryManager.ViewModels.SoundCloud;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace MusicLibraryManager.Presentation.SoundCloud;

public sealed partial class SoundCloudTrackListPanel : UserControl
{
    private static readonly Brush DefaultBrush = (Brush)Application.Current.Resources["SurfaceBrush"];
    private static readonly Brush HoverBrush = (Brush)Application.Current.Resources["OutlineVariantBrush"];
    private static readonly Brush PressedBrush = (Brush)Application.Current.Resources["OutlineBrush"];
    private ScrollViewer? _listScrollViewer;
    private readonly TranslateTransform _dragGhostTransform = new();

    public event EventHandler<double>? VerticalOffsetChanged;
    public double ScrollableHeight => _listScrollViewer?.ScrollableHeight ?? 0;

    public static readonly DependencyProperty HeaderTitleProperty =
        DependencyProperty.Register(
            nameof(HeaderTitle),
            typeof(string),
            typeof(SoundCloudTrackListPanel),
            new PropertyMetadata("Track List"));

    public static readonly DependencyProperty FooterTitleProperty =
        DependencyProperty.Register(
            nameof(FooterTitle),
            typeof(string),
            typeof(SoundCloudTrackListPanel),
            new PropertyMetadata("No playlist selected"));

    public string HeaderTitle
    {
        get => (string)GetValue(HeaderTitleProperty);
        set => SetValue(HeaderTitleProperty, value);
    }

    public string FooterTitle
    {
        get => (string)GetValue(FooterTitleProperty);
        set => SetValue(FooterTitleProperty, value);
    }

    public SoundCloudTrackListPanel()
    {
        this.InitializeComponent();
        this.Loaded += SoundCloudTrackListPanel_Loaded;
        this.PointerMoved += SoundCloudTrackListPanel_PointerMoved;
        this.PointerReleased += SoundCloudTrackListPanel_PointerReleased;
        DragGhost.RenderTransform = _dragGhostTransform;
    }

    private void SoundCloudTrackListPanel_Loaded(object sender, RoutedEventArgs e)
    {
        _listScrollViewer = FindDescendantScrollViewer(TracksListView);
        if (_listScrollViewer is not null)
        {
            _listScrollViewer.ViewChanged += ListScrollViewer_ViewChanged;
        }
    }

    private SyncSoundCloudTrackListViewModel? ViewModel => DataContext as SyncSoundCloudTrackListViewModel;

    private void InsertPaletteHandle_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Border handle || ViewModel is null)
        {
            return;
        }

        // Capture on the panel so PointerMoved keeps firing as cursor leaves the handle
        this.CapturePointer(e.Pointer);
        DragGhost.Visibility = Visibility.Visible;
        ViewModel.BeginInsertPaletteDrag();
        UpdateDragGhostAndTarget(e);
    }

    private void InsertPaletteHandle_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (ViewModel?.IsInsertPaletteDragging != true)
        {
            return;
        }

        UpdateDragGhostAndTarget(e);
    }

    private void InsertPaletteHandle_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        CompleteInsertDrag();
    }

    private void InsertPaletteHandle_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        CompleteInsertDrag();
    }

    private void ManualPlaceholder_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not SoundCloudSearchItemViewModel item || ViewModel is null)
        {
            return;
        }

        if (!e.GetCurrentPoint(border).Properties.IsLeftButtonPressed)
        {
            return;
        }

        border.CapturePointer(e.Pointer);
        DragGhost.Visibility = Visibility.Visible;
        ViewModel.BeginExistingPlaceholderDrag(item);
        UpdateDragGhostAndTarget(e);
    }

    private void ManualPlaceholder_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (ViewModel?.IsInsertPaletteDragging != true)
        {
            return;
        }

        UpdateDragGhostAndTarget(e);
    }

    private void ManualPlaceholder_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.ReleasePointerCapture(e.Pointer);
        }

        CompleteInsertDrag();
    }

    private void ManualPlaceholder_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        if (ViewModel?.IsInsertPaletteDragging != true)
        {
            CompleteInsertDrag();
        }
    }

    private void ManualPlaceholder_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not SoundCloudSearchItemViewModel item || ViewModel is null)
        {
            return;
        }

        var menuFlyout = new MenuFlyout();
        var removeItem = new MenuFlyoutItem { Text = "Remove placeholder" };
        removeItem.Click += (_, _) => ViewModel.RemovePlaceholder(item);
        menuFlyout.Items.Add(removeItem);
        menuFlyout.ShowAt(border, e.GetPosition(border));
        e.Handled = true;
    }

    private void SoundCloudTrackListPanel_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (ViewModel is null || (!ViewModel.IsInsertPaletteDragging && !ViewModel.IsNewInsertDragging))
        {
            return;
        }

        UpdateDragGhostAndTarget(e);
    }

    private void SoundCloudTrackListPanel_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (ViewModel is null || (!ViewModel.IsInsertPaletteDragging && !ViewModel.IsNewInsertDragging))
        {
            return;
        }

        CompleteInsertDrag();
    }

    private void SearchItem_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = HoverBrush;
        }
    }

    private void TracksListView_DragOver(object sender, DragEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        var point = e.GetPosition(TracksListView);
        ViewModel.SetDropTargetIndex(CalculateDropTargetIndex(point));
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
    }

    private void TracksListView_Drop(object sender, DragEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        var point = e.GetPosition(TracksListView);
        ViewModel.SetDropTargetIndex(CalculateDropTargetIndex(point));
        CompleteInsertDrag();
    }

    private void SearchItem_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = DefaultBrush;
        }
    }

    private void SearchItem_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = PressedBrush;
            border.CapturePointer(e.Pointer);
        }
    }

    private void SearchItem_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = HoverBrush;
            border.ReleasePointerCapture(e.Pointer);

            if (border.DataContext is SoundCloudSearchItemViewModel item)
            {
                ViewModel?.OnSelectItem(item);
            }
        }
    }

    private void SearchItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not SoundCloudSearchItemViewModel item)
        {
            return;
        }

        var menuFlyout = new MenuFlyout();
        var removeItem = new MenuFlyoutItem
        {
            Text = "Remove from playlist"
        };

        removeItem.Click += async (_, _) =>
        {
            if (ViewModel is not null)
            {
                await ViewModel.RemoveTrackFromPlaylistAsync(item);
            }
        };

        menuFlyout.Items.Add(removeItem);
        menuFlyout.ShowAt(border, e.GetPosition(border));
        e.Handled = true;
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

    private void UpdateDragGhostAndTarget(PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(this).Position;
        _dragGhostTransform.X = point.X - (DragGhost.Width / 2);
        _dragGhostTransform.Y = point.Y - (DragGhost.Height / 2);

        if (ViewModel is null)
        {
            return;
        }

        ViewModel.SetDropTargetIndex(CalculateDropTargetIndex(point));
    }

    // panelPoint is in SoundCloudTrackListPanel coordinate space
    private int CalculateDropTargetIndex(Point panelPoint)
    {
        if (ViewModel is null || ViewModel.SearchResults.Count == 0)
        {
            return 0;
        }

        var realized = new List<(int Index, double Top, double Height)>();

        for (var i = 0; i < ViewModel.SearchResults.Count; i++)
        {
            if (TracksListView.ContainerFromIndex(i) is not ListViewItem container)
            {
                continue;
            }

            // Transform each container into panel space for a consistent coordinate origin
            var origin = container.TransformToVisual(this).TransformPoint(new Point(0, 0));
            realized.Add((i, origin.Y, container.ActualHeight));
        }

        if (realized.Count == 0)
        {
            return ViewModel.SearchResults.Count;
        }

        realized.Sort((a, b) => a.Top.CompareTo(b.Top));

        if (panelPoint.Y <= realized[0].Top)
        {
            return realized[0].Index;
        }

        foreach (var item in realized)
        {
            var midpoint = item.Top + (item.Height / 2);
            if (panelPoint.Y < midpoint)
            {
                return item.Index;
            }
        }

        var lastItem = realized[^1];
        if (panelPoint.Y >= lastItem.Top + lastItem.Height)
        {
            return ViewModel.SearchResults.Count;
        }

        return lastItem.Index + 1;
    }

    private void CompleteInsertDrag()
    {
        if (ViewModel is null)
        {
            DragGhost.Visibility = Visibility.Collapsed;
            return;
        }

        if (ViewModel.IsNewInsertDragging)
        {
            ViewModel.CommitNewInsert();
        }

        if (ViewModel.IsInsertPaletteDragging || ViewModel.IsNewInsertDragging)
        {
            ViewModel.EndInsertPaletteDrag();
        }

        this.ReleasePointerCaptures();
        DragGhost.Visibility = Visibility.Collapsed;
    }

}
