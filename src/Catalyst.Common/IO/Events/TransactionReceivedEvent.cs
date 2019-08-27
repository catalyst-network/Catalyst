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

using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Events;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Broadcast;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.Modules.Mempool.Models;
using Catalyst.Protocol.Interfaces.Validators;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Transaction;
using Serilog;

namespace Catalyst.Common.IO.Events
{
    public class TransactionReceivedEvent : ITransactionReceivedEvent
    {
        private readonly ITransactionValidator _validator;
        private readonly ILogger _logger;
        private readonly IMempool _mempool;
        private readonly IPeerIdentifier _peerIdentifier;
        private readonly IBroadcastManager _broadcastManager;

        public TransactionReceivedEvent(ITransactionValidator validator, IMempool mempool, IBroadcastManager broadcastManager, IPeerIdentifier peerIdentifier, ILogger logger)
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

            // TODO: Check ledger to see if ledger already contains transaction, if so we need to send Successful/Fail response
            if (_mempool.ContainsDocument(transactionSignature))
            {
                _logger.Information("Transaction {signature} already exists in mempool", transactionSignature);
                return ResponseCode.Error;
            }

            _mempool.SaveMempoolDocument(new MempoolDocument
            {
                Transaction = transaction
            });

            _logger.Information("Broadcasting {signature} transaction", transactionSignature);
            var transactionToBroadcast = transaction.ToProtocolMessage(_peerIdentifier.PeerId,
                CorrelationId.GenerateCorrelationId());
            _broadcastManager.BroadcastAsync(transactionToBroadcast);
            return ResponseCode.Successful;
        }
    }
}
