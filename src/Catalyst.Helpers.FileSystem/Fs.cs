using System;
using System.IO;
using Catalyst.Helpers.Util;

namespace Catalyst.Helpers.FileSystem
{
    public static class Fs
    {
        /// <summary>
        ///     Gets current home directory. @TODO move FS functions from CatalystSystem to here.
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
            Guard.NotNull(dataDir, nameof(dataDir));
            return Directory.Exists(dataDir);
        }

        /// <summary>
        /// </summary>
        /// <param name="dataDir"></param>
        public static void CreateSystemFolder(string dataDir)
        {
            Guard.NotNull(dataDir, nameof(dataDir));
            Directory.CreateDirectory(dataDir);
        }

        /// <summary>
        /// </summary>
        /// <param name="dataDir"></param>
        /// <param name="network"></param>
        /// <returns></returns>
        public static void CopySkeletonConfigs(string dataDir, string network)
        {
            Guard.NotNull(dataDir, nameof(dataDir));
            Guard.NotNull(network, nameof(network));
            Guard.NotEmpty(dataDir, nameof(dataDir));
            Guard.NotEmpty(network, nameof(network));
            File.Copy(AppDomain.CurrentDomain.BaseDirectory + "/Config/components.json", dataDir);
            File.Copy(AppDomain.CurrentDomain.BaseDirectory + "/Config/" + network + ".json", dataDir);
        }

        /// <summary>
        ///     Checks a config exists for a network in given location
        /// </summary>
        /// <param name="dataDir"></param>
        /// <param name="network"></param>
        /// <returns></returns>
        public static bool CheckConfigExists(string dataDir, string network)
        {
            Guard.NotNull(dataDir, nameof(dataDir));
            Guard.NotNull(network, nameof(network));
            Guard.NotEmpty(dataDir, nameof(dataDir));
            Guard.NotEmpty(network, nameof(network));
            return File.Exists(dataDir + "/" + network + ".json");
        }
    }
}