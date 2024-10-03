using System;
using Windows.UI.Xaml.Controls;

namespace MusicLibraryManager.Source.Services.Concrete
{
    public class NavigationService : INavigationService
    {
        private readonly Frame _frame;

        public Type CurrentPage { get; private set; }

        public NavigationService(Frame frame)
        {
            _frame = frame;
            CurrentPage = null;
        }

        public void Navigate<TPage>() where TPage : Page
        {
            CurrentPage = typeof(TPage);
            _frame.Navigate(CurrentPage);
        }
    }
}
