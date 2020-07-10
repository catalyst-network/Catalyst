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
using Catalyst.Core.Lib.Service;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Lib.P2P;
using Microsoft.Reactive.Testing;
using MultiFormats;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Abstractions.Sync;

namespace Catalyst.Core.Lib.Tests.UnitTests.P2P.IO.Observers
{
    public sealed class DeltaHeightRequestObserverTests : IDisposable
    {
        private readonly TestScheduler _testScheduler;
        private readonly ILogger _subbedLogger;
        private readonly DeltaHeightRequestObserver _deltaHeightRequestObserver;
        private readonly IPeerClient _peerClient;

        public DeltaHeightRequestObserverTests()
        {
            _testScheduler = new TestScheduler();
            _subbedLogger = Substitute.For<ILogger>();
            _peerClient = Substitute.For<IPeerClient>();
            var peerSettings = MultiAddressHelper.GetAddress("sender").ToSubstitutedPeerSettings();
            var syncState = new SyncState { IsSynchronized = true };
            _deltaHeightRequestObserver = new DeltaHeightRequestObserver(peerSettings,
                Substitute.For<IDeltaIndexService>(), new TestMapperProvider(), _peerClient, syncState,
                _subbedLogger
            );
        }

        [Test]
        public async Task Can_Process_DeltaHeightRequest_Correctly()
        {
            var deltaHeightRequestMessage = new LatestDeltaHashRequest();
            var channeledAny = deltaHeightRequestMessage.ToProtocolMessage(MultiAddressHelper.GetAddress(),
                    CorrelationId.GenerateCorrelationId());
            var observableStream = new[] { channeledAny }.ToObservable(_testScheduler);

            _deltaHeightRequestObserver.StartObserving(observableStream);

            _testScheduler.Start();

            var hash = MultiHash.ComputeHash(new byte[32]);
            var cid = new Cid { Hash = hash };

            var responder = MultiAddressHelper.GetAddress();
            var protocolMessage = new LatestDeltaHashResponse
            {
                DeltaIndex = new DeltaIndex { Cid = cid.ToArray().ToByteString(), Height = 100 }
            }.ToProtocolMessage(responder, CorrelationId.GenerateCorrelationId());

            await _peerClient.ReceivedWithAnyArgs(1).SendMessageAsync(protocolMessage, responder).ConfigureAwait(false);

            _subbedLogger.ReceivedWithAnyArgs(1);
        }

        public void Dispose() { _deltaHeightRequestObserver?.Dispose(); }
    }
}
