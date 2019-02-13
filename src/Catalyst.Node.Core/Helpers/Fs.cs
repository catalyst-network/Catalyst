using System;
using System.IO;
using Dawn;
using Serilog;

namespace Catalyst.Node.Core.Helpers
{
    public interface IFileSystem {
        DirectoryInfo GetCatalystHomeDir();
    }

    public class Fs : IFileSystem
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
                Logger.Error($"Failed to create system folder {dataDir}", e);
                throw;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="dataDir"></param>
        /// <param name="network"></param>
        /// <param name="configDir"></param>
        /// <param name="modulesFile"></param>
        /// <returns></returns>
        public static void CopySkeletonConfigs(string dataDir,
            string network,
            string configDir = "Config",
            string modulesFile = "components.json")
        {
            Guard.Argument(dataDir, nameof(dataDir)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(network, nameof(network)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(configDir, nameof(configDir)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(modulesFile, nameof(modulesFile)).NotNull().NotEmpty().NotWhiteSpace();
            try
            {
                var sourceFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configDir, modulesFile);
                var targetFile = Path.Combine(dataDir, modulesFile);
                File.Copy(sourceFile, targetFile);

                var filename = network + ".json";
                sourceFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configDir, filename);
                targetFile = Path.Combine(dataDir, filename);
                File.Copy(sourceFile, targetFile);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message, e);
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