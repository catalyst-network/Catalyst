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
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.IO.Messaging;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Xunit;

namespace Catalyst.Common.UnitTests.IO.Messaging
{
    public sealed class MessageCorrelationManagerTests
    {
        [Fact]
        public async Task RequestStore_should_not_keep_records_for_longer_than_ttl()
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

            using (var cache = new MemoryCache(new MemoryCacheOptions()))
            {
                var ttl = TimeSpan.FromMilliseconds(100);
                var messageCorrelationCacheManager = new MessageCorrelationManager(cache, ttl);

                // using (messageCorrelationCacheManager.EvictionEvents
                //    .Subscribe(evictionObserver.OnNext))
                // {
                //     requests.ForEach(r => messageCorrelationCacheManager.AddPendingRequest(r));
                //
                //     await Task.Delay(ttl.Add(TimeSpan.FromMilliseconds(ttl.TotalMilliseconds * 0.2)))
                //        .ConfigureAwait(false);
                //     await Task.Yield();
                //
                //     foreach (var response in responses)
                //     {
                //         messageCorrelationCacheManager.TryMatchResponse(response).Should()
                //            .BeFalse("we have passed the TTL so the records should have disappeared");
                //     }
                //
                //     evictionObserver.Received(requestCount).OnNext(Arg.Any<IMessageEvictionEvent>());
                // }
            }
        }
    }
}
