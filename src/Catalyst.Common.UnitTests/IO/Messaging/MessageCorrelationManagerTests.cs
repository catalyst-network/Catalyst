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
using Catalyst.Common.Interfaces.Util;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using Xunit;

namespace Catalyst.Common.UnitTests.IO.Messaging
{
    public sealed class MessageCorrelationManagerTests
    {
        [Fact]
        public async Task RequestStore_Should_Not_Keep_Records_For_Longer_Than_Ttl()
        {
            var senderPeerId = PeerIdHelper.GetPeerId("sender");

            var requestCount = 3;
            var targetPeerIds = Enumerable.Range(0, requestCount).Select(i =>
                PeerIdentifierHelper.GetPeerIdentifier($"target-{i}")).ToList();
            
            var correlationIds = Enumerable.Range(0, requestCount).Select(i => CorrelationId.GenerateCorrelationId()).ToList();

            var requests = correlationIds
               .Zip(targetPeerIds, (c, p) => new
                {
                    CorrelationId = c, PeerIdentifier = p
                })
               .Select(c => new CorrelatableMessage
                {
                    Content = new PingRequest().ToProtocolMessage(senderPeerId, c.CorrelationId),
                    Recipient = c.PeerIdentifier,
                    SentAt = DateTimeOffset.MinValue
                }).ToList();

            var responses = requests.Select(r =>
                new PingResponse().ToProtocolMessage(r.Recipient.PeerId, r.Content.CorrelationId.ToCorrelationId()));

            var evictionObserver = Substitute.For<IObserver<IMessageEvictionEvent>>();

            var changeToken = Substitute.For<IChangeToken>();
            var changeTokenProvider = Substitute.For<IChangeTokenProvider>();
            changeTokenProvider.GetChangeToken().Returns(changeToken);

            using (var cache = new MemoryCache(new MemoryCacheOptions()))
            {
                var messageCorrelationCacheManager = new MessageCorrelationManager(cache, changeTokenProvider);

                using (messageCorrelationCacheManager.EvictionEvents
                   .SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(evictionObserver.OnNext))
                {
                    requests.ForEach(r => messageCorrelationCacheManager.AddPendingRequest(r));

                    changeToken.HasChanged.Returns(true);

                    foreach (var response in responses)
                    {
                        messageCorrelationCacheManager.TryMatchResponse(response).Should()
                           .BeFalse("the changeToken has simulated a TTL expiry");
                    }

                    await TaskHelper.WaitForAsync(() => evictionObserver.ReceivedCalls().Any(),
                        TimeSpan.FromMilliseconds(2000));

                    evictionObserver.Received(requestCount).OnNext(Arg.Any<IMessageEvictionEvent>());
                }
            }
        }
    }
}
