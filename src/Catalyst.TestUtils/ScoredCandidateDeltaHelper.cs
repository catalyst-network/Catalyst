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

using Catalyst.Abstractions.Hashing;
using Catalyst.Core.Modules.Consensus.Deltas;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Lib.P2P;

namespace Catalyst.TestUtils
{
    public static class ScoredCandidateDeltaHelper
    {
        public static ScoredCandidateDelta GetScoredCandidateDelta(IHashProvider hashProvider,
            CandidateDeltaBroadcast candidate = default,
            int score = 0)
        {
            var candidateDelta = candidate ?? DeltaHelper.GetCandidateDelta(hashProvider);
            return new ScoredCandidateDelta(candidateDelta, score);
        }

        public static ScoredCandidateDelta GetScoredCandidateDelta(IHashProvider hashProvider,
            Cid previousDeltaHash = null,
            Cid hash = null,
            PeerId producerId = null,
            int score = 0)
        {
            var candidateDelta = DeltaHelper.GetCandidateDelta(hashProvider, hash: hash, producerId: producerId,
                previousDeltaHash: previousDeltaHash);
            return new ScoredCandidateDelta(candidateDelta, score);
        }
    }
}
