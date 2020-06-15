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
using System.Text;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Core.Abstractions.Sync;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Core.Modules.Consensus.IO.Observers;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using MultiFormats.Registry;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using MultiFormats;
using Catalyst.Core.Modules.Consensus.Deltas;

namespace Catalyst.Core.Modules.Consensus.Tests.UnitTests.IO.Observers
{
    public sealed class DeltaDfsHashObserverTests
    {
        private IHashProvider _hashProvider;
        private IDeltaHashProvider _deltaHashProvider;
        private IDeltaProducersProvider _deltaProducersProvider;
        private IChannelHandlerContext _fakeChannelContext;
        private SyncState _syncState;
        private ILogger _logger;
        private IPeerRepository _peerRepository;

        [SetUp]
        public void Init()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));
            _deltaHashProvider = Substitute.For<IDeltaHashProvider>();
            _deltaProducersProvider = Substitute.For<IDeltaProducersProvider>();
            _fakeChannelContext = Substitute.For<IChannelHandlerContext>();
            _syncState = new SyncState { IsSynchronized = true };
            _logger = Substitute.For<ILogger>();
            _peerRepository = Substitute.For<IPeerRepository>();
        }

        [Test]
        public void HandleBroadcast_Should_Cast_Hashes_To_Multihash_And_Try_Update()
        {
            var newHash = _hashProvider.ComputeUtf8MultiHash("newHash").ToCid();
            var prevHash = _hashProvider.ComputeUtf8MultiHash("prevHash").ToCid();
            var receivedMessage = PrepareReceivedMessage(newHash.ToArray(), prevHash.ToArray());

            var multiAddress = new MultiAddress(receivedMessage.Payload.Address);
            _deltaProducersProvider.GetDeltaProducersFromPreviousDelta(prevHash).Returns(new[] { multiAddress.GetPublicKey() });
            _peerRepository.GetPoaPeersByPublicKey(multiAddress.GetPublicKey()).Returns(new List<Peer>() { new Peer() });
            var deltaDfsHashObserver = new DeltaDfsHashObserver(_deltaHashProvider, _deltaProducersProvider, _syncState, _peerRepository, _logger);

            deltaDfsHashObserver.HandleBroadcast(receivedMessage);

            _deltaHashProvider.Received(1).TryUpdateLatestHash(prevHash, newHash);
        }

        [Test]
        public void HandleBroadcast_Should_Not_Try_Update_Invalid_Hash()
        {
            var invalidNewHash = Encoding.UTF8.GetBytes("invalid hash");
            var prevHash = _hashProvider.ComputeUtf8MultiHash("prevHash").ToCid();
            var receivedMessage = PrepareReceivedMessage(invalidNewHash, prevHash.ToArray());

            var deltaDfsHashObserver = new DeltaDfsHashObserver(_deltaHashProvider, _deltaProducersProvider, _syncState, _peerRepository, _logger);

            deltaDfsHashObserver.HandleBroadcast(receivedMessage);

            _deltaHashProvider.DidNotReceiveWithAnyArgs().TryUpdateLatestHash(default, default);
        }

        [Test]
        public void HandleBroadcast_Should_Not_Try_Update_Invalid_Peer()
        {
            var newHash = _hashProvider.ComputeUtf8MultiHash("newHash").ToCid();
            var prevHash = _hashProvider.ComputeUtf8MultiHash("prevHash").ToCid();
            var receivedMessage = PrepareReceivedMessage(newHash.ToArray(), prevHash.ToArray());

            var multiAddress = new MultiAddress(receivedMessage.Payload.Address);
            _peerRepository.GetPoaPeersByPublicKey(multiAddress.GetPublicKey()).Returns(new List<Peer>());
            var deltaDfsHashObserver = new DeltaDfsHashObserver(_deltaHashProvider, _deltaProducersProvider, _syncState, _peerRepository, _logger);

            deltaDfsHashObserver.HandleBroadcast(receivedMessage);

            _deltaHashProvider.Received(0).TryUpdateLatestHash(prevHash, newHash);
            _logger.Received(1).Error(Arg.Any<string>());
        }

        private IObserverDto<ProtocolMessage> PrepareReceivedMessage(byte[] newHash, byte[] prevHash)
        {
            var message = new DeltaDfsHashBroadcast
            {
                DeltaDfsHash = newHash.ToByteString(),
                PreviousDeltaDfsHash = prevHash.ToByteString()
            };

            var receivedMessage = new ObserverDto(_fakeChannelContext,
                message.ToProtocolMessage(MultiAddressHelper.GetAddress()));
            return receivedMessage;
        }
    }
}
