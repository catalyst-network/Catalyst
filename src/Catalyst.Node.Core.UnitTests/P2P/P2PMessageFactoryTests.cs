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
using Catalyst.Node.Common.P2P;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Node.Core.UnitTest.TestUtils;
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
            var pingRequestDatagram = P2PMessageFactory<PingRequest>.GetMessage(
                new MessageDto<PingRequest>(
                    P2PMessageType.PingRequest,
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
            var pingResponseDatagram = P2PMessageFactory<PingResponse>.GetMessage(
                new MessageDto<PingResponse>(
                    P2PMessageType.PingResponse,
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
            var transactionDatagram = P2PMessageFactory<Transaction>.GetMessage(
                new MessageDto<Transaction>(
                    P2PMessageType.PingResponse,
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
