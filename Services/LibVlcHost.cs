using System;
using System.IO;
using System.Runtime.InteropServices;
using LibVLCSharp.Shared;

namespace KaraMovieMaker.Services
{
    internal static class LibVlcHost
    {
        private static LibVLC? _instance;
        private static bool _initialized;

        public static LibVLC Instance
        {
            get
            {
                EnsureInitialized();
                return _instance!;
            }
        }

        public static void EnsureInitialized()
        {
            if (_initialized)
                return;

            var libvlcPath = GetLibVlcDirectory();
            if (Directory.Exists(libvlcPath))
                Core.Initialize(libvlcPath);
            else
                Core.Initialize();

            _instance = new LibVLC();
            _initialized = true;
        }

        private static string GetLibVlcDirectory()
        {
            var archFolder = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "win-x64",
                Architecture.X86 => "win-x86",
                Architecture.Arm64 => "win-arm64",
                _ => "win-x64"
            };

            return Path.Combine(AppContext.BaseDirectory, "libvlc", archFolder);
        }
    }
}
