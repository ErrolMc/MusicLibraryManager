using MusicLibraryManager.Source.Services;
using MusicLibraryManager.Views.Pages;
using ReactiveUI;
using System.Windows.Input;

namespace MusicLibraryManager.ViewModels.Pages
{
    public class LoginPageViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;

        public ICommand GotoMainCommand { get; private set; }

        public LoginPageViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;

            GotoMainCommand = ReactiveCommand.Create(GotoMain);
        }

        private void GotoMain()
        {
            _navigationService.Navigate<MainPage>();
        }
    }
}
