#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Catalyst.Common.Interfaces.Config;
using Dawn;

namespace Catalyst.Common.Config
{
    public class ConfigCopier : IConfigCopier
    {
        /// <summary>
        ///     Finds out which config files are missing from the catalyst home directory and
        ///     copies them over if needed.
        /// </summary>
        /// <param name="dataDir">Home catalyst directory</param>
        /// <param name="network">Network on which to run the node</param>
        /// <param name="sourceFolder"></param>
        /// <param name="overwrite">Should config existing config files be overwritten by default?</param>
        public void RunConfigStartUp(string dataDir, Network networkParam = null, string sourceFolder = null, bool overwrite = false)
        {
            Guard.Argument(dataDir, nameof(dataDir)).NotNull().NotEmpty().NotWhiteSpace();

            var network = networkParam == null ? Network.Dev : networkParam;

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

            var requiredConfigFiles = RequiredConfigFiles(network);

            //TODO: think about case sensitivity of the environment we are in, this is oversimplified
            var filenameComparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparer.InvariantCultureIgnoreCase
                : StringComparer.InvariantCulture;

            var filesToCopy = overwrite
                ? requiredConfigFiles
                : requiredConfigFiles.Except(existingConfigs, filenameComparer);

            foreach (var fileName in filesToCopy)
            {
                CopyConfigFileToFolder(dataDir, fileName,
                    sourceFolder ?? AppDomain.CurrentDomain.BaseDirectory, overwrite);
            }
        }

        protected virtual IEnumerable<string> RequiredConfigFiles(Network network)
        {
            var requiredConfigFiles = new[]
            {
                Constants.NetworkConfigFile(network),
                Constants.ComponentsJsonConfigFile,
                Constants.SerilogJsonConfigFile,
                Constants.MessageHandlersConfigFile
            };
            return requiredConfigFiles;
        }

        private static void CopyConfigFileToFolder(string targetFolder,
            string fileName,
            string sourceFolder,
            bool overwrite = false)
        {
            var combinedSourceFolder = Path.Combine(sourceFolder, Constants.ConfigSubFolder);
            var sourceFile = new DirectoryInfo(combinedSourceFolder).Exists ? Path.Combine(combinedSourceFolder, fileName) : Path.Combine(sourceFolder, fileName);

            var targetFile = Path.Combine(targetFolder, fileName);
            if (!overwrite && File.Exists(targetFile))
            {
                return;
            }

            File.Copy(sourceFile, targetFile, overwrite);
        }
    }
}
