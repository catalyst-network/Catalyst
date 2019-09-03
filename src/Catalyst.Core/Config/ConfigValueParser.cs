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

using System.Linq;
using System.Net;
using Catalyst.Core.Network;
using Microsoft.Extensions.Configuration;

namespace Catalyst.Core.Config
{
    public static class ConfigValueParser
    {
        /// <summary>
        ///     use to parse array of IPEndpoints from config
        /// </summary>
        /// <param name="configurationRoot"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        internal static IPEndPoint[] GetIpEndpointArrValues(IConfigurationRoot configurationRoot, string section)
        {
            return configurationRoot.GetSection("CatalystNodeConfiguration")
               .GetSection("Peer")
               .GetSection(section)
               .GetChildren()
               .Select(p => EndpointBuilder.BuildNewEndPoint(p.Value)).ToArray();
        }

        /// <summary>
        ///     Use to parse array of strings from config
        /// </summary>
        /// <param name="configurationRoot"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        public static string[] GetStringArrValues(IConfigurationRoot configurationRoot, string section)
        {
            return configurationRoot.GetSection("CatalystNodeConfiguration")
               .GetSection("Peer")
               .GetSection(section)
               .GetChildren()
               .Select(p => p.Value).ToArray();
        }
    }
}
