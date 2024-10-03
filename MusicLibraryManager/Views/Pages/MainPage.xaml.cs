using Microsoft.Extensions.DependencyInjection;
using MusicLibraryManager.ViewModels.Pages;
using Windows.UI.Xaml.Controls;

namespace MusicLibraryManager.Views.Pages
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = App.Services.GetRequiredService<MainPageViewModel>();
        }

        public MainPageViewModel ViewModel => (MainPageViewModel)this.DataContext;
    }
}
