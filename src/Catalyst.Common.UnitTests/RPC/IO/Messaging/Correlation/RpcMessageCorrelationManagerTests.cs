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
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.RPC.IO.Messaging.Correlation;
using Catalyst.Common.UnitTests.IO.Messaging;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Common.UnitTests.RPC.IO.Messaging.Correlation
{
    public sealed class RpcMessageCorrelationManagerTests : MessageCorrelationManagerTests<IMessageCorrelationManager>, IDisposable
    {
        public RpcMessageCorrelationManagerTests(ITestOutputHelper output) : base(output)
        {
            ChangeTokenProvider.GetChangeToken().Returns(ChangeToken);

            Cache = new MemoryCache(new MemoryCacheOptions());

            CorrelationManager = new RpcMessageCorrelationManager(Cache,
                SubbedLogger,
                ChangeTokenProvider
            );
        }

        [Fact]
        public override async Task RequestStore_Should_Not_Keep_Records_For_Longer_Than_Ttl()
        {
            var senderPeerId = PeerIdHelper.GetPeerId("sender");

            const int requestCount = 3;
            var targetPeerIds = Enumerable.Range(0, requestCount).Select(i =>
                PeerIdentifierHelper.GetPeerIdentifier($"target-{i.ToString()}")).ToList();

            var correlationIds = Enumerable.Range(0, requestCount).Select(i => CorrelationId.GenerateCorrelationId())
               .ToList();

            var requests = correlationIds
               .Zip(targetPeerIds, (c, p) => new
                {
                    CorrelationId = c, PeerIdentifier = p
                })
               .Select(c => new CorrelatableMessage
                {
                    Content = new GetInfoRequest().ToProtocolMessage(senderPeerId, c.CorrelationId),
                    Recipient = c.PeerIdentifier,
                    SentAt = DateTimeOffset.MinValue
                }).ToList();

            var responses = requests.Select(r =>
                new GetInfoResponse().ToProtocolMessage(r.Recipient.PeerId, r.Content.CorrelationId.ToCorrelationId()));

            SubbedLogger.ReceivedWithAnyArgs().Debug("log should be called");
        }
    }
}
