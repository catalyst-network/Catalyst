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
using System.Text;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Modules.Consensus.IO.Observers;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using MultiFormats;
using MultiFormats.Registry;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using Google.Protobuf;
using System.Collections.Generic;
using Catalyst.Core.Lib.P2P.Models;

namespace Catalyst.Core.Modules.Consensus.Tests.UnitTests.IO.Observers
{
    public sealed class FavouriteDeltaObserverTests
    {
        private IDeltaElector _deltaElector;
        private IChannelHandlerContext _fakeChannelContext;
        private MultiAddress _voterId;
        private MultiAddress _producerId;
        private FavouriteDeltaObserver _favouriteDeltaObserver;
        private byte[] _newHash;
        private byte[] _prevHash;

        [SetUp]
        public void Init()
        {
            var hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));
            _deltaElector = Substitute.For<IDeltaElector>();
            _fakeChannelContext = Substitute.For<IChannelHandlerContext>();
            var logger = Substitute.For<ILogger>();
            _voterId = MultiAddressHelper.GetAddress("favourite delta voter");
            _producerId = MultiAddressHelper.GetAddress("candidate delta producer");

            var peerRepository = Substitute.For<IPeerRepository>();
            peerRepository.GetPoaPeersByPublicKey(Arg.Any<string>()).Returns(new List<Peer> { new Peer() });

            _favouriteDeltaObserver = new FavouriteDeltaObserver(_deltaElector, peerRepository, hashProvider, logger);
            _newHash = MultiBase.Decode(hashProvider.ComputeUtf8MultiHash("newHash").ToCid());
            _prevHash = MultiBase.Decode(hashProvider.ComputeUtf8MultiHash("prevHash").ToCid());
        }

        [Test]
        public void HandleBroadcast_Should_Cast_Hashes_To_Multihash_And_Send_To_Voter()
        {
            var receivedMessage = PrepareReceivedMessage(_newHash, _prevHash, _producerId, _voterId);

            _favouriteDeltaObserver.HandleBroadcast(receivedMessage);

            _deltaElector.Received(1).OnNext(Arg.Is<FavouriteDeltaBroadcast>(c =>
                c.Candidate.Hash.SequenceEqual(_newHash.ToArray().ToByteString())
             && c.Candidate.PreviousDeltaDfsHash.Equals(_prevHash.ToArray().ToByteString())
             && c.Candidate.Producer == _producerId.GetKvmAddressByteString()));
        }

        [Test]
        public void HandleBroadcast_Should_Not_Try_Forwarding_Invalid_Hash()
        {
            var invalidNewHash = Encoding.UTF8.GetBytes("invalid hash");

            var receivedMessage = PrepareReceivedMessage(invalidNewHash, _prevHash, _producerId, _voterId);

            _favouriteDeltaObserver.HandleBroadcast(receivedMessage);

            _deltaElector.DidNotReceiveWithAnyArgs().OnNext(default);
        }

        [Test]
        public void HandleBroadcast_Should_Not_Try_Forwarding_Invalid_PreviousHash()
        {
            var invalidPrevHash = Encoding.UTF8.GetBytes("invalid previous hash");

            var receivedMessage = PrepareReceivedMessage(_newHash, invalidPrevHash, _producerId, _voterId);

            _favouriteDeltaObserver.HandleBroadcast(receivedMessage);

            _deltaElector.DidNotReceiveWithAnyArgs().OnNext(default);
        }

        private ProtocolMessage PrepareReceivedMessage(byte[] newHash,
            byte[] prevHash,
            MultiAddress producerId,
            MultiAddress voterId)
        {
            var candidate = new CandidateDeltaBroadcast
            {
                Hash = newHash.ToByteString(),
                PreviousDeltaDfsHash = prevHash.ToByteString(),
                Producer = producerId.GetKvmAddressByteString()
            };

            var favouriteDeltaBroadcast = new FavouriteDeltaBroadcast
            {
                Candidate = candidate,
                Voter = voterId.GetKvmAddressByteString()
            };

            return favouriteDeltaBroadcast.ToProtocolMessage(MultiAddressHelper.GetAddress());
        }
    }
}
