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
using Catalyst.Abstractions.P2P.IO.Messaging.Dto;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.P2P.IO.Observers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.P2P.IO.Observers
{
    public sealed class GetNeighbourResponseObserverTests : IDisposable
    {
        private readonly IChannelHandlerContext _fakeContext;
        private readonly GetNeighbourResponseObserver _observer;

        public GetNeighbourResponseObserverTests()
        {
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _observer = new GetNeighbourResponseObserver(Substitute.For<ILogger>());
        }

        [Fact]
        public void Observer_Can_Process_GetNeighbourResponse_Correctly()
        {
            var testScheduler = new TestScheduler();

            var peers = new[]
            {
                PeerIdHelper.GetPeerId(),
                PeerIdHelper.GetPeerId(),
                PeerIdHelper.GetPeerId()
            };

            var peerNeighborsResponse = new PeerNeighborsResponse
            {
                Peers =
                {
                    peers
                }
            };
            var protocolMessage =
                peerNeighborsResponse.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender").PeerId,
                    CorrelationId.GenerateCorrelationId());

            var peerNeighborsResponseObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, testScheduler,
                protocolMessage);

            _observer.StartObserving(messageStream);

            using (_observer.MessageStream.Subscribe(peerNeighborsResponseObserver.OnNext))
            {
                testScheduler.Start();

                peerNeighborsResponseObserver.Received(1)
                   .OnNext(Arg.Is<IPeerClientMessageDto>(p => test(p.Message, peers[0])));
                peerNeighborsResponseObserver.Received(1)
                   .OnNext(Arg.Is<IPeerClientMessageDto>(p => test(p.Message, peers[1])));
                peerNeighborsResponseObserver.Received(1)
                   .OnNext(Arg.Is<IPeerClientMessageDto>(p => test(p.Message, peers[2])));
            }
        }

        private bool test(IMessage msg, PeerId peerId)
        {
            var x = (PeerNeighborsResponse) msg;
            return x.Peers.Contains(peerId);
        }

        public void Dispose() { _observer?.Dispose(); }
    }
}
