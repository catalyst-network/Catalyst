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

using Catalyst.Abstractions.IO.Events;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Core.Modules.Rpc.Server.IO.Observers
{
    public class BroadcastRawTransactionRequestObserver
        : RequestObserverBase<BroadcastRawTransactionRequest, BroadcastRawTransactionResponse>, IRpcRequestObserver
    {
        private readonly ITransactionReceivedEvent _transactionReceivedEvent;

        public BroadcastRawTransactionRequestObserver(ILogger logger,
            PeerId peerId,
            ITransactionReceivedEvent transactionReceivedEvent)
            : base(logger, peerId)
        {
            _transactionReceivedEvent = transactionReceivedEvent;
        }

        protected override BroadcastRawTransactionResponse HandleRequest(BroadcastRawTransactionRequest messageDto,
            IChannelHandlerContext channelHandlerContext,
            PeerId senderPeerId,
            ICorrelationId correlationId)
        {
            var responseCode = _transactionReceivedEvent.OnTransactionReceived(messageDto.Transaction);

            return new BroadcastRawTransactionResponse {ResponseCode = responseCode};
        }
    }
}
