using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using MusicLibraryManager.Presentation.SoundCloud;
using MusicLibraryManager.ViewModels;

namespace MusicLibraryManager.Presentation;

public sealed partial class SyncPlaylistPanelPage : Page
{
    private bool _isDraggingLocal;
    private bool _isDraggingSoundCloud;
    private double _dragStartX;
    private double _initialLeftWidth;
    private double _initialRightWidth;
    private bool _isSyncingScroll;
    private bool _isCenterScrollDriving;
    private DateTime _lastLocalScrollSyncUtc = DateTime.MinValue;
    private DateTime _lastSoundCloudScrollSyncUtc = DateTime.MinValue;

    public SyncPlaylistPanelPage()
    {
        this.InitializeComponent();
        this.Loaded += SyncPlaylistPanelPage_Loaded;
    }

    private void SyncPlaylistPanelPage_Loaded(object sender, RoutedEventArgs e)
    {
        LocalTrackListPanel.VerticalOffsetChanged += LocalTrackListPanel_VerticalOffsetChanged;
        SoundCloudTrackListPanel.VerticalOffsetChanged += SoundCloudTrackListPanel_VerticalOffsetChanged;
        DispatcherQueue.TryEnqueue(UpdateLinkedScrollBarRange);
    }

    private async void SelectSoundCloudPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        await Task.CompletedTask;

        var playlistWindow = new SoundCloudPlaylistWindow();
        playlistWindow.Activate();
    }

    private void LocalTrackListPanel_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SyncPlaylistPanelViewModel vm)
        {
            vm.TrackListPanelViewModel.Title = SyncPlaylistPanelViewModel.LocalTracksTitle;
        }

        UpdateLinkedScrollBarRange();
    }

    private void UpdateLinkedScrollBarRange()
    {
        var maxScrollable = Math.Max(LocalTrackListPanel.ScrollableHeight, SoundCloudTrackListPanel.ScrollableHeight);
        LinkedScrollBar.Minimum = 0;
        LinkedScrollBar.Maximum = Math.Max(0, maxScrollable);
        LinkedScrollBar.ViewportSize = 1;
        LinkedScrollBar.SmallChange = 16;
        LinkedScrollBar.LargeChange = 160;
    }

    private void LocalTrackListPanel_VerticalOffsetChanged(object? sender, double offset)
    {
        if (_isSyncingScroll)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if ((now - _lastSoundCloudScrollSyncUtc).TotalMilliseconds < 80)
        {
            return;
        }

        _isSyncingScroll = true;
        try
        {
            SoundCloudTrackListPanel.SetVerticalOffset(offset);
            LinkedScrollBar.Value = Math.Min(LinkedScrollBar.Maximum, Math.Max(LinkedScrollBar.Minimum, offset));
            _lastLocalScrollSyncUtc = now;
        }
        finally
        {
            _isSyncingScroll = false;
        }
    }

    private void SoundCloudTrackListPanel_VerticalOffsetChanged(object? sender, double offset)
    {
        if (_isSyncingScroll)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if ((now - _lastLocalScrollSyncUtc).TotalMilliseconds < 80)
        {
            return;
        }

        _isSyncingScroll = true;
        try
        {
            LocalTrackListPanel.SetVerticalOffset(offset);
            LinkedScrollBar.Value = Math.Min(LinkedScrollBar.Maximum, Math.Max(LinkedScrollBar.Minimum, offset));
            _lastSoundCloudScrollSyncUtc = now;
        }
        finally
        {
            _isSyncingScroll = false;
        }
    }

    private void LinkedScrollBar_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isSyncingScroll)
        {
            return;
        }

        _isCenterScrollDriving = true;
        _isSyncingScroll = true;
        try
        {
            LocalTrackListPanel.SetVerticalOffset(e.NewValue);
            SoundCloudTrackListPanel.SetVerticalOffset(e.NewValue);
            _lastLocalScrollSyncUtc = DateTime.UtcNow;
            _lastSoundCloudScrollSyncUtc = DateTime.UtcNow;
        }
        finally
        {
            _isSyncingScroll = false;
            _isCenterScrollDriving = false;
        }
    }

    private void LocalSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }

    private void LocalSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDraggingLocal && !_isDraggingSoundCloud)
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }
    }

    private void LocalSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isDraggingLocal = true;
        _dragStartX = e.GetCurrentPoint(LeftSyncGrid).Position.X;
        _initialLeftWidth = LocalInfoColumn.ActualWidth;
        _initialRightWidth = LocalListColumn.ActualWidth;

        ((UIElement)sender).CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void LocalSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDraggingLocal)
            return;

        var currentX = e.GetCurrentPoint(LeftSyncGrid).Position.X;
        var delta = currentX - _dragStartX;

        var newLeftWidth = _initialLeftWidth + delta;
        var newRightWidth = _initialRightWidth - delta;

        var minWidth = 100.0;
        if (newLeftWidth >= minWidth && newRightWidth >= minWidth)
        {
            LocalInfoColumn.Width = new GridLength(newLeftWidth, GridUnitType.Star);
            LocalListColumn.Width = new GridLength(newRightWidth, GridUnitType.Star);
        }

        e.Handled = true;
    }

    private void LocalSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        StopLocalDragging(sender, e, keepResizeCursor: true);
    }

    private void LocalSplitter_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        StopLocalDragging(sender, e, keepResizeCursor: false);
    }

    private void StopLocalDragging(object sender, PointerRoutedEventArgs e, bool keepResizeCursor)
    {
        if (_isDraggingLocal)
        {
            _isDraggingLocal = false;
            ((UIElement)sender).ReleasePointerCapture(e.Pointer);

            if (!keepResizeCursor)
            {
                ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
            }
        }
    }

    private void SoundCloudSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }

    private void SoundCloudSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDraggingLocal && !_isDraggingSoundCloud)
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }
    }

    private void SoundCloudSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isDraggingSoundCloud = true;
        _dragStartX = e.GetCurrentPoint(RightSyncGrid).Position.X;
        _initialLeftWidth = SoundCloudListColumn.ActualWidth;
        _initialRightWidth = SoundCloudInfoColumn.ActualWidth;

        ((UIElement)sender).CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void SoundCloudSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDraggingSoundCloud)
            return;

        var currentX = e.GetCurrentPoint(RightSyncGrid).Position.X;
        var delta = currentX - _dragStartX;

        var newLeftWidth = _initialLeftWidth + delta;
        var newRightWidth = _initialRightWidth - delta;

        var minWidth = 100.0;
        if (newLeftWidth >= minWidth && newRightWidth >= minWidth)
        {
            SoundCloudListColumn.Width = new GridLength(newLeftWidth, GridUnitType.Star);
            SoundCloudInfoColumn.Width = new GridLength(newRightWidth, GridUnitType.Star);
        }

        e.Handled = true;
    }

    private void SoundCloudSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        StopSoundCloudDragging(sender, e, keepResizeCursor: true);
    }

    private void SoundCloudSplitter_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        StopSoundCloudDragging(sender, e, keepResizeCursor: false);
    }

    private void StopSoundCloudDragging(object sender, PointerRoutedEventArgs e, bool keepResizeCursor)
    {
        if (_isDraggingSoundCloud)
        {
            _isDraggingSoundCloud = false;
            ((UIElement)sender).ReleasePointerCapture(e.Pointer);

            if (!keepResizeCursor)
            {
                ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
            }
        }
    }

}
