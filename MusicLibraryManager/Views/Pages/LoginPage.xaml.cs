using Microsoft.Extensions.DependencyInjection;
using MusicLibraryManager.ViewModels.Pages;
using Windows.UI.Xaml.Controls;

namespace MusicLibraryManager.Views.Pages
{
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            this.InitializeComponent();
            this.DataContext = App.Services.GetRequiredService<LoginPageViewModel>();
        }

        public LoginPageViewModel ViewModel => (LoginPageViewModel)this.DataContext;
    }
}
