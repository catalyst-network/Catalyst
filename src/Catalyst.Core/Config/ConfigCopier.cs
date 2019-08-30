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
using Catalyst.Abstractions.Config;
using Dawn;

namespace Catalyst.Core.Config
{
    public class ConfigCopier : IConfigCopier
    {
        /// <inheritdoc />
        public void RunConfigStartUp(string dataDir,
            Protocol.Common.Network network = Protocol.Common.Network.Devnet,
            string sourceFolder = null,
            bool overwrite = false,
            string overrideNetworkFile = null)
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

            string[] searchPatterns = {"*.json", "*.xml"};

            foreach (var searchPattern in searchPatterns)
            {
                var existingConfigs = dataDirInfo
                   .EnumerateFiles(searchPattern, SearchOption.TopDirectoryOnly)
                   .Select(fi => fi.Name)
                   .Concat(modulesFolderInfo.EnumerateFiles(searchPattern)
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
        }

        protected virtual IEnumerable<string> RequiredConfigFiles(Protocol.Common.Network network,
            string overrideNetworkFile = null)
        {
            var requiredConfigFiles = new[]
            {
                Constants.NetworkConfigFile(network, overrideNetworkFile),
                Constants.ComponentsJsonConfigFile,
                Constants.SerilogJsonConfigFile,
                Constants.MessageHandlersConfigFile,
                Constants.RpcAuthenticationCredentialsFile
            };
            return requiredConfigFiles;
        }

        private static void CopyConfigFileToFolder(string targetFolder,
            string fileName,
            string sourceFolder,
            bool overwrite = false)
        {
            var combinedSourceFolder = Path.Combine(sourceFolder, Constants.ConfigSubFolder);
            var sourceFile = new DirectoryInfo(combinedSourceFolder).Exists
                ? Path.Combine(combinedSourceFolder, fileName)
                : Path.Combine(sourceFolder, fileName);

            var targetFile = Path.Combine(targetFolder, fileName);
            if (!overwrite && File.Exists(targetFile))
            {
                return;
            }

            File.Copy(sourceFile, targetFile, overwrite);
        }
    }
}
