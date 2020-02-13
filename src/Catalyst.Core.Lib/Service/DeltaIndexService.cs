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
            var c = deltaIndexes.Count();
            _repository.Add(deltaIndexes);
            if (_repository.Count() > 100)
            {
                var a = 0;
            }
        }

        public void Add(DeltaIndexDao deltaIndex)
        {
            _repository.Add(deltaIndex);
            if (_repository.Count() > 100)
            {
                var a = 0;
            }
        }

        public IEnumerable<DeltaIndexDao> GetRange(int start, int count)
        {
            return _repository.FindAll(x => x.Height >= start && x.Height < start + count).OrderBy(x => x.Height);
        }

        public int Height()
        {
            var deltaIndex = LatestDeltaIndex();
            if (deltaIndex == null)
            {
                return 0;
            }
            return deltaIndex.Height;
        }

        public DeltaIndexDao LatestDeltaIndex() {
            var pagingOptions = new PagingOptions<DeltaIndexDao, int>(1, 2, x => x.Height, isDescending: true);
            return _repository.GetAll(pagingOptions).FirstOrDefault();
        }
    }
}
