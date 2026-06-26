using System;
using System.IO;
using System.Threading.Tasks;
using LibVLCSharp.Shared;

namespace KaraMovieMaker.Services
{
    public sealed class AudioPlayerService : IDisposable
    {
        private MediaPlayer? _player;
        private Media? _media;

        public event EventHandler<TimeSpan>? TimeChanged;
        public event EventHandler<TimeSpan>? DurationChanged;
        public event EventHandler? PlaybackEnded;

        public AudioPlayerService()
        {
            LibVlcHost.EnsureInitialized();
            _player = new MediaPlayer(LibVlcHost.Instance);
            _player.TimeChanged += OnTimeChanged;
            _player.LengthChanged += OnLengthChanged;
            _player.EndReached += OnEndReached;
            _player.Volume = 80;
        }

        public bool IsLoaded => _media is not null;

        public bool IsPlaying => _player?.IsPlaying ?? false;

        public int Volume
        {
            get => _player?.Volume ?? 80;
            set
            {
                if (_player is not null)
                    _player.Volume = Math.Clamp(value, 0, 100);
            }
        }

        public TimeSpan Duration
        {
            get
            {
                var ms = GetDurationMs();
                return ms > 0 ? TimeSpan.FromMilliseconds(ms) : TimeSpan.Zero;
            }
        }

        public async Task<bool> TryLoadAsync(string path)
        {
            DisposeMedia();

            var fullPath = Path.GetFullPath(path);
            var demuxOptions = new[] { "avformat", "any", (string?)null };

            foreach (var demux in demuxOptions)
            {
                try
                {
                    _media = new Media(LibVlcHost.Instance, fullPath, FromType.FromPath);

                    if (demux is not null)
                        _media.AddOption($":demux={demux}");

                    _media.AddOption(":no-video");

                    await _media.Parse(
                        MediaParseOptions.ParseLocal | MediaParseOptions.FetchLocal,
                        timeout: 15000);

                    if (_media.ParsedStatus == MediaParsedStatus.Failed)
                    {
                        _media.Dispose();
                        _media = null;
                        continue;
                    }

                    _player!.Media = _media;
                    _player.SetRate(1f);
                    NotifyDuration();
                    return true;
                }
                catch
                {
                    _media?.Dispose();
                    _media = null;
                }
            }

            return false;
        }

        public void Play()
        {
            if (_player is null)
                return;

            _player.SetRate(1f);
            _player.Play();
            NotifyDuration();
        }

        public void Pause() => _player?.Pause();

        public void SeekToFraction(float fraction)
        {
            if (_player is null)
                return;

            SeekToFractionInternal(fraction);
        }

        public void SeekAndPlay(float fraction)
        {
            if (_player is null)
                return;

            _player.SetRate(1f);

            if (!_player.IsPlaying)
                _player.Play();

            SeekToFractionInternal(fraction);
        }

        private void SeekToFractionInternal(float fraction)
        {
            fraction = Math.Clamp(fraction, 0f, 1f);
            var lengthMs = GetDurationMs();

            if (lengthMs > 0)
                _player!.Time = (long)(fraction * lengthMs);
            else
                _player!.Position = fraction;
        }

        public void Dispose()
        {
            if (_player is not null)
            {
                _player.TimeChanged -= OnTimeChanged;
                _player.LengthChanged -= OnLengthChanged;
                _player.EndReached -= OnEndReached;
            }

            DisposeMedia();
            _player?.Dispose();
            _player = null;
            GC.SuppressFinalize(this);
        }

        private long GetDurationMs()
        {
            var ms = _media?.Duration ?? -1;
            if (ms <= 0)
                ms = _player?.Length ?? -1;

            return ms;
        }

        private void NotifyDuration()
        {
            var ms = GetDurationMs();
            if (ms > 0)
                DurationChanged?.Invoke(this, TimeSpan.FromMilliseconds(ms));
        }

        private void OnTimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
        {
            TimeChanged?.Invoke(this, TimeSpan.FromMilliseconds(e.Time));
        }

        private void OnLengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
        {
            if (e.Length > 0)
                DurationChanged?.Invoke(this, TimeSpan.FromMilliseconds(e.Length));
        }

        private void OnEndReached(object? sender, EventArgs e)
        {
            PlaybackEnded?.Invoke(this, EventArgs.Empty);
        }

        private void DisposeMedia()
        {
            _player?.Stop();
            _media?.Dispose();
            _media = null;
        }
    }
}
