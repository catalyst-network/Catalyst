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
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.P2P.ReputationSystem;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.UnitTests.IO.Messaging;
using Catalyst.Common.UnitTests.IO.Messaging.Correlation;
using Catalyst.Node.Core.P2P.IO.Messaging.Correlation;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTests.P2P.IO.Messaging.Correlation
{
    public sealed class PeerMessageCorrelationManagerTests : MessageCorrelationManagerTests<IPeerMessageCorrelationManager>
    {
        private readonly Dictionary<IPeerIdentifier, long> _reputationByPeerIdentifier;

        public PeerMessageCorrelationManagerTests(ITestOutputHelper output) : base(output)
        {
            var subbedRepManager = Substitute.For<IReputationManager>();
            
            CorrelationManager = new PeerMessageCorrelationManager(subbedRepManager,
                Cache,
                SubbedLogger,
                ChangeTokenProvider
            );

            _reputationByPeerIdentifier = PeerIds.ToDictionary(p => p, p => (long) 0); //smells funky
            
            PendingRequests = PeerIds.Select((p, i) => new CorrelatableMessage<ProtocolMessage>
            {
                Content = new PingRequest().ToProtocolMessage(SenderPeerId, CorrelationId.GenerateCorrelationId()),
                Recipient = p,
                SentAt = DateTimeOffset.MinValue.Add(TimeSpan.FromMilliseconds(100 * i))
            }).ToList();
            
            foreach (var correlatableMessage in PendingRequests)
            {
                CorrelationManager.AddPendingRequest(correlatableMessage);
            }
            
            CorrelationManager.ReputationEventStream.Subscribe(change =>
            {
                if (!_reputationByPeerIdentifier.ContainsKey(change.PeerIdentifier))
                {
                    return;
                }
                
                _reputationByPeerIdentifier[change.PeerIdentifier] += change.ReputationEvent.Amount;
            });
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
                    Content = new PingRequest().ToProtocolMessage(senderPeerId, c.CorrelationId),
                    Recipient = c.PeerIdentifier,
                    SentAt = DateTimeOffset.MinValue
                }).ToList();

            var responses = requests.Select(r =>
                new PingResponse().ToProtocolMessage(r.Recipient.PeerId, r.Content.CorrelationId.ToCorrelationId()));

            var evictionObserver = Substitute.For<IObserver<IPeerReputationChange>>();
            
            using (CorrelationManager.ReputationEventStream.SubscribeOn(ImmediateScheduler.Instance)
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

                evictionObserver.Received(requestCount).OnNext(Arg.Is<IPeerReputationChange>(r => r.ReputationEvent.Name == ReputationEvents.NoResponseReceived.Name));
                evictionObserver.Received(requestCount).OnNext(Arg.Is<IPeerReputationChange>(r => r.ReputationEvent.Name == ReputationEvents.UnCorrelatableMessage.Name));
            }
        }
        
        [Fact]
        public void TryMatchResponseAsync_when_matching_should_increase_reputation()
        {
            var reputationBefore = _reputationByPeerIdentifier[PeerIds[1]];

            var responseMatchingIndex1 = new PingResponse().ToProtocolMessage(
                PeerIds[1].PeerId,
                PendingRequests[1].Content.CorrelationId.ToCorrelationId());

            var request = CorrelationManager.TryMatchResponse(responseMatchingIndex1);
            request.Should().BeTrue();
            
            var reputationAfter = _reputationByPeerIdentifier[PeerIds[1]];
            reputationAfter.Should().BeGreaterThan(reputationBefore);

            _reputationByPeerIdentifier.Where(r => !r.Key.Equals(PeerIds[1]))
               .Select(r => r.Value).Should().AllBeEquivalentTo(0);
        }

        [Fact]
        public void UncorrelatedMessage_should_decrease_reputation()
        {
            var reputationBefore = _reputationByPeerIdentifier[PeerIds[1]];
            var responseMatchingIndex1 = new PingResponse().ToProtocolMessage(
                PeerIds[1].PeerId,
                CorrelationId.GenerateCorrelationId());

            var request = CorrelationManager.TryMatchResponse(responseMatchingIndex1);
            var reputationAfter = _reputationByPeerIdentifier[PeerIds[1]];
            reputationAfter.Should().BeLessThan(reputationBefore);
        }
    }
}
