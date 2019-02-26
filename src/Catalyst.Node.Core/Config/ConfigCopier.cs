using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Catalyst.Node.Common.Modules;
using Dawn;

namespace Catalyst.Node.Core.Config
{
    public class ConfigCopier
    {
        private const bool OverwriteFilesByDefault = false;

        /// <summary>
        /// Finds out which config files are missing from the catalyst home directory and
        /// copies them over if needed.
        /// </summary>
        /// <param name="dataDir">Home catalyst directory</param>
        /// <param name="network">Network on which to run the node</param>
        /// <param name="overwrite">Should config existing config files be overwritten by default?</param>
        public void RunConfigStartUp(string dataDir, NodeOptions.Networks network, bool overwrite = OverwriteFilesByDefault)
        {
            Guard.Argument(dataDir, nameof(dataDir)).NotNull().NotEmpty().NotWhiteSpace();

            var dataDirInfo = new DirectoryInfo(dataDir);
            if (!dataDirInfo.Exists)
            {
                dataDirInfo.Create();
            }

            var modulesFolderInfo = new DirectoryInfo(Path.Combine(dataDir, Constants.ModulesSubFolder));
            if (!modulesFolderInfo.Exists)
            {
                modulesFolderInfo.Create();
            }

            const string jsonSearchPattern = "*.json";
            var existingConfigs = dataDirInfo
               .EnumerateFiles(jsonSearchPattern, SearchOption.TopDirectoryOnly)
               .Select(fi => fi.Name).Concat(
                    modulesFolderInfo.EnumerateFiles(jsonSearchPattern)
                       .Select(m => Path.Combine(Constants.ModulesSubFolder, m.Name)));

            var requiredConfigFiles = new[]
            {
                Constants.NetworkConfigFile(network),
                Constants.ComponentsJsonConfigFile,
                Constants.SerilogJsonConfigFile
            }.Concat(Constants.AllModuleFiles);

            //TODO: think about case sensitivity of the environment we are in, this is oversimplified
            var filenameComparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparer.InvariantCultureIgnoreCase
                : StringComparer.InvariantCulture;

            var filesToCopy = overwrite
                ? requiredConfigFiles
                : requiredConfigFiles.Except(existingConfigs, filenameComparer);

            foreach (var fileName in filesToCopy)
            {
                CopyConfigFileToFolder(dataDir, fileName, overwrite);
            }
        }

        private void CopyConfigFileToFolder(string targetFolder, string fileName,
            bool overwrite = OverwriteFilesByDefault)
        {
            var sourceFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.ConfigSubFolder, fileName);
            var targetFile = Path.Combine(targetFolder, fileName);
            if (!overwrite && File.Exists(targetFile))
            {
                return;
            }
            File.Copy(sourceFile, targetFile, overwrite);
        }
    }
}
