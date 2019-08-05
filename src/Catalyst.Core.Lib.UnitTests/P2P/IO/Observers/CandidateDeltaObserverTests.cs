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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Modules.Consensus.Deltas;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.Util;
using Catalyst.Core.Lib.P2P.IO.Observers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Deltas;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Multiformats.Hash.Algorithms;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Lib.UnitTests.P2P.IO.Observers
{
    public class CandidateDeltaObserverTests
    {
        private readonly IDeltaVoter _deltaVoter;
        private readonly IChannelHandlerContext _fakeChannelContext;
        private readonly ILogger _logger;
        private readonly IMultihashAlgorithm _multihashAlgorithm;

        public CandidateDeltaObserverTests()
        {
            _multihashAlgorithm = new BLAKE2B_128();
            _deltaVoter = Substitute.For<IDeltaVoter>();
            _fakeChannelContext = Substitute.For<IChannelHandlerContext>();
            _logger = Substitute.For<ILogger>();
        }

        [Fact]
        public void HandleBroadcast_Should_Cast_Hashes_To_Multihash_And_Send_To_Voter()
        {
            var newHash = Encoding.UTF8.GetBytes("newHash").ComputeMultihash(_multihashAlgorithm);
            var prevHash = Encoding.UTF8.GetBytes("prevHash").ComputeMultihash(_multihashAlgorithm);

            var producerId = PeerIdHelper.GetPeerId("candidate delta producer");
            var receivedMessage = PrepareReceivedMessage(newHash.ToBytes(), prevHash.ToBytes(), producerId);

            var candidateDeltaObserver = new CandidateDeltaObserver(_deltaVoter, _logger);

            candidateDeltaObserver.HandleBroadcast(receivedMessage);
            
            _deltaVoter.Received(1).OnNext(Arg.Is<CandidateDeltaBroadcast>(c =>
                c.Hash.SequenceEqual(newHash.ToBytes().ToByteString())
             && c.PreviousDeltaDfsHash.Equals(prevHash.ToBytes().ToByteString())
             && c.ProducerId.Equals(producerId)));
        }

        [Fact]
        public void HandleBroadcast_Should_Not_Try_Forwarding_Invalid_Hash()
        {
            var invalidNewHash = Encoding.UTF8.GetBytes("invalid hash");
            var prevHash = Encoding.UTF8.GetBytes("prevHash").ComputeMultihash(_multihashAlgorithm);
            var receivedMessage = PrepareReceivedMessage(invalidNewHash, prevHash, PeerIdHelper.GetPeerId("candidate delta producer"));

            var deltaDfsHashObserver = new CandidateDeltaObserver(_deltaVoter, _logger);

            deltaDfsHashObserver.HandleBroadcast(receivedMessage);

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
