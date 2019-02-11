using System;
using System.IO;
using Catalyst.Node.Core.Helpers.Logger;
using Dawn;

namespace Catalyst.Node.Core.Helpers
{
    public interface IFileSystem {
        DirectoryInfo GetCatalystHomeDir();
    }

    public class Fs : IFileSystem
    {
        public const string CatalystSubfolder = ".Catalyst";
        
        private static string GetUserHomeDir()
        {
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return homePath;
        }

        public DirectoryInfo GetCatalystHomeDir()
        {
            var path = Path.Combine(GetUserHomeDir(), CatalystSubfolder);
            return new DirectoryInfo(path);
        }
        
        public static bool DirectoryExists(string dataDir)
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
            catch (Exception e)
            {
                LogException.Message(e.Message, e);
                throw;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="dataDir"></param>
        /// <param name="network"></param>
        /// <param name="configDir"></param>
        /// <param name="modulesFiles"></param>
        /// <returns></returns>
        public static void CopySkeletonConfigs(string dataDir,
            string network,
            string configDir = "Config",
            string modulesFiles = "components.json")
        {
            Guard.Argument(dataDir, nameof(dataDir)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(network, nameof(network)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(configDir, nameof(configDir)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(modulesFiles, nameof(modulesFiles)).NotNull().NotEmpty().NotWhiteSpace();
            try
            {
                var sourceFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configDir, modulesFiles);
                File.Copy(sourceFolder, dataDir);

                var filename = network + ".json";
                var sourceFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configDir, filename);
                File.Copy(sourceFile, dataDir);
            }
            catch (Exception e)
            {
                LogException.Message(e.Message, e);
                throw;
            }
        }

        /// <summary>
        /// Checks a config exists for a network in given location
        /// </summary>
        /// <param name="dataDir"></param>
        /// <param name="network"></param>
        /// <returns></returns>
        public static bool CheckConfigExists(string dataDir, string network)
        {
            Guard.Argument(dataDir, nameof(dataDir)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(network, nameof(network)).NotNull().NotEmpty().NotWhiteSpace();
            var filePath = Path.Combine(dataDir, network + ".json");
            return File.Exists(filePath);
        }
    }
}