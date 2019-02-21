using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
            var networkAsString = Enum.GetName(typeof(NodeOptions.Networks), network);

            var dataDirInfo = new DirectoryInfo(dataDir);
            if (!dataDirInfo.Exists)
            {
                dataDirInfo.Create();
            }

            var existingConfigs = dataDirInfo.EnumerateFiles("*.json").Select(fi => fi.Name);

            var requiredConfigFiles = new[]
            {
                Constants.NetworkConfigFile(network),
                Constants.ComponentsJsonConfigFile,
                Constants.SerilogJsonConfigFile
            };

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
            var sourceFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.ConfigFolder, fileName);
            var targetFile = Path.Combine(targetFolder, fileName);
            if (!overwrite && File.Exists(targetFile))
            {
                return;
            }
            File.Copy(sourceFile, targetFile, overwrite);
        }
    }
}
