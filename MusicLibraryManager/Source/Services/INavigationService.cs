using System;
using Windows.UI.Xaml.Controls;

namespace MusicLibraryManager.Source.Services
{
    public interface INavigationService
    {
        void Navigate<T>() where T : Page;
        Type CurrentPage { get; }
    }
}
