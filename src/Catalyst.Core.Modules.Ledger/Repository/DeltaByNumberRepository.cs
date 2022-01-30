#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

using Catalyst.Abstractions.Ledger.Models;
using Lib.P2P;
using SharpRepository.Repository;

namespace Catalyst.Core.Modules.Ledger.Repository
{
    public class DeltaByNumberRepository : IDeltaByNumberRepository
    {
        readonly IRepository<DeltaByNumber, string> _repository;

        public DeltaByNumberRepository(IRepository<DeltaByNumber, string> repository)
        {
            _repository = repository;
        }

        public void Map(long deltaNumber, Cid deltaHash)
        {
            if (!_repository.TryGet(DeltaByNumber.BuildDocumentId(deltaNumber), out _))
            {
                _repository.Add(new DeltaByNumber(deltaNumber, deltaHash));
            }
        }

        public bool TryFind(long deltaNumber, out Cid deltaHash)
        {
            if (_repository.TryGet(DeltaByNumber.BuildDocumentId(deltaNumber), out var delta))
            {
                deltaHash = delta.DeltaId;
                return true;
            }

            deltaHash = default;
            return false;
        }
    }
}
