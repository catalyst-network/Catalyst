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
using Catalyst.Abstractions.Mempool;
using Catalyst.Core.Extensions;
using Catalyst.Core.Ledger.Models;
using Catalyst.Core.Ledger.Repository;
using Catalyst.Core.Mempool.Documents;
using Catalyst.Protocol.Transaction;
using Dawn;
using Multiformats.Hash;
using Serilog;

namespace Catalyst.Core.Ledger
{
    /// <summary>
    ///  This class represents a ledger and is a collection of accounts and data store.
    /// </summary>
    /// <inheritdoc cref="ILedger" />
    /// <inheritdoc cref="IDisposable" />
    public class Ledger : ILedger, IDisposable
    {
        public IAccountRepository Accounts { get; }
        private readonly IDeltaDfsReader _deltaDfsReader;
        private readonly ILedgerSynchroniser _synchroniser;
        private readonly IMempool<MempoolDocument> _mempool;
        private readonly ILogger _logger;
        private readonly IDisposable _deltaUpdatesSubscription;

        public Ledger(IAccountRepository accounts, 
            IDeltaHashProvider deltaHashProvider, 
            IDeltaDfsReader deltaDfsReader,
            ILedgerSynchroniser synchroniser,
            IMempool<MempoolDocument> mempool, 
            ILogger logger)
        {
            Accounts = accounts;
            _deltaDfsReader = deltaDfsReader;
            _synchroniser = synchroniser;
            _mempool = mempool;
            _logger = logger;

            _deltaUpdatesSubscription = deltaHashProvider.DeltaHashUpdates.Subscribe(Update);
        }

        private void FlushTransactionsFromDelta(TransactionSignature confirmedDelta)
        {
            var transactionsToFlush = _mempool.Repository.GetAll().Select(d => d.ToString()); //LOL
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
                if (!_deltaDfsReader.TryReadDeltaFromDfs(deltaHash.AsBase32Address(), out var delta))
                {
                    _logger.Warning(
                        "Failed to retrieve Delta with hash {hash} from the Dfs, ledger has not been updated.", deltaHash);
                    return;
                }

                if (!Equals(delta.PreviousDeltaDfsHash.AsMultihash(), LatestKnownDelta))
                {
                    var chainedDeltas =
                        _synchroniser.RetrieveDeltasBetween(LatestKnownDelta, deltaHash, CancellationToken.None);

                    // now they should all be in the cache and we need to find them back :)
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed to update the ledger using the delta with hash {deltaHash}", deltaHash);
            }
        }

        public Multihash LatestKnownDelta { get; private set; }

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
        }
    }
}
