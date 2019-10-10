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
using Catalyst.Abstractions.Mempool.Repositories;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.Repository;
using Dawn;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Core.Modules.Mempool.Repositories
{
    public class MempoolRepository : RepositoryWrapper<TransactionBroadcastDao>,
        IMempoolRepository<TransactionBroadcastDao>
    {
        public MempoolRepository(IRepository<TransactionBroadcastDao, string> repository) : base(repository) { }

        /// <inheritdoc />
        public bool TryReadItem(string signature)
        {
            Guard.Argument(signature, nameof(signature)).NotNull();
            return Repository.TryGet(signature, out _);
        }

        public TransactionBroadcastDao ReadItem(string signature)
        {
            Guard.Argument(signature, nameof(signature)).NotNull();
            return Repository.Get(signature);
        }

        /// <inheritdoc />
        public bool DeleteItem(params string[] transactionSignatures)
        {
            try
            {
                Repository.Delete(transactionSignatures);
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Failed to delete transactions from the mempool {transactionSignatures}",
                    transactionSignatures);
                return false;
            }

            return true;
        }

        public bool CreateItem(TransactionBroadcastDao transactionBroadcast)
        {
            Guard.Argument(transactionBroadcast.Signature, nameof(transactionBroadcast.Signature)).NotNull();

            try
            {
                Repository.Add(transactionBroadcast);
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, e.Message);
                return false;
            }

            return true;
        }
    }
}
