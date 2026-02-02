using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;

namespace MusicLibraryManager.Presentation;

public sealed partial class SplitPanelPage : Page
{
    private bool _isDragging;
    private double _initialLeftWidth;
    private double _initialRightWidth;
    private double _dragStartX;

    public SplitPanelPage()
    {
        this.InitializeComponent();
    }

    private void Splitter_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }

    private void Splitter_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging)
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }
    }

    private void Splitter_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isDragging = true;
        _dragStartX = e.GetCurrentPoint(SplitContainer).Position.X;
        _initialLeftWidth = LeftColumn.ActualWidth;
        _initialRightWidth = RightColumn.ActualWidth;

        ((UIElement)sender).CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void Splitter_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging)
            return;

        var currentX = e.GetCurrentPoint(SplitContainer).Position.X;
        var delta = currentX - _dragStartX;

        var newLeftWidth = _initialLeftWidth + delta;
        var newRightWidth = _initialRightWidth - delta;

        // Respect minimum widths
        var minWidth = 100.0;
        if (newLeftWidth >= minWidth && newRightWidth >= minWidth)
        {
            LeftColumn.Width = new GridLength(newLeftWidth, GridUnitType.Star);
            RightColumn.Width = new GridLength(newRightWidth, GridUnitType.Star);
        }

        e.Handled = true;
    }

    private void Splitter_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        StopDragging(sender, e, keepResizeCursor: true);
    }

    private void Splitter_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        StopDragging(sender, e, keepResizeCursor: false);
    }

    private void StopDragging(object sender, PointerRoutedEventArgs e, bool keepResizeCursor)
    {
        if (_isDragging)
        {
            _isDragging = false;
            ((UIElement)sender).ReleasePointerCapture(e.Pointer);
            
            if (!keepResizeCursor)
            {
                ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
            }
        }
    }
}
