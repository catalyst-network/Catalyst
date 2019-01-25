using System;
using System.IO;
using Catalyst.Helpers.Util;
using Dawn;

namespace Catalyst.Helpers.FileSystem
{
    public static class Fs
    {
        /// <summary>
        ///     Gets current home directory.
        /// </summary>
        /// <param name="platform"></param>
        /// <returns></returns>
        public static DirectoryInfo GetUserHomeDir()
        {
            var dir = Environment.OSVersion.Platform == PlatformID.Unix ||
                      Environment.OSVersion.Platform == PlatformID.MacOSX
                ? Environment.GetEnvironmentVariable("HOME")
                : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

            if (dir == null) throw new Exception();

            return new DirectoryInfo(dir);
        }

        /// <summary>
        /// </summary>
        /// <param name="dataDir"></param>
        /// <returns></returns>
        public static bool DataDirCheck(string dataDir)
        {
            Guard.Argument(dataDir, nameof(dataDir)).NotNull().NotEmpty().NotWhiteSpace();
            return Directory.Exists(dataDir);
        }

        /// <summary>
        /// </summary>
        /// <param name="dataDir"></param>
        public static void CreateSystemFolder(string dataDir)
        {
            Guard.Argument(dataDir, nameof(dataDir)).NotNull().NotEmpty().NotWhiteSpace();
            Directory.CreateDirectory(dataDir);
        }

        /// <summary>
        /// </summary>
        /// <param name="dataDir"></param>
        /// <param name="network"></param>
        /// <returns></returns>
        public static void CopySkeletonConfigs(string dataDir, string network, string configDir = "Config", string modulesFiles = "comonents.json")
        {
            Guard.Argument(dataDir, nameof(dataDir)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(network, nameof(network)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(configDir, nameof(configDir)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(modulesFiles, nameof(modulesFiles)).NotNull().NotEmpty().NotWhiteSpace();
            File.Copy($"{AppDomain.CurrentDomain.BaseDirectory}/{configDir}/{modulesFiles}", dataDir);
            File.Copy($"{AppDomain.CurrentDomain.BaseDirectory}/{configDir}/{network}.json", dataDir);
        }

        /// <summary>
        ///     Checks a config exists for a network in given location
        /// </summary>
        /// <param name="dataDir"></param>
        /// <param name="network"></param>
        /// <returns></returns>
        public static bool CheckConfigExists(string dataDir, string network)
        {
            Guard.Argument(dataDir, nameof(dataDir)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(network, nameof(network)).NotNull().NotEmpty().NotWhiteSpace();
            return File.Exists(dataDir + "/" + network + ".json");
        }
    }
}