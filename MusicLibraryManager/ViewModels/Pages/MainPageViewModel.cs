using MusicLibraryManager.Source.Services;
using MusicLibraryManager.Views.Pages;
using ReactiveUI;
using System.Windows.Input;

namespace MusicLibraryManager.ViewModels.Pages
{
    public class MainPageViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;

        public ICommand GotoLoginCommand { get; private set; }

        public MainPageViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;

            GotoLoginCommand = ReactiveCommand.Create(GotoLogin);
        }

        private void GotoLogin()
        {
            _navigationService.Navigate<LoginPage>();
        }
    }
}
