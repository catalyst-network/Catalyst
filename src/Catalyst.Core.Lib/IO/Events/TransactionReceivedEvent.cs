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
using Catalyst.Abstractions.Validators;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Wire;
using Serilog;

namespace Catalyst.Core.Lib.IO.Events
{
    public class TransactionReceivedEvent : ITransactionReceivedEvent
    {
        private readonly ITransactionValidator _validator;
        private readonly ILogger _logger;
        private readonly IMempool<TransactionBroadcastDao> _mempool;
        private readonly IBroadcastManager _broadcastManager;
        private readonly IPeerSettings _peerSettings;

        public TransactionReceivedEvent(ITransactionValidator validator,
            IMempool<TransactionBroadcastDao> mempool,
            IBroadcastManager broadcastManager,
            IPeerSettings peerSettings,
            ILogger logger)
        {
            _peerSettings = peerSettings;
            _broadcastManager = broadcastManager;
            _mempool = mempool;
            _validator = validator;
            _logger = logger;
        }

        public ResponseCode OnTransactionReceived(ProtocolMessage protocolMessage)
        {
            var transaction = protocolMessage.FromProtocolMessage<TransactionBroadcast>();
            var transactionValid = _validator.ValidateTransaction(transaction, _peerSettings.NetworkType);
            if (!transactionValid)
            {
                return ResponseCode.Error;
            }

            var transactionBroadcastDao = new TransactionBroadcastDao().ToDao(transaction);
            var transactionSignature = transactionBroadcastDao.Signature;
            _logger.Verbose("Adding transaction {signature} to mempool", transactionSignature);

            // https://github.com/catalyst-network/Catalyst.Node/issues/910 - should we fail or succeed if we already have the transaction in the ledger?
            if (_mempool.Repository.TryReadItem(transactionSignature.RawBytes))
            {
                _logger.Information("Transaction {signature} already exists in mempool", transactionSignature);
                return ResponseCode.Error;
            }

            _mempool.Repository.CreateItem(transactionBroadcastDao);

            _logger.Information("Broadcasting {signature} transaction", protocolMessage);
            _broadcastManager.BroadcastAsync(protocolMessage).ConfigureAwait(false);

            return ResponseCode.Successful;
        }
    }
}
