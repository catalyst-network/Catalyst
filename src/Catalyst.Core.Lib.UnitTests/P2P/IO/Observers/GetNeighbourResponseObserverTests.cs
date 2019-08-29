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
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Dto;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Core.Lib.P2P.IO.Observers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.Lib.UnitTests.P2P.IO.Observers
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
        public async void Observer_Can_Process_GetNeighbourResponse_Correctly()
        {
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
                peerNeighborsResponse.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender").PeerId, CorrelationId.GenerateCorrelationId());

            var peerNeighborsResponseObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext,
                protocolMessage);
                
            _observer.StartObserving(messageStream);
            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync();

            using (_observer.MessageStream.SubscribeOn(TaskPoolScheduler.Default)
               .Subscribe(peerNeighborsResponseObserver.OnNext))
            {
                await TaskHelper.WaitForAsync(() => peerNeighborsResponseObserver.ReceivedCalls().Any(),
                    TimeSpan.FromMilliseconds(1000));
                
                peerNeighborsResponseObserver.Received(1).OnNext(Arg.Is<IPeerClientMessageDto>(p => test(p.Message, peers[0])));
                peerNeighborsResponseObserver.Received(1).OnNext(Arg.Is<IPeerClientMessageDto>(p => test(p.Message, peers[1])));
                peerNeighborsResponseObserver.Received(1).OnNext(Arg.Is<IPeerClientMessageDto>(p => test(p.Message, peers[2])));
            }
        }

        private bool test(IMessage msg, PeerId peerId)
        {
            var x = (PeerNeighborsResponse) msg;
            return x.Peers.Contains(peerId);
        }

        public void Dispose()
        {
            _observer?.Dispose();
        }
    }
}
