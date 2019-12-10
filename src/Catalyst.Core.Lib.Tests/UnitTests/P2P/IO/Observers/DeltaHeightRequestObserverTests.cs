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
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.P2P.IO.Observers;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Microsoft.Reactive.Testing;
using MultiFormats;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.Lib.Tests.UnitTests.P2P.IO.Observers
{
    public sealed class DeltaHeightRequestObserverTests : IDisposable
    {
        private readonly TestScheduler _testScheduler;
        private readonly ILogger _subbedLogger;
        private readonly DeltaHeightRequestObserver _deltaHeightRequestObserver;

        public DeltaHeightRequestObserverTests()
        {
            _testScheduler = new TestScheduler();
            _subbedLogger = Substitute.For<ILogger>();
            var peerSettings = PeerIdHelper.GetPeerId("sender").ToSubstitutedPeerSettings();
            _deltaHeightRequestObserver = new DeltaHeightRequestObserver(peerSettings,
                _subbedLogger
            );
        }

        [Fact]
        public async Task Can_Process_DeltaHeightRequest_Correctly()
        {
            var deltaHeightRequestMessage = new LatestDeltaHashRequest();
            
            var fakeContext = Substitute.For<IChannelHandlerContext>();
            var channeledAny = new ObserverDto(fakeContext, deltaHeightRequestMessage.ToProtocolMessage(PeerIdHelper.GetPeerId(), CorrelationId.GenerateCorrelationId()));
            var observableStream = new[] {channeledAny}.ToObservable(_testScheduler);
            
            _deltaHeightRequestObserver.StartObserving(observableStream);

            _testScheduler.Start();

            // @TODO get type from hashing module
            var deltaHash = MultiHash.ComputeHash(new byte[32]);

            await fakeContext.Channel.ReceivedWithAnyArgs(1)
               .WriteAndFlushAsync(new LatestDeltaHashResponse
                {
                    DeltaHash = deltaHash.Digest.ToByteString()
                }.ToProtocolMessage(PeerIdHelper.GetPeerId(), CorrelationId.GenerateCorrelationId())).ConfigureAwait(false);
            
            _subbedLogger.ReceivedWithAnyArgs(1);
        }
        
        public void Dispose()
        {
            _deltaHeightRequestObserver?.Dispose();
        }
    }
}
