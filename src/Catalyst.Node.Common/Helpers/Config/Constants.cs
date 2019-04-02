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
using Catalyst.Node.Common.Helpers.Enumerator;
using Catalyst.Node.Common.Modules;

namespace Catalyst.Node.Common.Helpers.Config
{
    public static class Constants
    {
        public static string ConfigSubFolder => "Config";
        public static string ModulesSubFolder => "Modules";
        public static string ComponentsJsonConfigFile => "components.json";
        public static string SerilogJsonConfigFile => "serilog.json";
        private static string JsonFilePattern => "{0}.json";
        public static string CatalystSubFolder => ".Catalyst";
        public static string ShellComponentsJsonConfigFile => "shell.components.json";
        public static string ShellNodesConfigFile => "nodes.json";

        public static IEnumerable<string> AllModuleFiles =>
            Enumeration.GetAll<ModuleName>()
               .Select(m => Path.Combine(ModulesSubFolder, string.Format(JsonFilePattern, m.Name.ToLower())));

        public static string NetworkConfigFile(Network network) { return string.Format(JsonFilePattern, network.Name); }
    }
}