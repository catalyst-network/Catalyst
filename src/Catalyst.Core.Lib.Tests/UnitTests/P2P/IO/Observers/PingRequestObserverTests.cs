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
using Catalyst.Core.Lib.P2P.IO.Observers;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using Catalyst.Abstractions.P2P;
using MultiFormats;

namespace Catalyst.Core.Lib.Tests.UnitTests.P2P.IO.Observers
{
    public sealed class PingRequestObserverTests : IDisposable
    {
        private readonly TestScheduler _testScheduler;
        private readonly ILogger _subbedLogger;
        private readonly PingRequestObserver _pingRequestObserver;
        private readonly IPeerClient _peerClient;

        public PingRequestObserverTests()
        {
            _peerClient = Substitute.For<IPeerClient>();
            _testScheduler = new TestScheduler();
            _subbedLogger = Substitute.For<ILogger>();
            var peerSettings = MultiAddressHelper.GetAddress("sender").ToSubstitutedPeerSettings();
            _pingRequestObserver = new PingRequestObserver(peerSettings, _peerClient, _subbedLogger);
        }

        [Test]
        public async Task Can_Process_PingRequest_Correctly()
        {
            var pingRequestMessage = new PingRequest();
            
            var channeledAny = pingRequestMessage.ToProtocolMessage(MultiAddressHelper.GetAddress(), CorrelationId.GenerateCorrelationId());
            var observableStream = new[] {channeledAny}.ToObservable(_testScheduler);
            
            _pingRequestObserver.StartObserving(observableStream);

            _testScheduler.Start();

            var response = new PingResponse().ToProtocolMessage(MultiAddressHelper.GetAddress(), CorrelationId.GenerateCorrelationId());

            await _peerClient.ReceivedWithAnyArgs(1).SendMessageAsync(response, Arg.Any<MultiAddress>()).ConfigureAwait(false);

            _subbedLogger.ReceivedWithAnyArgs(1);
        }
        
        public void Dispose()
        {
            _pingRequestObserver?.Dispose();
        }
    }
}
