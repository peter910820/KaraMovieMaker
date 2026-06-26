using System;
using Avalonia.Controls;
using Avalonia.Input;
using KaraMovieMaker.ViewModels;

namespace KaraMovieMaker.Views
{
    public partial class VideoView : UserControl
    {
        public VideoView()
        {
            InitializeComponent();
        }

        private void OnProgressPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Border host || DataContext is not VideoViewModel vm)
                return;

            vm.BeginScrub();
            Scrub(host, e, vm);
            e.Pointer.Capture(host);
        }

        private void OnProgressPointerMoved(object? sender, PointerEventArgs e)
        {
            if (sender is not Border host || DataContext is not VideoViewModel vm)
                return;

            if (!ReferenceEquals(e.Pointer.Captured, host))
                return;

            Scrub(host, e, vm);
        }

        private void OnProgressPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (sender is not Border host || DataContext is not VideoViewModel vm)
                return;

            Scrub(host, e, vm);
            vm.EndScrub();
            e.Pointer.Capture(null);
        }

        private static void Scrub(Border host, PointerEventArgs e, VideoViewModel vm)
        {
            var width = host.Bounds.Width;
            if (width <= 0)
                return;

            var fraction = Math.Clamp(e.GetPosition(host).X / width, 0, 1);
            vm.ScrubToFraction(fraction, andPlay: true);
        }
    }
}
