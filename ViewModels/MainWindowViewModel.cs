using CommunityToolkit.Mvvm.Input;

namespace KaraMovieMaker.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly SubtitleViewModel _subtitleViewModel = new();
        private readonly VideoViewModel _videoViewModel = new();

        public MainWindowViewModel()
        {
            CurrentPage = _subtitleViewModel;
        }

        public ViewModelBase CurrentPage { get; private set; }

        public bool IsSubtitleActive => ReferenceEquals(CurrentPage, _subtitleViewModel);

        public bool IsVideoActive => ReferenceEquals(CurrentPage, _videoViewModel);

        [RelayCommand]
        private void NavigateSubtitle()
        {
            if (ReferenceEquals(CurrentPage, _subtitleViewModel))
                return;

            CurrentPage = _subtitleViewModel;
            NotifyNavigationChanged();
        }

        [RelayCommand]
        private void NavigateVideo()
        {
            if (ReferenceEquals(CurrentPage, _videoViewModel))
                return;

            CurrentPage = _videoViewModel;
            NotifyNavigationChanged();
        }

        private void NotifyNavigationChanged()
        {
            OnPropertyChanged(nameof(CurrentPage));
            OnPropertyChanged(nameof(IsSubtitleActive));
            OnPropertyChanged(nameof(IsVideoActive));
        }
    }
}
