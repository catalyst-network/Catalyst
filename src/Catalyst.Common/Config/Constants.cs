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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Catalyst.Common.Enumerator;
using Catalyst.Common.Modules;

namespace Catalyst.Common.Config
{
    public static class Constants
    {
        public static string ConfigSubFolder => "Config";
        public static string ModulesSubFolder => "Modules";
        public static string ComponentsJsonConfigFile => "components.json";
        public static string SerilogJsonConfigFile => "serilog.json";
        private static string JsonFilePattern => "{0}.json";
        internal static string CatalystSubFolder => ".Catalyst";
        public static string ShellComponentsJsonConfigFile => "shell.components.json";
        public static string ShellNodesConfigFile => "nodes.json";
        public static string ShellConfigFile => "shell.config.json";
        public static string MessageHandlersConfigFile => "messageHandlers.json";

        /// <summary>The expiry minutes of initialization</summary>
        public const int FileTransferExpiryMinutes = 1;

        /// <summary>The chunk size in bytes</summary>
        public const int FileTransferChunkSize = 1000000;

        /// <summary>The CLI chunk writing wait time</summary>
        public const int FileTransferRpcWaitTime = 30;

        /// <summary>The maximum chunk retry count</summary>
        public const int FileTransferMaxChunkRetryCount = 3;

        /// <summary>The maximum chunk read tries</summary>
        public const int FileTransferMaxChunkReadTries = 3;
        
        public static IEnumerable<string> AllModuleFiles =>
            Enumeration.GetAll<ModuleName>()
               .Select(m => Path.Combine(ModulesSubFolder, string.Format(JsonFilePattern, m.Name.ToLower())));

        public static string NetworkConfigFile(Network network) { return string.Format(JsonFilePattern, network.Name); }
    }
}
