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
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Transaction;
using Nethermind.Dirichlet.Numerics;

namespace Catalyst.Core.Modules.Consensus.Deltas.Building
{
    internal sealed class AveragePriceComparer : IComparer<PublicEntry>
    {
        private readonly int _multiplier;

        private AveragePriceComparer(int multiplier) { _multiplier = multiplier; }

        public int Compare(PublicEntry x, PublicEntry y)
        {
            return _multiplier * Comparer<UInt256?>.Default.Compare(x?.GasPrice.ToUInt256(), y?.GasPrice.ToUInt256());
        }

        public static AveragePriceComparer InstanceDesc { get; } = new(-1);
        public static AveragePriceComparer InstanceAsc { get; } = new(1);
    }
}
