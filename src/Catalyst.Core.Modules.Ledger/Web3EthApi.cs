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
using Autofac.Features.AttributeFilters;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.IO.Events;
using Catalyst.Abstractions.Kvm;
using Catalyst.Abstractions.Ledger;
using Catalyst.Abstractions.Ledger.Models;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Modules.Ledger.Repository;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using Nethermind.Core.Crypto;
using Nethermind.Evm;
using Nethermind.Store;

namespace Catalyst.Core.Modules.Ledger
{
    public class Web3EthApi : IWeb3EthApi, ITransactionReceiptResolver
    {
        private readonly ITransactionReceiptRepository _receipts;
        private readonly ITransactionReceivedEvent _transactionReceived;
        private readonly IHashProvider _hashProvider;
        public const string ComponentName = nameof(Web3EthApi);

        public Web3EthApi(IStateReader stateReader, IDeltaResolver deltaResolver, IDeltaCache deltaCache, IDeltaExecutor executor, [KeyFilter(ComponentName)] IStorageProvider storageProvider, [KeyFilter(ComponentName)] IStateProvider stateProvider, ITransactionReceiptRepository receipts, ITransactionReceivedEvent transactionReceived, IHashProvider hashProvider)
        {
            _receipts = receipts;
            _transactionReceived = transactionReceived ?? throw new ArgumentNullException(nameof(transactionReceived));
            _hashProvider = hashProvider;
            StateReader = stateReader ?? throw new ArgumentNullException(nameof(stateReader));
            DeltaResolver = deltaResolver ?? throw new ArgumentNullException(nameof(deltaResolver));
            DeltaCache = deltaCache ?? throw new ArgumentNullException(nameof(deltaCache));
            Executor = executor ?? throw new ArgumentNullException(nameof(executor));
            StorageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
            StateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
        }

        public IStateReader StateReader { get; }
        public IDeltaResolver DeltaResolver { get; }
        public IDeltaCache DeltaCache { get; }

        public IDeltaExecutor Executor { get; }
        public IStorageProvider StorageProvider { get; }
        public IStateProvider StateProvider { get; }
        public ITransactionReceiptResolver ReceiptResolver => this;

        public Keccak SendTransaction(PublicEntry publicEntry)
        {
            TransactionBroadcast broadcast = new TransactionBroadcast
            {
                PublicEntry = publicEntry
            };

            _transactionReceived.OnTransactionReceived(broadcast.ToProtocolMessage(new PeerId(), new CorrelationId(Guid.NewGuid())));

            return new Keccak(_hashProvider.ComputeMultiHash(broadcast).Digest);
        }

        public TransactionReceipt Find(Keccak hash)
        {
            if (_receipts.TryFind(hash, out TransactionReceipt receipt))
            {
                return receipt;
            }

            throw new Exception("Receipt not found");
        }
    }
}
