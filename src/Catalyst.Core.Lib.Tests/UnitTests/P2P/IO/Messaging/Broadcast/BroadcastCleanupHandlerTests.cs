#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Modules.Network.Dotnetty.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Modules.Network.Dotnetty.IO.Handlers;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels.Embedded;
using NSubstitute;
using NUnit.Framework;

namespace Catalyst.Core.Lib.Tests.UnitTests.P2P.IO.Messaging.Broadcast
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

        [Test]
        public void Can_Clean_Up_Broadcast()
        {
            var correlationId = CorrelationId.GenerateCorrelationId();
            var fakeMessage =
                new TransactionBroadcast()
                   .ToProtocolMessage(MultiAddressHelper.GetAddress("Test"), correlationId);
            _fakeChannel.WriteInbound(fakeMessage);
            _broadcastManager.Received(1).RemoveSignedBroadcastMessageData(correlationId);
        }
    }
}
