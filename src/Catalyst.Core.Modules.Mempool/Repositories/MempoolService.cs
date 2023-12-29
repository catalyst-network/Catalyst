#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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
using System.Collections.Generic;
using Catalyst.Abstractions.Mempool.Services;
using Catalyst.Core.Lib.DAO.Transaction;
using Dawn;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Core.Modules.Mempool.Repositories
{
    public class MempoolService : IMempoolService<PublicEntryDao>
    {
        private readonly IRepository<PublicEntryDao, string> _repository;

        public MempoolService(IRepository<PublicEntryDao, string> repository)
        {
            _repository = repository;
        }

        public IEnumerable<PublicEntryDao> GetAll()
        {
            return _repository.GetAll();
        }

        /// <inheritdoc />
        public bool TryReadItem(string id)
        {
            Guard.Argument(id, nameof(id)).NotNull();
            return _repository.TryGet(id, out _);
        }

        public PublicEntryDao ReadItem(string id)
        {
            Guard.Argument(id, nameof(id)).NotNull();
            return _repository.Get(id);
        }

        public void Delete(IEnumerable<PublicEntryDao> mempoolItems)
        {
            _repository.Delete(mempoolItems);
        }

        /// <inheritdoc />
        public bool DeleteItem(params string[] ids)
        {
            try
            {
                _repository.Delete(ids);
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Failed to delete transactions from the mempool {ids}", ids);
                return false;
            }

            return true;
        }

        public bool CreateItem(PublicEntryDao mempoolItem)
        {
            Guard.Argument(mempoolItem.Id, nameof(mempoolItem.Id)).NotNull();

            try
            {
                _repository.Add(mempoolItem);
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, e.Message);
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            _repository.Dispose();
        }
    }
}
