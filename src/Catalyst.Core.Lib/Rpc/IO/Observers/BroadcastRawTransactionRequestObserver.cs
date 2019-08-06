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
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Broadcast;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Observers;
using Catalyst.Common.Modules.Mempool.Models;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Core.Lib.Rpc.IO.Observers
{
    public class BroadcastRawTransactionRequestObserver
        : RequestObserverBase<BroadcastRawTransactionRequest, BroadcastRawTransactionResponse>, IRpcRequestObserver
    {
        private readonly IMempool _mempool;
        private readonly IBroadcastManager _broadcastManager;

        public BroadcastRawTransactionRequestObserver(ILogger logger,
            IPeerIdentifier peerIdentifier,
            IMempool mempool,
            IBroadcastManager broadcastManager)
            : base(logger, peerIdentifier)
        {
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

            if (signatureValid)
            {
                // TODO: Check ledger to see if ledger already contains transaction, if so we need to send Successful/Fail response
                if (_mempool.ContainsDocument(messageDto.Transaction.Signature))
                {
                    return new BroadcastRawTransactionResponse {ResponseCode = responseCode};
                }

                _mempool.SaveMempoolDocument(new MempoolDocument
                {
                    Transaction = messageDto.Transaction
                });

                var transactionToBroadcast = messageDto.Transaction.ToProtocolMessage(PeerIdentifier.PeerId,
                    CorrelationId.GenerateCorrelationId());
                _broadcastManager.BroadcastAsync(transactionToBroadcast);
            }
            else
            {
                responseCode = ResponseCode.Error;
            }

            return new BroadcastRawTransactionResponse {ResponseCode = responseCode};
        }
    }
}
