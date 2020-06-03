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
using Catalyst.Abstractions.Rpc;
using Microsoft.Extensions.Configuration;
using MultiFormats;

namespace Catalyst.Core.Modules.Rpc.Client
{
    public sealed class RpcClientSettings : IRpcClientConfig
    {
        public static IList<IRpcClientConfig> BuildRpcNodeSettingList(IConfigurationRoot config)
        {
            var section = config.GetSection("CatalystCliRpcNodes").GetSection("nodes");
            var children = section.GetChildren().Select(c=>c.GetChildren());
            var nodeList = section.GetChildren().Select(child => new RpcClientSettings
            {
                NodeId = child.GetSection("nodeId").Value,
                PfxFileName = child.GetSection("PfxFileName").Value,
                SslCertPassword = child.GetSection("SslCertPassword").Value,
                Address = child.GetSection("Address").Value
            } as IRpcClientConfig).ToList();

            return nodeList;
        }

        public MultiAddress Address { get; set; }
        public string NodeId { get; set; }
        public string PfxFileName { get; set; }
        public string SslCertPassword { get; set; }
    }
}
