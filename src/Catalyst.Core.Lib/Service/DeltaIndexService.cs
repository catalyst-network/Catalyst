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

using System.Collections.Generic;
using System.Linq;
using Catalyst.Core.Lib.DAO.Ledger;
using Lib.P2P;
using SharpRepository.Repository;
using SharpRepository.Repository.Queries;

namespace Catalyst.Core.Lib.Service
{
    public class DeltaIndexService : IDeltaIndexService
    {
        private readonly IRepository<DeltaIndexDao, string> _repository;

        public DeltaIndexService(IRepository<DeltaIndexDao, string> repository) { _repository = repository; }

        public void Add(IEnumerable<DeltaIndexDao> deltaIndexes)
        {
            _repository.Add(deltaIndexes);
        }

        public void Add(DeltaIndexDao deltaIndex)
        {
            _repository.Add(deltaIndex);
        }

        public IEnumerable<DeltaIndexDao> GetRange(ulong start, ulong count)
        {
            return _repository.FindAll(x => x.Height >= start && x.Height <= start + count).OrderBy(x => x.Height);
        }

        public ulong Height()
        {
            var deltaIndex = LatestDeltaIndex();
            if (deltaIndex == null)
            {
                return 0;
            }
            return deltaIndex.Height;
        }

        public DeltaIndexDao LatestDeltaIndex()
        {
            var pagingOptions = new PagingOptions<DeltaIndexDao, ulong>(1, 2, x => x.Height, isDescending: true);
            return _repository.GetAll(pagingOptions).FirstOrDefault();
        }

        public void Map(long deltaNumber, Cid deltaHash)
        {
            if (!_repository.TryGet(DeltaIndexDao.BuildDocumentId((ulong)deltaNumber), out _))
            {
                _repository.Add(new DeltaIndexDao { Height = (ulong)deltaNumber, Cid = deltaHash });
            }
        }

        public bool TryFind(long deltaNumber, out Cid deltaHash)
        {
            if (_repository.TryGet(DeltaIndexDao.BuildDocumentId((ulong)deltaNumber), out var delta))
            {
                deltaHash = delta.Cid;
                return true;
            }

            deltaHash = default;
            return false;
        }
    }
}
