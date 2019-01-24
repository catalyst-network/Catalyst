using System;
using System.IO;

namespace Catalyst.Helpers.Platform
{
    /// <summary>
    ///     Utility class for runtime platform detection
    /// </summary>
    public static class Detection
    {
        /// <summary>
        ///     True if runtime platform is Linux
        /// </summary>
        private static bool _isLinux;

        /// <summary>
        ///     True if runtime platform is Mac OS X
        /// </summary>
        private static bool _isMacOsX;

        /// <summary>
        ///     True if runtime platform is Windows
        /// </summary>
        private static bool _isWindows;

        /// <summary>
        ///     True if 64-bit runtime is used
        /// </summary>
        public static bool Uses64BitRuntime => IntPtr.Size == 8;

        /// <summary>
        ///     True if 32-bit runtime is used
        /// </summary>
        public static bool Uses32BitRuntime => IntPtr.Size == 4;

        /// <summary>
        ///     True if runtime platform is Windows
        /// </summary>
        private static bool IsWindows
        {
            get
            {
                DetectPlatform();
                return _isWindows;
            }
        }

        /// <summary>
        ///     True if runtime platform is Linux
        /// </summary>
        private static bool IsLinux
        {
            get
            {
                DetectPlatform();
                return _isLinux;
            }
        }

        /// <summary>
        ///     True if runtime platform is Mac OS X
        /// </summary>
        private static bool IsMacOsX
        {
            get
            {
                DetectPlatform();
                return _isMacOsX;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static uint Os()
        {
            uint platform = 0;

            if (IsLinux)
                platform = 1;
            else if (IsMacOsX)
                platform = 2;
            else if (IsWindows) platform = 3;

            if (platform == 0) throw new Exception();

            return platform;
        }

        /// <summary>
        ///     Performs platform detection
        /// </summary>
        private static void DetectPlatform()
        {
            // Supported platform has already been detected
            if (_isWindows || _isLinux || _isMacOsX)
                return;

            var winDir = Environment.GetEnvironmentVariable("windir");
            if (!string.IsNullOrEmpty(winDir) && winDir.Contains(@"\") && Directory.Exists(winDir))
            {
                _isWindows = true;
            }
            else if (File.Exists(@"/proc/sys/kernel/ostype"))
            {
                var osType = File.ReadAllText(@"/proc/sys/kernel/ostype");
                if (osType.StartsWith("Linux", StringComparison.OrdinalIgnoreCase))
                    _isLinux = true;
                else
                    throw new UnsupportedPlatformException(
                        $"Catalyst Catalyst.Node is not supported on \"{osType}\" platform");
            }
            else if (File.Exists(@"/System/Library/CoreServices/SystemVersion.plist"))
            {
                _isMacOsX = true;
            }
            else
            {
                throw new UnsupportedPlatformException("Catalyst Catalyst.Node is not supported on this platform");
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal class UnsupportedPlatformException : Exception
    {
        /// <summary>
        ///     Initializes new instance of UnsupportedPlatformException class
        /// </summary>
        /// <param name="message"></param>
        public UnsupportedPlatformException(string message) : base(message)
        {
        }
    }
}