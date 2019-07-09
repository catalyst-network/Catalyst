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
using Catalyst.Node.Core.P2P.IO.Messaging.Dto;
using Catalyst.Node.Core.P2P.IO.Observers;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.P2P.IO.Observers
{
    public sealed class GetNeighbourResponseObserverTests
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
            var response = new DtoFactory().GetDto(new PeerNeighborsResponse
                {
                    Peers =
                    {
                        PeerIdHelper.GetPeerId(),
                        PeerIdHelper.GetPeerId(),
                        PeerIdHelper.GetPeerId()
                    }
                },
                PeerIdentifierHelper.GetPeerIdentifier("sender"),
                PeerIdentifierHelper.GetPeerIdentifier("recipient"),
                CorrelationId.GenerateCorrelationId()
            );
            
            var peerNeighborsResponseObserver = Substitute.For<IObserver<IPeerClientMessageDto<PeerNeighborsResponse>>>();

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext,
                response.Content.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender").PeerId,
                    response.CorrelationId));
                
            _observer.StartObserving(messageStream);
            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync();

            using (_observer.PeerNeighborsResponseStream.SubscribeOn(ImmediateScheduler.Instance)
               .Subscribe(peerNeighborsResponseObserver.OnNext))
            {
                await TaskHelper.WaitForAsync(() => peerNeighborsResponseObserver.ReceivedCalls().Any(),
                    TimeSpan.FromMilliseconds(1000));
                peerNeighborsResponseObserver.Received(1).OnNext(Arg.Any<IPeerClientMessageDto<PeerNeighborsResponse>>());
            }
        }
    }
}
