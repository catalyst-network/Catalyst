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
using Catalyst.Abstractions.Mempool;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.Mempool.Documents;
using Catalyst.Protocol.Interfaces.Validators;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Transaction;
using Serilog;

namespace Catalyst.Core.IO.Events
{
    public class TransactionReceivedEvent : ITransactionReceivedEvent
    {
        private readonly ITransactionValidator _validator;
        private readonly ILogger _logger;
        private readonly IMempool<MempoolDocument> _mempool;
        private readonly IPeerIdentifier _peerIdentifier;
        private readonly IBroadcastManager _broadcastManager;

        public TransactionReceivedEvent(ITransactionValidator validator, IMempool<MempoolDocument> mempool, IBroadcastManager broadcastManager, IPeerIdentifier peerIdentifier, ILogger logger)
        {
            _broadcastManager = broadcastManager;
            _peerIdentifier = peerIdentifier;
            _mempool = mempool;
            _validator = validator;
            _logger = logger;
        }

        public ResponseCode OnTransactionReceived(TransactionBroadcast transaction)
        {
            var transactionValid = _validator.ValidateTransaction(transaction);
            if (!transactionValid)
            {
                return ResponseCode.Error;
            }

            var transactionSignature = transaction.Signature;
            _logger.Verbose("Adding transaction {signature} to mempool", transactionSignature);

            // https://github.com/catalyst-network/Catalyst.Node/issues/910 - should we fail or succeed if we already have the transaction in the ledger?
            if (_mempool.Repository.TryReadItem(transactionSignature))
            {
                _logger.Information("Transaction {signature} already exists in mempool", transactionSignature);
                return ResponseCode.Error;
            }

            _mempool.Repository.CreateItem(transaction);

            _logger.Information("Broadcasting {signature} transaction", transactionSignature);
            var transactionToBroadcast = transaction.ToProtocolMessage(_peerIdentifier.PeerId,
                CorrelationId.GenerateCorrelationId());
            _broadcastManager.BroadcastAsync(transactionToBroadcast);
            return ResponseCode.Successful;
        }
    }
}
