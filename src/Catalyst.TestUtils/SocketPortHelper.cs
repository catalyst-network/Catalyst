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
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Catalyst.TestUtils
{
    public static class SocketPortHelper
    {
        public static void AlterConfigurationToGetUniquePort(IConfigurationRoot config, string currentTestName)
        {
            var serverSection = config.GetSection("CatalystNodeConfiguration").GetSection("Rpc");
            var peerSection = config.GetSection("CatalystNodeConfiguration").GetSection("Peer");

            var randomPort = int.Parse(serverSection.GetSection("Port").Value) +
                new Random(currentTestName.GetHashCode()).Next(0, 500);

            serverSection.GetSection("Port").Value = randomPort.ToString();
            peerSection.GetSection("Port").Value = (randomPort + 1).ToString();

            var clientSection = config.GetSection("CatalystCliRpcNodes").GetSection("nodes");
            clientSection.GetChildren().ToList().ForEach(c => { c.GetSection("port").Value = randomPort.ToString(); });
        }
    }
}
