using Microsoft.UI.Xaml.Data;

namespace MusicLibraryManager.Converters;

/// <summary>
/// Converts a boolean value to Visibility. True = Collapsed, False = Visible.
/// </summary>
public class TrueToCollapsedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Collapsed;
        }
        return false;
    }
}
