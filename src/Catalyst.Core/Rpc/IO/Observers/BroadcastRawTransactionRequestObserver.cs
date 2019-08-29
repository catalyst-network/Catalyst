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

using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.Mempool;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.IO.Observers;
using Catalyst.Core.Mempool.Documents;
using Catalyst.Core.Mempool.Models;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Core.Rpc.IO.Observers
{
    public class BroadcastRawTransactionRequestObserver
        : RequestObserverBase<BroadcastRawTransactionRequest, BroadcastRawTransactionResponse>, IRpcRequestObserver
    {
        private readonly ILogger _logger;
        private readonly IMempool<MempoolDocument> _mempool;
        private readonly IBroadcastManager _broadcastManager;

        public BroadcastRawTransactionRequestObserver(ILogger logger,
            IPeerIdentifier peerIdentifier,
            IMempool<MempoolDocument> mempool,
            IBroadcastManager broadcastManager)
            : base(logger, peerIdentifier)
        {
            _logger = logger;
            _mempool = mempool;
            _broadcastManager = broadcastManager;
        }

        protected override BroadcastRawTransactionResponse HandleRequest(BroadcastRawTransactionRequest messageDto,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            // TODO: Signature Check
            var signatureValid = true;
            var responseCode = ResponseCode.Successful;

            var transactionSignature = messageDto.Transaction.Signature;
            _logger.Verbose("Adding transaction {signature} to mempool", transactionSignature);
            if (signatureValid)
            {
                // TODO: Check ledger to see if ledger already contains transaction, if so we need to send Successful/Fail response
                if (_mempool.Repository.TryReadItem(transactionSignature))
                {
                    _logger.Information("Transaction {signature} already exists in mempool", transactionSignature);
                    responseCode = ResponseCode.Error;
                    return new BroadcastRawTransactionResponse {ResponseCode = responseCode};
                }

                // _mempool.SaveMempoolItem(new MempoolDocument
                // {
                //     Transaction = messageDto.Transaction
                // });
                
                _mempool.Repository.CreateItem(messageDto.Transaction);

                _logger.Information("Broadcasting {signature} transaction", transactionSignature);
                var transactionToBroadcast = messageDto.Transaction.ToProtocolMessage(PeerIdentifier.PeerId,
                    CorrelationId.GenerateCorrelationId());
                _broadcastManager.BroadcastAsync(transactionToBroadcast);
            }
            else
            {
                _logger.Information(
                    "Transaction {signature} doesn't have a valid signature and was not added to the mempool.",
                    transactionSignature);
                responseCode = ResponseCode.Error;
            }

            return new BroadcastRawTransactionResponse {ResponseCode = responseCode};
        }
    }
}
