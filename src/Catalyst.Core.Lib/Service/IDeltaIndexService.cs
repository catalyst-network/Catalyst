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

using System.Collections.Generic;
using Catalyst.Core.Lib.DAO.Ledger;
using Lib.P2P;

namespace Catalyst.Core.Lib.Service
{
    public interface IDeltaIndexService
    {
        void Add(DeltaIndexDao deltaIndex);
        void Add(IEnumerable<DeltaIndexDao> deltaIndexes);
        ulong Height();
        IEnumerable<DeltaIndexDao> GetRange(ulong start, ulong count);
        DeltaIndexDao LatestDeltaIndex();
        void Map(long deltaNumber, Cid deltaHash);
        bool TryFind(long deltaNumber, out Cid deltaHash);
    }
}
