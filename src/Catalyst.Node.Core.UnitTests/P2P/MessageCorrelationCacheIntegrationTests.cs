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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Node.Core.UnitTest.TestUtils;
using Catalyst.Protocol.IPPN;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Abstractions;
using PendingRequest = Catalyst.Node.Common.P2P.PendingRequest;

namespace Catalyst.Node.Core.UnitTest.P2P
{
    public sealed class MessageCorrelationCacheIntegrationTests : ConfigFileBasedTest
    {
        private readonly ILifetimeScope _scope;

        public MessageCorrelationCacheIntegrationTests(ITestOutputHelper output) : base(output)
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .Build();

            ConfigureContainerBuilder(config);
            var container = ContainerBuilder.Build();
            _scope = container.BeginLifetimeScope(CurrentTestName);
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task RequestStore_should_not_keep_records_for_longer_than_ttl()
        {
            var senderPeerId = PeerIdHelper.GetPeerId("sender");

            var peerIds = Enumerable.Range(0, 3).Select(i =>
                PeerIdentifierHelper.GetPeerIdentifier($"dude-{i}")).ToList();

            var correlationIds = Enumerable.Range(0, 3).Select(i => Guid.NewGuid()).ToList();

            var requests = correlationIds
               .Zip(peerIds, (c, p) => new { CorrelationId = c, PeerIdentifier = p })
               .Select(c => new PendingRequest()
                {
                    Content = new PingRequest().ToAnySigned(senderPeerId, c.CorrelationId),
                    SentTo = c.PeerIdentifier,
                    SentAt = DateTimeOffset.MinValue
                }).ToList();

            var reputations = requests.ToDictionary(r => r.SentTo, r => 0);

            var cache = _scope.Resolve<IMemoryCache>();
            var ttl = TimeSpan.FromMilliseconds(100);
            var pendingRequestCache = new MessageCorrelationCache(cache, ttl);

            pendingRequestCache.PeerRatingChanges
               .Subscribe(c => reputations[c.PeerIdentifier] += c.ReputationChange);

            requests.ForEach(r => pendingRequestCache.AddPendingRequest(r));

            var responseFromDude1 = new PingResponse{}.ToAnySigned(peerIds[1].PeerId, correlationIds[1]);
            var match = pendingRequestCache.TryMatchResponse<PingRequest, PingResponse>(responseFromDude1);
            match.Should().NotBeNull();

            await Task.Delay(ttl.Add(TimeSpan.FromMilliseconds(ttl.TotalMilliseconds * 0.2)));

            correlationIds.Select(c => cache.TryGetValue(c.ToByteString(), out _))
               .Should().AllBeEquivalentTo(false, "entries are removed by matching or expiring");

            reputations.Where(p => !p.Key.Equals(peerIds[1])).Select(p => p.Value < 0)
               .Should().AllBeEquivalentTo(true, "expiring scores negatively");
            reputations[peerIds[1]].Should().BeGreaterThan(0, "matching scores positively");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _scope?.Dispose();
        }
    }
}
