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
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Observers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Delta;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Common.UnitTests.IO.Observers
{
    public class ResponseObserverBaseTests
    {
        private sealed class FailingResponseObserver : ResponseObserverBase<GetPeerCountResponse>
        {
            public int Counter;

            public FailingResponseObserver(ILogger logger) : base(logger) { }

            protected override void HandleResponse(GetPeerCountResponse messageDto,
                IChannelHandlerContext channelHandlerContext,
                IPeerIdentifier senderPeerIdentifier,
                ICorrelationId correlationId)
            {
                var count = Interlocked.Increment(ref Counter);
                if (count % 2 == 0)
                {
                    throw new ArgumentException("something went wrong handling the request");
                }
            }
        }

        [Fact]
        public async Task OnNext_Should_Still_Get_Called_After_HandleBroadcast_Failure()
        {
            var candidateDeltaMessages = Enumerable.Range(0, 10)
               .Select(i => new GetPeerCountResponse {PeerCount = i}).ToArray();

            var messageStream = MessageStreamHelper.CreateStreamWithMessages(candidateDeltaMessages);
            using (var observer = new FailingResponseObserver(Substitute.For<ILogger>()))
            {
                observer.StartObserving(messageStream);
                await messageStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync();
                observer.Counter.Should().Be(10);
            }
        }
    }
}
