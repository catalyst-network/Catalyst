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
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Consensus.IO.Observers;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using NSubstitute;
using Serilog;
using TheDotNetLeague.MultiFormats.MultiBase;
using TheDotNetLeague.MultiFormats.MultiHash;
using Xunit;

namespace Catalyst.Core.Modules.Consensus.Tests.UnitTests.IO.Observers
{
    public sealed class FavouriteDeltaObserverTests
    {
        private readonly IDeltaElector _deltaElector;
        private readonly IChannelHandlerContext _fakeChannelContext;
        private readonly PeerId _voterId;
        private readonly PeerId _producerId;
        private readonly FavouriteDeltaObserver _favouriteDeltaObserver;
        private readonly byte[] _newHash;
        private readonly byte[] _prevHash;

        public FavouriteDeltaObserverTests()
        {
            var hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));
            _deltaElector = Substitute.For<IDeltaElector>();
            _fakeChannelContext = Substitute.For<IChannelHandlerContext>();
            var logger = Substitute.For<ILogger>();
            _voterId = PeerIdHelper.GetPeerId("favourite delta voter");
            _producerId = PeerIdHelper.GetPeerId("candidate delta producer");

            _favouriteDeltaObserver = new FavouriteDeltaObserver(_deltaElector, hashProvider, logger);
            _newHash = MultiBase.Decode(CidHelper.CreateCid(hashProvider.ComputeUtf8MultiHash("newHash")));
            _prevHash = MultiBase.Decode(CidHelper.CreateCid(hashProvider.ComputeUtf8MultiHash("prevHash")));
        }

        [Fact]
        public void HandleBroadcast_Should_Cast_Hashes_To_Multihash_And_Send_To_Voter()
        {
            var receivedMessage = PrepareReceivedMessage(_newHash, _prevHash, _producerId, _voterId);

            _favouriteDeltaObserver.HandleBroadcast(receivedMessage);

            _deltaElector.Received(1).OnNext(Arg.Is<FavouriteDeltaBroadcast>(c =>
                c.Candidate.Hash.SequenceEqual(_newHash.ToArray().ToByteString())
             && c.Candidate.PreviousDeltaDfsHash.Equals(_prevHash.ToArray().ToByteString())
             && c.Candidate.ProducerId.Equals(_producerId)));
        }

        [Fact]
        public void HandleBroadcast_Should_Not_Try_Forwarding_Invalid_Hash()
        {
            var invalidNewHash = Encoding.UTF8.GetBytes("invalid hash");

            var receivedMessage = PrepareReceivedMessage(invalidNewHash, _prevHash, _producerId, _voterId);

            _favouriteDeltaObserver.HandleBroadcast(receivedMessage);

            _deltaElector.DidNotReceiveWithAnyArgs().OnNext(default);
        }

        [Fact]
        public void HandleBroadcast_Should_Not_Try_Forwarding_Invalid_PreviousHash()
        {
            var invalidPrevHash = Encoding.UTF8.GetBytes("invalid previous hash");

            var receivedMessage = PrepareReceivedMessage(_newHash, invalidPrevHash, _producerId, _voterId);

            _favouriteDeltaObserver.HandleBroadcast(receivedMessage);

            _deltaElector.DidNotReceiveWithAnyArgs().OnNext(default);
        }

        private IObserverDto<ProtocolMessage> PrepareReceivedMessage(byte[] newHash,
            byte[] prevHash,
            PeerId producerId,
            PeerId voterId)
        {
            var candidate = new CandidateDeltaBroadcast
            {
                Hash = newHash.ToByteString(),
                PreviousDeltaDfsHash = prevHash.ToByteString(),
                ProducerId = producerId
            };

            var favouriteDeltaBroadcast = new FavouriteDeltaBroadcast
            {
                Candidate = candidate,
                VoterId = voterId
            };

            var receivedMessage = new ObserverDto(_fakeChannelContext,
                favouriteDeltaBroadcast.ToProtocolMessage(PeerIdHelper.GetPeerId()));
            return receivedMessage;
        }
    }
}
