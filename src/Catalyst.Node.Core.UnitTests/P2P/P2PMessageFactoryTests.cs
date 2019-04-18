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
using System.Net;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Transaction;
using DotNetty.Buffers;
using FluentAssertions;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.P2P
{
    public sealed class P2PMessageFactoryTests
    {
        [Fact]
        public void CanProduceAValidPingRequestMessage()
        {
            var pingRequestDatagram = new P2PMessageFactory<PingRequest, P2PMessages>().GetMessageInDatagramEnvelope(
                new P2PMessageDto<PingRequest, P2PMessages>(
                    P2PMessages.PingRequest,
                    new PingRequest(), 
                    new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort), 
                    PeerIdentifierHelper.GetPeerIdentifier("Im_A_Frigging_Public_Key")
                )
            );

            pingRequestDatagram.Should().BeAssignableTo(typeof(IByteBufferHolder));
            pingRequestDatagram.Content.Should().BeAssignableTo(typeof(IByteBuffer));
        }
        
        [Fact]
        public void CanProduceAValidPingResponseMessage()
        {
            var pingResponseDatagram = new P2PMessageFactory<PingResponse, P2PMessages>().GetMessageInDatagramEnvelope(
                new P2PMessageDto<PingResponse, P2PMessages>(
                    P2PMessages.PingResponse,
                    new PingResponse(), 
                    new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort), 
                    PeerIdentifierHelper.GetPeerIdentifier("Im_A_Frigging_Public_Key")
                )
            );

            pingResponseDatagram.Should().BeAssignableTo(typeof(IByteBufferHolder));
            pingResponseDatagram.Content.Should().BeAssignableTo(typeof(IByteBuffer));
        }
        
        [Fact]
        public void CanProduceAValidTransactionMessage()
        {
            var transactionDatagram = new P2PMessageFactory<Transaction, P2PMessages>().GetMessageInDatagramEnvelope(
                new P2PMessageDto<Transaction, P2PMessages>(
                    P2PMessages.PingResponse,
                    new Transaction(), 
                    new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort), 
                    PeerIdentifierHelper.GetPeerIdentifier("Im_A_Frigging_Public_Key")
                )
            );

            transactionDatagram.Should().BeAssignableTo(typeof(IByteBufferHolder));
            transactionDatagram.Content.Should().BeAssignableTo(typeof(IByteBuffer));
        }
    }
}
