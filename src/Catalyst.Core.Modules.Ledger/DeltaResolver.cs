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

using Catalyst.Abstractions.Kvm;
using Catalyst.Abstractions.Ledger;
using Catalyst.Core.Lib.Service;
using Lib.P2P;

namespace Catalyst.Core.Modules.Ledger
{
    public sealed class DeltaResolver : IDeltaResolver
    {
        readonly IDeltaIndexService _deltaIndexService;
        readonly ILedger _ledger;

        public DeltaResolver(IDeltaIndexService deltaIndexService, ILedger ledger)
        {
            _deltaIndexService = deltaIndexService;
            _ledger = ledger;
        }

        public bool TryResolve(long deltaNumber, out Cid deltaHash) => _deltaIndexService.TryFind(deltaNumber, out deltaHash);

        public long LatestDeltaNumber => _ledger.LatestKnownDeltaNumber;

        public Cid LatestDelta => _ledger.LatestKnownDelta;
    }
}
