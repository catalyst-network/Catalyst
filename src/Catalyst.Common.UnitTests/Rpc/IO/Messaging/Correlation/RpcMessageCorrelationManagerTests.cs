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
using System.Threading;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Util;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.Rpc.IO.Messaging.Correlation;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Common.UnitTests.Rpc.IO.Messaging.Correlation
{
    public sealed class RpcMessageCorrelationManagerTests
    {
        public RpcMessageCorrelationManagerTests()
        {
            _testScheduler = new TestScheduler();
            _memoryCache = Substitute.For<IMemoryCache>();
            var logger = Substitute.For<ILogger>();
            var changeTokenProvider = Substitute.For<IChangeTokenProvider>();

            var expirationMinutes = 60;
            //var expirationTime = DateTime.Now.AddMinutes(expirationMinutes);
            var expirationToken = new CancellationChangeToken(
                new CancellationTokenSource(TimeSpan.FromMinutes(expirationMinutes + .01)).Token);
            changeTokenProvider.GetChangeToken().Returns(expirationToken);

            _rpcMessageCorrelationManager =
                new RpcMessageCorrelationManager(_memoryCache, logger, changeTokenProvider, _testScheduler);
        }

        private readonly TestScheduler _testScheduler;

        private readonly IMemoryCache _memoryCache;

        private readonly RpcMessageCorrelationManager _rpcMessageCorrelationManager;

        [Fact]
        public void Dispose_Should_Dispose_RpcMessageCorrelationManager() { _rpcMessageCorrelationManager.Dispose(); }

        [Fact]
        public void Message_Eviction_Should_Cause_Eviction_Event()
        {
            var peerIds = new[]
            {
                PeerIdentifierHelper.GetPeerIdentifier("peer1"),
                PeerIdentifierHelper.GetPeerIdentifier("peer2"),
                PeerIdentifierHelper.GetPeerIdentifier("peer3")
            };

            var pendingRequests = peerIds.Select((p, i) => new CorrelatableMessage<ProtocolMessage>
            {
                Content = new VersionRequest().ToProtocolMessage(PeerIdHelper.GetPeerId("sender"),
                    CorrelationId.GenerateCorrelationId()),
                Recipient = p,
                SentAt = DateTimeOffset.MinValue.Add(TimeSpan.FromMilliseconds(100 * i))
            }).ToList();

            pendingRequests.ForEach(pendingRequest =>
            {
                _rpcMessageCorrelationManager.AddPendingRequest(pendingRequest);
            });

            pendingRequests.ForEach(pendingRequest =>
            {
                _rpcMessageCorrelationManager.TryMatchResponse(pendingRequest.Content);
            });

            _rpcMessageCorrelationManager.EvictionEvents.Subscribe(response =>
            {
                var a = 0;
            });

            _testScheduler.Start();

            var b = 0;
        }
    }
}

//_memoryCache.TryGetValue(pendingRequest.Content.CorrelationId, out Arg.Any<object>())
//                  .Returns(ci =>
//                   {
//                       ci[1] = pendingRequest;
//                       return true;
//                   });
