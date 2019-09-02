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
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Core.IO.Observers;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Transaction;
using Serilog;

namespace Catalyst.Core.P2P.IO.Observers
{
    public sealed class TransactionBroadcastObserver
        : BroadcastObserverBase<TransactionBroadcast>,
            IP2PMessageObserver
    {
        private readonly ITransactionReceivedEvent _transactionReceivedEvent;

        public TransactionBroadcastObserver(ILogger logger, ITransactionReceivedEvent transactionReceivedEvent)
            : base(logger)
        {
            _transactionReceivedEvent = transactionReceivedEvent;
        }

        public override void HandleBroadcast(IObserverDto<ProtocolMessage> messageDto)
        {
            Logger.Debug("received broadcast");

            var deserialised = messageDto.Payload.FromProtocolMessage<TransactionBroadcast>();
            _transactionReceivedEvent.OnTransactionReceived(deserialised);

            Logger.Debug("transaction signature is {0}", deserialised.Signature);
        }
    }
}
