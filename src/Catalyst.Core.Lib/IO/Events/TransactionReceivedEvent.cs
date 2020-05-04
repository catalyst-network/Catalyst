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
using Catalyst.Abstractions.IO.Events;
using Catalyst.Abstractions.Mempool;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Abstractions.Validators;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Transaction;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using MultiFormats;
using Nethermind.Core.Crypto;
using Newtonsoft.Json;
using Serilog;

namespace Catalyst.Core.Lib.IO.Events
{
    public class TransactionReceivedEvent : ITransactionReceivedEvent
    {
        private readonly ITransactionValidator _validator;
        private readonly ILogger _logger;
        private readonly IMempool<PublicEntryDao> _mempool;
        private readonly IBroadcastManager _broadcastManager;
        private readonly IMapperProvider _mapperProvider;

        public TransactionReceivedEvent(ITransactionValidator validator,
            IMempool<PublicEntryDao> mempool,
            IBroadcastManager broadcastManager,
            IMapperProvider mapperProvider,
            ILogger logger)
        {
            _mapperProvider = mapperProvider;
            _broadcastManager = broadcastManager;
            _mempool = mempool;
            _validator = validator;
            _logger = logger;
        }

        public ResponseCode OnTransactionReceived(ProtocolMessage protocolMessage)
        {
            var transactionBroadcast = protocolMessage.FromProtocolMessage<TransactionBroadcast>();
            PublicEntry publicEntry = transactionBroadcast.PublicEntry;
            if (publicEntry.SenderAddress.Length == 32)
            {
                var transactionValid = _validator.ValidateTransaction(publicEntry);
                if (!transactionValid)
                {
                    return ResponseCode.Error;
                }
                
                byte[] kvmAddressBytes = Keccak.Compute(publicEntry.SenderAddress.ToByteArray()).Bytes.AsSpan(12).ToArray();
                string hex = kvmAddressBytes.ToHexString() ?? throw new ArgumentNullException("kvmAddressBytes.ToHexString()");
                publicEntry.SenderAddress = kvmAddressBytes.ToByteString();
                
                if (publicEntry.ReceiverAddress.Length == 1)
                {
                    publicEntry.ReceiverAddress = ByteString.Empty;
                }
            }

            var transactionDao = transactionBroadcast.PublicEntry.ToDao<PublicEntry, PublicEntryDao>(_mapperProvider);

            _logger.Verbose("Adding transaction {id} to mempool", transactionDao.Id);

            if (_mempool.Service.TryReadItem(transactionDao.Id))
            {
                _logger.Information("Transaction {id} already exists in mempool", transactionDao.Id);
                return ResponseCode.Exists;
            }

            _mempool.Service.CreateItem(transactionDao);

            _logger.Information("Broadcasting {signature} transaction", protocolMessage);
            _broadcastManager.BroadcastAsync(protocolMessage).ConfigureAwait(false);

            return ResponseCode.Successful;
        }
    }
}
