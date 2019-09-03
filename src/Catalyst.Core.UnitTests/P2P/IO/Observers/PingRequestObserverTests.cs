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
using System.Reactive.Linq;
using System.Threading.Tasks;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.IO.Messaging.Dto;
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
    public sealed class PingRequestObserverTests : IDisposable
    {
        private readonly TestScheduler _testScheduler;
        private readonly ILogger _subbedLogger;
        private readonly PingRequestObserver _pingRequestObserver;

        public PingRequestObserverTests()
        {
            _testScheduler = new TestScheduler();
            _subbedLogger = Substitute.For<ILogger>();
            _pingRequestObserver = new PingRequestObserver(PeerIdentifierHelper.GetPeerIdentifier("sender"),
                _subbedLogger
            );
        }

        [Fact]
        public async Task Can_Process_PingRequest_Correctly()
        {
            var pingRequestMessage = new PingRequest();
            
            var fakeContext = Substitute.For<IChannelHandlerContext>();
            var channeledAny = new ObserverDto(fakeContext, pingRequestMessage.ToProtocolMessage(PeerIdHelper.GetPeerId(), CorrelationId.GenerateCorrelationId()));
            var observableStream = new[] {channeledAny}.ToObservable(_testScheduler);
            
            _pingRequestObserver.StartObserving(observableStream);

            _testScheduler.Start();

            await fakeContext.Channel.ReceivedWithAnyArgs(1)
               .WriteAndFlushAsync(new PingResponse().ToProtocolMessage(PeerIdHelper.GetPeerId(), CorrelationId.GenerateCorrelationId()));
            
            _subbedLogger.ReceivedWithAnyArgs(1);
        }
        
        public void Dispose()
        {
            _pingRequestObserver?.Dispose();
        }
    }
}
