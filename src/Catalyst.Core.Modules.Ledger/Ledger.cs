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
using System.Linq;
using System.Threading;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Mempool;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Extensions.Protocol.Account;
using Catalyst.Core.Lib.Mempool.Documents;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Ledger.Repository;
using Catalyst.Protocol.Account;
using Catalyst.Protocol.Transaction;
using Dawn;
using Multiformats.Hash;
using Nethermind.Core;
using Nethermind.Evm;
using Nethermind.Store;
using Serilog;
using Address = Catalyst.Protocol.Account.Address;
using EthereumAddress = Nethermind.Core.Address;

namespace Catalyst.Core.Modules.Ledger
{
    /// <summary>
    ///  This class represents a ledger and is a collection of accounts and data store.
    /// </summary>
    /// <inheritdoc cref="ILedger" />
    /// <inheritdoc cref="IDisposable" />
    public class Ledger : ILedger, IDisposable
    {
        public IAccountRepository Accounts { get; }
        private readonly ILedgerSynchroniser _synchroniser;
        private readonly IMempool<MempoolDocument> _mempool;
        private readonly IPeerSettings _peerSettings;
        private readonly IVirtualMachine _evm;
        private readonly IStateProvider _stateProvider;
        private readonly ILogger _logger;
        private readonly IDisposable _deltaUpdatesSubscription;

        private readonly object _synchronisationLock = new object();
        private readonly ICryptoContext _cryptoContext;

        public Ledger(IAccountRepository accounts, 
            IDeltaHashProvider deltaHashProvider,
            ILedgerSynchroniser synchroniser,
            IMempool<MempoolDocument> mempool,
            IPeerSettings peerSettings, 
            IVirtualMachine evm,
            IStateProvider stateProvider,
            ILogger logger)
        {
            Accounts = accounts;
            _synchroniser = synchroniser;
            _mempool = mempool;
            _peerSettings = peerSettings;
            _evm = evm;
            _stateProvider = stateProvider;
            _logger = logger;

            _deltaUpdatesSubscription = deltaHashProvider.DeltaHashUpdates.Subscribe(Update);
            LatestKnownDelta = _synchroniser.DeltaCache.GenesisAddress;
            _cryptoContext = new FfiWrapper();
        }

        private void FlushTransactionsFromDelta()
        {
            var transactionsToFlush = _mempool.Repository.GetAll().Select(d => d.ToString()); //@TODO no get alls
            _mempool.Repository.DeleteItem(transactionsToFlush.ToArray());
        }

        /// <inheritdoc />
        public bool SaveAccountState(Account account)
        {
            Guard.Argument(account, nameof(account)).NotNull();

            try
            {
                Accounts.Add(account);
                return true;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to add account state to the Ledger");
                return false;
            }
        }

        /// <inheritdoc />
        public void Update(Multihash deltaHash)
        {
            try
            {
                lock (_synchronisationLock)
                {
                    var chainedDeltaHashes = _synchroniser
                       .CacheDeltasBetween(LatestKnownDelta, deltaHash, CancellationToken.None)
                       .Reverse()
                       .ToList();

                    if (!Equals(chainedDeltaHashes.First(), LatestKnownDelta))
                    {
                        _logger.Warning(
                            "Failed to walk back the delta chain to {LatestKnownDelta}, giving up ledger update.", LatestKnownDelta);
                        return;
                    }

                    foreach (var chainedDeltaHash in chainedDeltaHashes)
                    {
                        UpdateLedgerFromDelta(chainedDeltaHash);
                    }
                }

                //https://github.com/catalyst-network/Catalyst.Node/issues/871
                FlushTransactionsFromDelta();
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed to update the ledger using the delta with hash {deltaHash}", deltaHash);
            }
        }

        private void UpdateLedgerFromDelta(Multihash deltaHash)
        {
            if (!_synchroniser.DeltaCache.TryGetOrAddConfirmedDelta(deltaHash.AsBase32Address(), out var nextDeltaInChain))
            {
                _logger.Warning(
                    "Failed to retrieve Delta with hash {hash} from the Dfs, ledger has not been updated.", deltaHash);
                return;
            }

            foreach (var entry in nextDeltaInChain.PublicEntries)
            {
                UpdateLedgerAccountFromEntry(entry);
            }

            LatestKnownDelta = deltaHash;
        }

        private void UpdateLedgerAccountFromEntry(PublicEntry entry)
        {
            var address = GetEthereumReceiverAddress(entry.Base);

            var account = Accounts.Get(address.ToString());
        }

        private void RunSmartContract(ContractEntry entry)
        {
            var address = GetEthereumReceiverAddress(entry.Base);
            var codeInfo = _evm.GetCachedCodeInfo(address);
        }

        private EthereumAddress GetEthereumReceiverAddress(BaseEntry entry)
        {
            var pubKey = _cryptoContext.GetPublicKeyFromBytes(entry.ReceiverPublicKey.ToByteArray());
            var address = pubKey.Bytes.ToEthereumAddress(_peerSettings.NetworkType, AccountType.PublicAccount);
            return address;
        }

        public Multihash LatestKnownDelta { get; private set; }

        public bool IsSynchonising => Monitor.IsEntered(_synchronisationLock);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            Accounts?.Dispose();
            _deltaUpdatesSubscription?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
