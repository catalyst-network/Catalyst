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
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Dto;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.P2P.IO.Observers;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.P2P.IO.Observers
{
    public sealed class PingResponseObserverTests : IDisposable
    {
        private readonly IChannelHandlerContext _fakeContext;
        private readonly PingResponseObserver _observer;

        public PingResponseObserverTests()
        {
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _observer = new PingResponseObserver(Substitute.For<ILogger>(), Substitute.For<IPeerChallenger>());
        }

        [Fact]
        public void Observer_Can_Process_PingResponse_Correctly()
        {
            var testScheduler = new TestScheduler();

            var pingResponse = new PingResponse();
            var protocolMessage =
                pingResponse.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender").PeerId,
                    CorrelationId.GenerateCorrelationId());

            var pingResponseObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, testScheduler,
                protocolMessage);

            _observer.StartObserving(messageStream);

            using (_observer.MessageStream.Subscribe(pingResponseObserver.OnNext))
            {
                testScheduler.Start();

                pingResponseObserver.Received(1).OnNext(Arg.Any<IPeerClientMessageDto>());
            }
        }

        public void Dispose() { _observer?.Dispose(); }
    }
}
