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

using System;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Transaction;
using DotNetty.Buffers;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.P2P
{
    public sealed class P2PMessageFactoryTests
    {
        private readonly IReputableCache _subbedReputationCache;
        public P2PMessageFactoryTests() { _subbedReputationCache = Substitute.For<IReputableCache>(); }
        
        [Fact]
        public void CanProduceAValidPingRequestMessage()
        {
            var pingRequestDatagram = new P2PMessageFactory(_subbedReputationCache).GetMessageInDatagramEnvelope(new MessageDto( 
                new PingRequest(),
                MessageTypes.Ask,
                PeerIdentifierHelper.GetPeerIdentifier("im_a_recipient"),
                PeerIdentifierHelper.GetPeerIdentifier("im_a_sender")
            ));

            pingRequestDatagram.Should().BeAssignableTo(typeof(IByteBufferHolder));
            pingRequestDatagram.Content.Should().BeAssignableTo(typeof(IByteBuffer));
        }
        
        [Fact]
        public void CanProduceAValidPingResponseMessage()
        {
            var pingResponseDatagram = new P2PMessageFactory(_subbedReputationCache).GetMessageInDatagramEnvelope(new MessageDto(
                    new PingResponse(),
                    MessageTypes.Tell,
                    PeerIdentifierHelper.GetPeerIdentifier("im_a_recipient"),
                    PeerIdentifierHelper.GetPeerIdentifier("im_a_sender")
                ),
                Guid.NewGuid()
            );

            pingResponseDatagram.Should().BeAssignableTo(typeof(IByteBufferHolder));
            pingResponseDatagram.Content.Should().BeAssignableTo(typeof(IByteBuffer));
        }
        
        [Fact]
        public void CanProduceAValidTransactionMessage()
        {
            var transactionDatagram = new P2PMessageFactory(_subbedReputationCache).GetMessageInDatagramEnvelope(new MessageDto(
                new TransactionBroadcast(),
                MessageTypes.Ask,
                PeerIdentifierHelper.GetPeerIdentifier("im_a_recipient"),
                PeerIdentifierHelper.GetPeerIdentifier("im_a_sender")
            ));

            transactionDatagram.Should().BeAssignableTo(typeof(IByteBufferHolder));
            transactionDatagram.Content.Should().BeAssignableTo(typeof(IByteBuffer));
        }
    }
}
