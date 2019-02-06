using System;
using System.IO;
using Catalyst.Node.Core.Helpers.Logger;
using Dawn;

namespace Catalyst.Node.Core.Helpers
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
            var dir =
                Environment.OSVersion.Platform == PlatformID.Unix || //@TODO hook this into platform detection helper.
                Environment.OSVersion.Platform == PlatformID.MacOSX
                    ? Environment.GetEnvironmentVariable("HOME")
                    : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

            Guard.Argument(dir, nameof(dir)).NotNull().NotEmpty();

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
            try
            {
                Directory.CreateDirectory(dataDir);
            }
            catch (ArgumentNullException e)
            {
                LogException.Message(e.Message, e);
                throw;
            }
            catch (ArgumentException e)
            {
                LogException.Message(e.Message, e);
                throw;
            }
            catch (IOException e)
            {
                LogException.Message(e.Message, e);
                throw;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="dataDir"></param>
        /// <param name="network"></param>
        /// <returns></returns>
        public static void CopySkeletonConfigs(string dataDir,
            string network,
            string configDir = "Config",
            string modulesFiles = "comonents.json")
        {
            Guard.Argument(dataDir, nameof(dataDir)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(network, nameof(network)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(configDir, nameof(configDir)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(modulesFiles, nameof(modulesFiles)).NotNull().NotEmpty().NotWhiteSpace();
            try
            {
                File.Copy($"{AppDomain.CurrentDomain.BaseDirectory}/{configDir}/{modulesFiles}", dataDir);
            }
            catch (ArgumentNullException e)
            {
                LogException.Message(e.Message, e);
                throw;
            }
            catch (ArgumentException e)
            {
                LogException.Message(e.Message, e);
                throw;
            }
            catch (IOException e)
            {
                LogException.Message(e.Message, e);
                throw;
            }

            try
            {
                File.Copy($"{AppDomain.CurrentDomain.BaseDirectory}/{configDir}/{network}.json", dataDir);
            }
            catch (ArgumentNullException e)
            {
                LogException.Message(e.Message, e);
                throw;
            }
            catch (ArgumentException e)
            {
                LogException.Message(e.Message, e);
                throw;
            }
            catch (IOException e)
            {
                LogException.Message(e.Message, e);
                throw;
            }
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