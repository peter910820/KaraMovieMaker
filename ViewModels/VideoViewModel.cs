using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KaraMovieMaker.Services;

namespace KaraMovieMaker.ViewModels
{
    public partial class VideoViewModel : ViewModelBase, IDisposable
    {
        private readonly AudioPlayerService _player = new();
        private bool _isScrubbing;

        public VideoViewModel()
        {
            _player.TimeChanged += OnPlayerTimeChanged;
            _player.DurationChanged += OnPlayerDurationChanged;
            _player.PlaybackEnded += (_, _) => RunOnUi(() => IsPlaying = false);
            Volume = _player.Volume;
        }

        public string Title { get; } = "影片";
        public string Description { get; } = "媒體匯入與預覽";

        [ObservableProperty]
        private bool _isAudioLoaded;

        [ObservableProperty]
        private string _fileName = string.Empty;

        [ObservableProperty]
        private string _trackTitle = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private double _positionSeconds;

        [ObservableProperty]
        private double _durationSeconds;

        [ObservableProperty]
        private int _volume = 80;

        public string PositionLabel => FormatTime(PositionSeconds);
        public string DurationLabel => FormatTime(DurationSeconds);

        partial void OnPositionSecondsChanged(double value) =>
            OnPropertyChanged(nameof(PositionLabel));

        partial void OnDurationSecondsChanged(double value) =>
            OnPropertyChanged(nameof(DurationLabel));

        partial void OnVolumeChanged(int value) => _player.Volume = value;

        public void BeginScrub() => _isScrubbing = true;

        public void EndScrub() => _isScrubbing = false;

        /// <summary>fraction: 0.0 ~ 1.0</summary>
        public void ScrubToFraction(double fraction, bool andPlay)
        {
            if (!_player.IsLoaded)
                return;

            fraction = Math.Clamp(fraction, 0, 1);

            if (DurationSeconds > 0)
                PositionSeconds = fraction * DurationSeconds;

            if (andPlay)
            {
                _player.SeekAndPlay((float)fraction);
                IsPlaying = true;
            }
            else
            {
                _player.SeekToFraction((float)fraction);
            }
        }

        [RelayCommand]
        private async Task PickAudioAsync()
        {
            ErrorMessage = string.Empty;

            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            var topLevel = TopLevel.GetTopLevel(desktop.MainWindow);
            if (topLevel?.StorageProvider is not { } storageProvider)
                return;

            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "選擇媒體檔案",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("音訊 / 影片")
                    {
                        Patterns =
                        [
                            "*.mp3", "*.wav", "*.flac", "*.aac", "*.ogg", "*.wma", "*.m4a", "*.aiff",
                            "*.mp4", "*.mkv", "*.mov", "*.avi", "*.webm"
                        ]
                    },
                    new FilePickerFileType("所有檔案") { Patterns = ["*.*"] }
                ]
            });

            if (files.Count == 0)
                return;

            var path = files[0].TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(path))
            {
                ErrorMessage = "無法讀取所選檔案路徑。";
                return;
            }

            await LoadMediaAsync(path);
        }

        [RelayCommand]
        private void TogglePlayPause()
        {
            if (!_player.IsLoaded)
                return;

            if (_player.IsPlaying)
            {
                _player.Pause();
                IsPlaying = false;
                return;
            }

            if (DurationSeconds > 0 && PositionSeconds >= DurationSeconds)
                _player.SeekToFraction(0);

            _player.Play();
            IsPlaying = true;
        }

        public void Dispose()
        {
            _player.TimeChanged -= OnPlayerTimeChanged;
            _player.DurationChanged -= OnPlayerDurationChanged;
            _player.Dispose();
        }

        private async Task LoadMediaAsync(string path)
        {
            IsPlaying = false;

            if (!await _player.TryLoadAsync(path))
            {
                IsAudioLoaded = false;
                FileName = string.Empty;
                TrackTitle = string.Empty;
                ErrorMessage = "無法開啟此媒體檔案，請確認檔案是否損毀。";
                DurationSeconds = 0;
                PositionSeconds = 0;
                return;
            }

            FileName = Path.GetFileName(path);
            TrackTitle = Path.GetFileNameWithoutExtension(path);
            DurationSeconds = _player.Duration.TotalSeconds;
            PositionSeconds = 0;
            IsAudioLoaded = true;
            ErrorMessage = string.Empty;
        }

        private void OnPlayerTimeChanged(object? sender, TimeSpan time)
        {
            RunOnUi(() =>
            {
                if (_isScrubbing)
                    return;

                PositionSeconds = time.TotalSeconds;

                if (IsPlaying && DurationSeconds > 0 && time.TotalSeconds >= DurationSeconds - 0.05)
                    IsPlaying = false;
            });
        }

        private void OnPlayerDurationChanged(object? sender, TimeSpan duration)
        {
            RunOnUi(() =>
            {
                DurationSeconds = duration.TotalSeconds;
                OnPropertyChanged(nameof(DurationLabel));
            });
        }

        private static void RunOnUi(Action action)
        {
            if (Dispatcher.UIThread.CheckAccess())
                action();
            else
                Dispatcher.UIThread.Post(action);
        }

        private static string FormatTime(double totalSeconds)
        {
            totalSeconds = Math.Max(0, totalSeconds);
            var total = (int)Math.Floor(totalSeconds);
            var hours = total / 3600;
            var minutes = (total % 3600) / 60;
            var seconds = total % 60;

            return hours > 0
                ? $"{hours}:{minutes:D2}:{seconds:D2}"
                : $"{minutes}:{seconds:D2}";
        }
    }
}
