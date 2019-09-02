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
using System.Linq;
using System.Net;
using Catalyst.Abstractions.Rpc;
using Microsoft.Extensions.Configuration;

namespace Catalyst.Core.Rpc
{
    public sealed class RpcClientSettings : IRpcNodeConfig
    {
        public static IList<IRpcNodeConfig> BuildRpcNodeSettingList(IConfigurationRoot config)
        {
            var section = config.GetSection("CatalystCliRpcNodes").GetSection("nodes");

            var nodeList = section.GetChildren().Select(child => new RpcClientSettings
            {
                NodeId = child.GetSection("nodeId").Value,
                HostAddress = IPAddress.Parse(child.GetSection("host").Value),
                Port = int.Parse(child.GetSection("port").Value),
                PfxFileName = child.GetSection("PfxFileName").Value,
                SslCertPassword = child.GetSection("SslCertPassword").Value,
                PublicKey = child.GetSection("PublicKey").Value
            } as IRpcNodeConfig).ToList();

            return nodeList;
        }

        public string NodeId { get; set; }
        public IPAddress HostAddress { get; set; }
        public int Port { get; set; }
        public string PfxFileName { get; set; }
        public string SslCertPassword { get; set; }
        public string PublicKey { get; set; }
    }
}
