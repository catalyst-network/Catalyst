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
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.Rpc.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.Rpc.IO.Messaging.Correlation;
using Catalyst.Common.UnitTests.IO.Messaging;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Common.UnitTests.Rpc.IO.Messaging.Correlation
{
    public sealed class RpcMessageCorrelationManagerTests : MessageCorrelationManagerTests<IRpcMessageCorrelationManager>, IDisposable
    {
        public RpcMessageCorrelationManagerTests(ITestOutputHelper output) : base(output)
        {
            ChangeTokenProvider.GetChangeToken().Returns(ChangeToken);

            Cache = new MemoryCache(new MemoryCacheOptions());

            CorrelationManager = new RpcMessageCorrelationManager(Cache,
                SubbedLogger,
                ChangeTokenProvider
            );
            
            PendingRequests = PeerIds.Select((p, i) => new CorrelatableMessage<ProtocolMessage>
            {
                Content = new GetInfoRequest().ToProtocolMessage(SenderPeerId, CorrelationId.GenerateCorrelationId()),
                Recipient = p,
                SentAt = DateTimeOffset.MinValue.Add(TimeSpan.FromMilliseconds(100 * i))
            }).ToList();
            
            foreach (var correlatableMessage in PendingRequests)
            {
                CorrelationManager.AddPendingRequest(correlatableMessage);
            }
        }

        [Fact]
        public override async Task RequestStore_Should_Not_Keep_Records_For_Longer_Than_Ttl()
        {
            var senderPeerId = PeerIdHelper.GetPeerId("sender");

            const int requestCount = 3;
            var targetPeerIds = Enumerable.Range(0, requestCount).Select(i =>
                PeerIdentifierHelper.GetPeerIdentifier($"target-{i.ToString()}")).ToList();
            
            var correlationIds = Enumerable.Range(0, requestCount).Select(i => CorrelationId.GenerateCorrelationId()).ToList();

            var requests = correlationIds
               .Zip(targetPeerIds, (c, p) => new
                {
                    CorrelationId = c, PeerIdentifier = p
                })
               .Select(c => new CorrelatableMessage<ProtocolMessage>
                {
                    Content = new GetInfoRequest().ToProtocolMessage(senderPeerId, c.CorrelationId),
                    Recipient = c.PeerIdentifier,
                    SentAt = DateTimeOffset.MinValue
                }).ToList();

            var responses = requests.Select(r =>
                new GetInfoResponse().ToProtocolMessage(r.Recipient.PeerId, r.Content.CorrelationId.ToCorrelationId()));

            var evictionObserver = Substitute.For<IObserver<ICacheEvictionEvent<ProtocolMessage>>>();
            
            using (CorrelationManager.EvictionEvents.SubscribeOn(ImmediateScheduler.Instance)
               .Subscribe(evictionObserver.OnNext))
            {
                requests.ForEach(r => CorrelationManager.AddPendingRequest(r));

                ChangeToken.HasChanged.Returns(true);

                foreach (var response in responses)
                {
                    CorrelationManager.TryMatchResponse(response).Should()
                       .BeFalse("the changeToken has simulated a TTL expiry");
                }

                await TaskHelper.WaitForAsync(() => evictionObserver.ReceivedCalls().Any(),
                    TimeSpan.FromMilliseconds(1000));
                
                evictionObserver.Received(requestCount).OnNext(Arg.Any<ICacheEvictionEvent<ProtocolMessage>>());
            }
        }
    }
}
