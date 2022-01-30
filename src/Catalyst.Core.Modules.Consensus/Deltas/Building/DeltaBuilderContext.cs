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

using System.Collections.Generic;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using Lib.P2P;

namespace Catalyst.Core.Modules.Consensus.Deltas.Building
{
    internal sealed class DeltaBuilderContext
    {
        public DeltaBuilderContext(Cid previousDeltaHash) { PreviousDeltaHash = previousDeltaHash; }
        
        public Cid PreviousDeltaHash { get; }
        public Delta PreviousDelta { get; set; }
        public IList<PublicEntry> Transactions { get; set; }
        public CandidateDeltaBroadcast Candidate { get; set; }
        public CoinbaseEntry CoinbaseEntry { get; set; }
        public Delta ProducedDelta { get; set; }
    }
}
