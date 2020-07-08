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
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using MultiFormats;
using Serilog;

namespace Catalyst.Core.Modules.Rpc.Server.IO.Observers
{
    public class BroadcastRawTransactionRequestObserver
        : RequestObserverBase<BroadcastRawTransactionRequest, BroadcastRawTransactionResponse>, IRpcRequestObserver
    {
        private readonly ITransactionReceivedEvent _transactionReceivedEvent;

        public BroadcastRawTransactionRequestObserver(ILogger logger,
            IPeerSettings peerSettings,
            IPeerClient peerClient,
            ITransactionReceivedEvent transactionReceivedEvent)
            : base(logger, peerSettings, peerClient)
        {
            _transactionReceivedEvent = transactionReceivedEvent;
        }

        protected override BroadcastRawTransactionResponse HandleRequest(BroadcastRawTransactionRequest messageDto,
            MultiAddress sender,
            ICorrelationId correlationId)
        {
            var responseCode = _transactionReceivedEvent.OnTransactionReceived(messageDto.Transaction.ToProtocolMessage(sender, correlationId));

            return new BroadcastRawTransactionResponse {ResponseCode = responseCode};
        }
    }
}
