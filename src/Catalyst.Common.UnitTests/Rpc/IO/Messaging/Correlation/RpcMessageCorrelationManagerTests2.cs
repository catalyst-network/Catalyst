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
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.Rpc.IO.Messaging.Correlation;
using Catalyst.Common.Rpc.IO.Messaging.Correlation;
using Catalyst.Common.UnitTests.IO.Messaging.Correlation;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using NSubstitute;
using Xunit;

namespace Catalyst.Common.UnitTests.Rpc.IO.Messaging.Correlation
{
    public sealed class RpcMessageCorrelationManagerTests2 : MessageCorrelationManagerTests<IRpcMessageCorrelationManager>
    {
        public RpcMessageCorrelationManagerTests2()
        {
            CorrelationManager = new RpcMessageCorrelationManager(Cache,
                SubbedLogger,
                ChangeTokenProvider
            );

            PrepareCacheWithPendingRequests<GetInfoRequest>();
        }

        [Fact]
        public void TryMatchResponseAsync_Should_Match_Existing_Records_With_Matching_Correlation_Id()
        {
            TryMatchResponseAsync_Should_Match_Existing_Records_With_Matching_Correlation_Id<GetInfoResponse>();
        }

        [Fact]
        public void TryMatchResponseAsync_Should_Not_Match_Existing_Records_With_Non_Matching_Correlation_Id()
        {
            TryMatchResponseAsync_Should_Not_Match_Existing_Records_With_Non_Matching_Correlation_Id<GetInfoResponse>();
        }

        protected override async Task CheckCacheEntriesCallback()
        {
            var observer = Substitute.For<IObserver<ICacheEvictionEvent<ProtocolMessage>>>();
            using (CorrelationManager.EvictionEvents.Subscribe(observer))
            {
                var firstCorrelationId = PendingRequests[0].Content.CorrelationId;
                FireEvictionCallBackByCorrelationId(firstCorrelationId);

                await TaskHelper.WaitForAsync(() => observer.ReceivedCalls().Any(), TimeSpan.FromSeconds(2));

                observer.Received(1).OnNext(Arg.Is<ICacheEvictionEvent<ProtocolMessage>>(p =>
                    p.EvictedContent.CorrelationId == firstCorrelationId
                 && p.PeerIdentifier.PeerId.Equals(PendingRequests[0].Content.PeerId)));
            }
        }
    }
}
