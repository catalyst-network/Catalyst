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

using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Wire;
using MultiFormats.Registry;
using NSubstitute;
using SharpRepository.InMemoryRepository;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Catalyst.Core.Lib.P2P.Repository;

namespace Catalyst.Core.Modules.Sync.Tests.UnitTests
{
    public class PeerSyncManagerUnitTests
    {
        private IPeerClient _peerClient;
        private IHashProvider _hashProvider;
        private IPeerService _peerService;
        private IPeerRepository _peerRepository;
        private ReplaySubject<ProtocolMessage> _deltaHeightReplaySubject;

        //todo add unit tests
        public PeerSyncManagerUnitTests()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));
            _peerService = Substitute.For<IPeerService>();
            _peerClient = Substitute.For<IPeerClient>();
            _peerRepository = new PeerRepository(new InMemoryRepository<Peer, string>());
            _deltaHeightReplaySubject = new ReplaySubject<ProtocolMessage>(1);
            _peerService.MessageStream.Returns(_deltaHeightReplaySubject.AsObservable());
        }
    }
}
