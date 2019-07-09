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

using System.Collections.Concurrent;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Network;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Discovery;
using Catalyst.Common.P2P;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.P2P.Discovery
{
    public class HastingsDiscovery : IHastingsDiscovery
    {
        public ILogger Logger { get; }
        public IRepository<Peer> PeerRepository { get; }
        public Task DiscoveryAsync() { throw new System.NotImplementedException(); }
        public IDns Dns { get; }
        public IProducerConsumerCollection<IPeerIdentifier> Peers { get; }

        public HastingsDiscovery(ILogger logger, IRepository<Peer> peerRepository, IDns dns, IPeerSettings peerSettings)
        {
            Logger = logger;
            PeerRepository = peerRepository;
            Dns = dns;
            
            Peers.TryAdd(Dns.GetSeedNodesFromDns(peerSettings.SeedServers).RandomElement());
        }
    }
}
