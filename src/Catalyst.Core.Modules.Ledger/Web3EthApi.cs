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
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.IO.Events;
using Catalyst.Abstractions.Kvm;
using Catalyst.Abstractions.Ledger;
using Catalyst.Abstractions.Ledger.Models;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Repository;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Modules.Ledger.Repository;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Lib.P2P;
using MultiFormats;
using Nethermind.Core.Crypto;
using Nethermind.State;

namespace Catalyst.Core.Modules.Ledger
{
    public sealed class Web3EthApi : IWeb3EthApi
    {
        private readonly ITransactionRepository _receipts;
        private readonly ITransactionReceivedEvent _transactionReceived;
        public IHashProvider HashProvider { get; }
        private readonly PeerId _peerId;

        public Web3EthApi(IStateReader stateReader,
            IDeltaResolver deltaResolver,
            IDeltaCache deltaCache,
            IDeltaExecutor executor,
            IStorageProvider storageProvider,
            IStateProvider stateProvider,
            ITransactionRepository receipts,
            ITransactionReceivedEvent transactionReceived,
            IHashProvider hashProvider,
            IPeerSettings peerSettings)
        {
            _receipts = receipts;
            _transactionReceived = transactionReceived ?? throw new ArgumentNullException(nameof(transactionReceived));
            HashProvider = hashProvider;
            _peerId = peerSettings.PeerId;

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

        public Keccak SendTransaction(PublicEntry publicEntry)
        {
            TransactionBroadcast broadcast = new TransactionBroadcast
            {
                PublicEntry = publicEntry
            };

            publicEntry.Timestamp = Timestamp.FromDateTime(DateTime.UtcNow);
            _transactionReceived.OnTransactionReceived(broadcast.ToProtocolMessage(_peerId));
            
            byte[] kvmAddressBytes = Keccak.Compute(publicEntry.SenderAddress.ToByteArray()).Bytes.AsSpan(12).ToArray();
            string hex = kvmAddressBytes.ToHexString() ?? throw new ArgumentNullException("kvmAddressBytes.ToHexString()");
            publicEntry.SenderAddress = kvmAddressBytes.ToByteString();
                
            if (publicEntry.ReceiverAddress.Length == 1)
            {
                publicEntry.ReceiverAddress = ByteString.Empty;
            }

            return publicEntry.GetHash(HashProvider);
        }

        public TransactionReceipt FindReceipt(Keccak transactionHash)
        {
            return _receipts.TryFind(transactionHash, out TransactionReceipt receipt) ? receipt : default;
        }

        public bool FindTransactionData(Keccak transactionHash, out Cid deltaHash, out Delta delta, out int index)
        {
            return _receipts.TryFind(transactionHash, out deltaHash, out delta, out index);
        }
    }
}
