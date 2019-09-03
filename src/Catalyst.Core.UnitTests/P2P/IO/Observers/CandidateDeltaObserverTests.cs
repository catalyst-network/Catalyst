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
    public sealed class CandidateDeltaObserverTests
    {
        private readonly IDeltaVoter _deltaVoter;
        private readonly IChannelHandlerContext _fakeChannelContext;
        private readonly byte[] _newHash;
        private readonly byte[] _prevHash;
        private readonly PeerId _producerId;
        private readonly CandidateDeltaObserver _candidateDeltaObserver;

        public CandidateDeltaObserverTests()
        {
            var multihashAlgorithm = new BLAKE2B_128();
            _deltaVoter = Substitute.For<IDeltaVoter>();
            _fakeChannelContext = Substitute.For<IChannelHandlerContext>();
            var logger = Substitute.For<ILogger>();
            _newHash = Encoding.UTF8.GetBytes("newHash").ComputeMultihash(multihashAlgorithm).ToBytes();
            _prevHash = Encoding.UTF8.GetBytes("prevHash").ComputeMultihash(multihashAlgorithm).ToBytes();
            _producerId = PeerIdHelper.GetPeerId("candidate delta producer");
            _candidateDeltaObserver = new CandidateDeltaObserver(_deltaVoter, logger);
        }

        [Fact]
        public void HandleBroadcast_Should_Cast_Hashes_To_Multihash_And_Send_To_Voter()
        {
            var receivedMessage = PrepareReceivedMessage(_newHash, _prevHash, _producerId);

            _candidateDeltaObserver.HandleBroadcast(receivedMessage);

            _deltaVoter.Received(1).OnNext(Arg.Is<CandidateDeltaBroadcast>(c =>
                c.Hash.SequenceEqual(_newHash.ToByteString())
             && c.PreviousDeltaDfsHash.Equals(_prevHash.ToByteString())
             && c.ProducerId.Equals(_producerId)));
        }

        [Fact]
        public void HandleBroadcast_Should_Not_Try_Forwarding_Invalid_Hash()
        {
            var invalidNewHash = Encoding.UTF8.GetBytes("invalid hash");
            var receivedMessage = PrepareReceivedMessage(invalidNewHash, _prevHash, _producerId);

            _candidateDeltaObserver.HandleBroadcast(receivedMessage);

            _deltaVoter.DidNotReceiveWithAnyArgs().OnNext(default);
        }

        [Fact]
        public void HandleBroadcast_Should_Not_Try_Forwarding_Invalid_PreviousHash()
        {
            var invalidPreviousHash = Encoding.UTF8.GetBytes("invalid previous hash");
            var receivedMessage = PrepareReceivedMessage(_newHash, invalidPreviousHash, _producerId);

            _candidateDeltaObserver.HandleBroadcast(receivedMessage);

            _deltaVoter.DidNotReceiveWithAnyArgs().OnNext(default);
        }

        private IObserverDto<ProtocolMessage> PrepareReceivedMessage(byte[] newHash, byte[] prevHash, PeerId producerId)
        {
            var message = new CandidateDeltaBroadcast
            {
                Hash = newHash.ToByteString(),
                PreviousDeltaDfsHash = prevHash.ToByteString(),
                ProducerId = producerId
            };

            var receivedMessage = new ObserverDto(_fakeChannelContext,
                message.ToProtocolMessage(PeerIdHelper.GetPeerId()));
            return receivedMessage;
        }
    }
}
