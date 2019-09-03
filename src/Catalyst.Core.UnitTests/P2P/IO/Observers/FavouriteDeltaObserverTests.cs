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
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Dto;
using Catalyst.Core.P2P.IO.Observers;
using Catalyst.Core.Util;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Deltas;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Multiformats.Hash.Algorithms;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.P2P.IO.Observers
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
            IMultihashAlgorithm multihashAlgorithm = new BLAKE2B_128();
            _deltaElector = Substitute.For<IDeltaElector>();
            _fakeChannelContext = Substitute.For<IChannelHandlerContext>();
            var logger = Substitute.For<ILogger>();
            _voterId = PeerIdHelper.GetPeerId("favourite delta voter");
            _producerId = PeerIdHelper.GetPeerId("candidate delta producer");

            _favouriteDeltaObserver = new FavouriteDeltaObserver(_deltaElector, logger);
            _newHash = Encoding.UTF8.GetBytes("newHash").ComputeMultihash(multihashAlgorithm).ToBytes();
            _prevHash = Encoding.UTF8.GetBytes("prevHash").ComputeMultihash(multihashAlgorithm).ToBytes();
        }

        [Fact]
        public void HandleBroadcast_Should_Cast_Hashes_To_Multihash_And_Send_To_Voter()
        {
            var receivedMessage = PrepareReceivedMessage(_newHash, _prevHash, _producerId, _voterId);

            _favouriteDeltaObserver.HandleBroadcast(receivedMessage);
            
            _deltaElector.Received(1).OnNext(Arg.Is<FavouriteDeltaBroadcast>(c =>
                c.Candidate.Hash.SequenceEqual(_newHash.ToByteString())
             && c.Candidate.PreviousDeltaDfsHash.Equals(_prevHash.ToByteString())
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

        private IObserverDto<ProtocolMessage> PrepareReceivedMessage(byte[] newHash, byte[] prevHash, PeerId producerId, PeerId voterId)
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
