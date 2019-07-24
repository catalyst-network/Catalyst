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
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Repository;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.P2P;
using Catalyst.Core.Lib.IntegrationTests;
using Catalyst.Core.Lib.Rpc;
using Catalyst.Modules.Lib.Dfs;
using Catalyst.TestUtils;
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
        private readonly IPeerSettings _nodeSettings;
        private readonly IPeerIdentifier _nodePeerId;
        private readonly IRpcServerSettings _rpcSettings;

        public PoaTestNode(IPeerSettings nodeSettings, IEnumerable<IPeerIdentifier> knownPeerIds, ITestOutputHelper output) 
            : base(nodeSettings.PublicKey, output)
        {
            _nodeSettings = nodeSettings;

            _rpcSettings = RpcServerSettingsHelper.GetRpcServerSettings(nodeSettings.Port + 100);
            _nodePeerId = new PeerIdentifier(nodeSettings);
            _dfs = new FileSystemDfs(new BLAKE2B_128(), FileSystem);
            _mempool = new AutoFillingMempool();
            _peerRepository = Substitute.For<IPeerRepository>();
            var peersInRepo = knownPeerIds.Select(p => new Peer {PeerIdentifier = p});
            _peerRepository.GetAll().Returns(peersInRepo);
        }

        protected override void OverrideContainerBuilderRegistrations()
        {
            ContainerBuilder.RegisterInstance(_nodeSettings).As<IPeerSettings>();
            ContainerBuilder.RegisterInstance(_rpcSettings).As<IRpcServerSettings>();
            ContainerBuilder.RegisterInstance(_nodePeerId).As<IPeerIdentifier>();
            ContainerBuilder.RegisterInstance(_dfs).As<IDfs>();
            ContainerBuilder.RegisterInstance(_mempool).As<IMempool>();
            ContainerBuilder.RegisterInstance(_peerRepository).As<IPeerRepository>();
        }
    }
}
