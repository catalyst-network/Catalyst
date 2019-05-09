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
        // <summary> Folder with config files </summary>
        public static string ConfigSubFolder => "Config";
        
        // <summary> Folder with modules for Catalyst.Node </summary>
        public static string ModulesSubFolder => "Modules";
        
        // <summary> Config file with Catalyst.Node component registrations for autofac </summary>
        public static string ComponentsJsonConfigFile => "components.json";
        
        // <summary> Serilog configuration file </summary>
        public static string SerilogJsonConfigFile => "serilog.json";
        
        // <summary> Search pattern for Json files </summary>
        private static string JsonFilePattern => "{0}.json";
        
        // <summary> Default Catalyst data directory </summary>
        internal static string CatalystSubFolder => ".Catalyst";
        
        // <summary> Config file with Catalyst.Cli component registrations for autofac </summary>
        public static string ShellComponentsJsonConfigFile => "shell.components.json";
        
        // <summary> Config file with nodes for use in rpc client </summary>
        public static string ShellNodesConfigFile => "nodes.json";
        
        // <summary> Shell configuration file </summary>
        public static string ShellConfigFile => "shell.config.json";
        
        // <summary> Registration of message handlers for autofac </summary>
        public static string MessageHandlersConfigFile => "messageHandlers.json";

        /// <summary>The expiry minutes of initialization </summary>
        public static int FileTransferExpirySeconds => 60;

        /// <summary>The chunk size in bytes </summary>
        public static int FileTransferChunkSize => 200000;
        
        /// <summary>The maximum chunk retry count </summary>
        public static int FileTransferMaxChunkRetryCount => 3;

        /// <summary>The maximum chunk read tries </summary>
        public static int FileTransferMaxChunkReadTries => 30;
        
        public static IEnumerable<string> AllModuleFiles =>
            Enumeration.GetAll<ModuleName>()
               .Select(m => Path.Combine(ModulesSubFolder, string.Format(JsonFilePattern, m.Name.ToLower())));

        public static string NetworkConfigFile(Network network) { return string.Format(JsonFilePattern, network.Name); }
    }
}
