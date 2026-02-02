using Microsoft.UI.Xaml.Input;

namespace MusicLibraryManager.Presentation;

public sealed partial class TrackListItem : UserControl
{
    private static readonly Brush DefaultBrush = (Brush)Application.Current.Resources["SurfaceBrush"];
    private static readonly Brush HoverBrush = (Brush)Application.Current.Resources["OutlineVariantBrush"];
    private static readonly Brush PressedBrush = (Brush)Application.Current.Resources["OutlineBrush"];

    private bool _isPressed;
    private bool _isHovered;

    public TrackListItem()
    {
        this.InitializeComponent();
    }

    private void ItemBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isHovered = true;
        UpdateVisualState();
    }

    private void ItemBorder_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isHovered = false;
        _isPressed = false;
        UpdateVisualState();
    }

    private void ItemBorder_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isPressed = true;
        UpdateVisualState();
        ItemBorder.CapturePointer(e.Pointer);
    }

    private void ItemBorder_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isPressed && _isHovered)
        {
            // Execute click command when released while still hovering
            if (DataContext is TrackListItemViewModel viewModel)
            {
                viewModel.ClickCommand.Execute(null);
            }
        }

        _isPressed = false;
        UpdateVisualState();
        ItemBorder.ReleasePointerCapture(e.Pointer);
    }

    private void UpdateVisualState()
    {
        if (_isPressed)
        {
            // Darker when pressed
            ItemBorder.Background = PressedBrush;
        }
        else if (_isHovered)
        {
            // Lighter when hovered
            ItemBorder.Background = HoverBrush;
        }
        else
        {
            // Default state
            ItemBorder.Background = DefaultBrush;
        }
    }
}
