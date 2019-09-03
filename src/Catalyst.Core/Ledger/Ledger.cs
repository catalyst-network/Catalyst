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
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Mempool;
using Catalyst.Core.Ledger.Models;
using Catalyst.Core.Ledger.Repository;
using Catalyst.Core.Mempool.Documents;
using Dawn;
using Multiformats.Hash;
using Serilog;

namespace Catalyst.Core.Ledger
{
    /// <summary>
    ///  This class represents a ledger and is a collection of accounts and data store.
    /// </summary>
    /// <seealso cref="ILedger" />
    public sealed class Ledger : ILedger, IDisposable
    {
        private IAccountRepository Accounts { get; }
        private readonly IMempool<MempoolDocument> _mempool;
        private readonly ILogger _logger;
        private readonly IDisposable _deltaUpdatesSubscription;

        public Ledger(IAccountRepository accounts, IDeltaHashProvider deltaHashProvider, IMempool<MempoolDocument> mempool, ILogger logger)
        {
            Accounts = accounts;
            _mempool = mempool;
            _logger = logger;

            _deltaUpdatesSubscription = deltaHashProvider.DeltaHashUpdates.Subscribe(FlushTransactionsFromDelta);
        }

        private void FlushTransactionsFromDelta(Multihash confirmedDelta)
        {
            var transactionsToFlush = _mempool.Repository.GetAll().Select(d => d.ToString()); //LOL
            _mempool.Repository.DeleteItem(transactionsToFlush.ToArray());
        }

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

        private void Dispose(bool disposing)
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
