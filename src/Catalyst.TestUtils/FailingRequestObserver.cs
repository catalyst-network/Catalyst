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
using System.Threading;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Peer;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.TestUtils
{
    public sealed class FailingRequestObserver :
        RequestObserverBase<PeerNeighborsRequest, PeerNeighborsResponse>,
        IP2PMessageObserver
    {
        private int _counter;
        public int Counter => _counter;

        public FailingRequestObserver(ILogger logger, PeerId peerId) : base(logger, peerId) { }

        protected override PeerNeighborsResponse HandleRequest(PeerNeighborsRequest messageDto,
            IChannelHandlerContext channelHandlerContext,
            PeerId senderPeerId,
            ICorrelationId correlationId)
        {
            var count = Interlocked.Increment(ref _counter);
            if (count % 2 == 0)
            {
                throw new ArgumentException("something went wrong handling the request");
            }

            return new PeerNeighborsResponse {Peers = {PeerIdHelper.GetPeerId()}};
        }
    }
}
