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
using Catalyst.Abstractions.P2P;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.Rpc.IO.Observers;
using DotNetty.Transport.Channels;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.Rpc.IO.Observers
{
    public sealed class GetMempoolResponseObserverTests
    {
        [Fact]
        public void Null_Mempool_Throws_Exception()
        {
            var channelHandlerContext = Substitute.For<IChannelHandlerContext>();
            var senderPeerIdentifier = Substitute.For<IPeerIdentifier>();
            var correlationId = CorrelationId.GenerateCorrelationId();

            var logger = Substitute.For<ILogger>();
            var getMempoolResponseObserver = new GetMempoolResponseObserver(logger);

            Assert.Throws<ArgumentNullException>(() => getMempoolResponseObserver
               .HandleResponseObserver(null, channelHandlerContext,
                    senderPeerIdentifier, correlationId));
        }
    }
}
