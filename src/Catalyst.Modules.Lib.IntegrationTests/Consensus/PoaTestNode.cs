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
using Autofac;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Repository;
using Catalyst.Common.P2P;
using Catalyst.Core.Lib.IntegrationTests;
using Catalyst.Modules.Lib.Dfs;
using Multiformats.Hash.Algorithms;
using NSubstitute;
using Xunit.Abstractions;

namespace Catalyst.Modules.Lib.IntegrationTests.Consensus
{
    public class PoaTestNode : TestCatalystNode
    {
        private readonly FileSystemDfs _dfs;
        private readonly AutoFillingMempool _mempool;
        private readonly IPeerRepository _peerRepository;
        private readonly IPeerIdentifier _nodePeerId;

        public PoaTestNode(IPeerIdentifier nodePeerId, IEnumerable<IPeerIdentifier> knownPeerIds, ITestOutputHelper output) 
            : base(nodePeerId.PublicKey.ToUtf8String(), output)
        {
            _nodePeerId = nodePeerId;
            _dfs = new FileSystemDfs(new BLAKE2B_128(), FileSystem);
            _mempool = new AutoFillingMempool();
            _peerRepository = Substitute.For<IPeerRepository>();
            var peersInRepo = knownPeerIds.Select(p => new Peer {PeerIdentifier = p});
            _peerRepository.GetAll().Returns(peersInRepo);
        }

        protected override void OverrideContainerBuilderRegistrations()
        {
            ContainerBuilder.RegisterInstance(_nodePeerId).As<IPeerIdentifier>();
            ContainerBuilder.RegisterInstance(_dfs).As<IDfs>();
            ContainerBuilder.RegisterInstance(_mempool).As<IMempool>();
            ContainerBuilder.RegisterInstance(_peerRepository).As<IPeerRepository>();
        }
    }
}
