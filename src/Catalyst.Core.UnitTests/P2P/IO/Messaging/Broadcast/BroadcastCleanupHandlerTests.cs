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

using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Handlers;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Protocol.Transaction;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels.Embedded;
using NSubstitute;
using Xunit;

namespace Catalyst.Core.UnitTests.P2P.IO.Messaging.Broadcast
{
    public sealed class BroadcastCleanupHandlerTests
    {
        private readonly IBroadcastManager _broadcastManager;
        private readonly EmbeddedChannel _fakeChannel;

        public BroadcastCleanupHandlerTests()
        {
            _broadcastManager = Substitute.For<IBroadcastManager>();
            var broadcastCleanupHandler = new BroadcastCleanupHandler(_broadcastManager);
            _fakeChannel = new EmbeddedChannel(broadcastCleanupHandler);
        }

        [Fact]
        public void Can_Clean_Up_Broadcast()
        {
            var correlationId = CorrelationId.GenerateCorrelationId();
            var fakeMessage =
                new TransactionBroadcast()
                   .ToProtocolMessage(PeerIdHelper.GetPeerId("Test"), correlationId);
            _fakeChannel.WriteInbound(fakeMessage);
            _broadcastManager.Received(1).RemoveSignedBroadcastMessageData(correlationId);
        }
    }
}
