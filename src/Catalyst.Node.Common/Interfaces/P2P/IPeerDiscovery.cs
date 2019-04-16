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
using System.Net;
using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Interfaces.Messaging;
using Catalyst.Node.Common.P2P;
using Catalyst.Protocol.Common;
using Microsoft.Extensions.Configuration;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Node.Common.Interfaces.P2P
{
    public interface IPeerDiscovery : IPassiveObserver
    {
        IDns Dns { get; }
        ILogger Logger { get; }
        IList<string> SeedNodes { get; }
        IList<IPEndPoint> Peers { get; }
        IRepository<Peer> PeerRepository { get; }
        Task GetSeedNodesFromDns(IList<string> seedServers);
        void ParseDnsServersFromConfig(IConfigurationRoot rootSection);
    }
}
