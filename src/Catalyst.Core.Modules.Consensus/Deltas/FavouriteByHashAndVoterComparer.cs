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
using System.Text;
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using CandidateDeltaBroadcast = Catalyst.Protocol.Wire.CandidateDeltaBroadcast;

namespace Catalyst.Core.Modules.Consensus.Deltas
{
    public class FavouriteByHashAndVoterComparer : IEqualityComparer<FavouriteDeltaBroadcast>,
        IComparer<FavouriteDeltaBroadcast>
    {
        public int Compare(FavouriteDeltaBroadcast x, FavouriteDeltaBroadcast y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (ReferenceEquals(null, y))
            {
                return 1;
            }

            if (ReferenceEquals(null, x))
            {
                return -1;
            }

            var candidateHashComparison = CompareCandidateHash(x.Candidate, y.Candidate);

            if (candidateHashComparison != 0)
            {
                return candidateHashComparison;
            }

            var voterIdXBytes = x.Voter.ToByteArray();
            var voterIdYBytes = y.Voter.ToByteArray();
            return ByteUtil.ByteListComparer.Default.Compare(voterIdXBytes, voterIdYBytes);
        }

        private static int CompareCandidateHash(CandidateDeltaBroadcast x, CandidateDeltaBroadcast y)
        {
            var xByteArray = x?.Hash?.ToByteArray();
            var yByteArray = y?.Hash?.ToByteArray();
            return
                ByteUtil.ByteListMinSizeComparer.Default.Compare(
                    xByteArray,
                    yByteArray);
        }

        public static IEqualityComparer<FavouriteDeltaBroadcast> Default { get; } = new FavouriteByHashAndVoterComparer();

        public bool Equals(FavouriteDeltaBroadcast x, FavouriteDeltaBroadcast y)
        {
            return Compare(x, y) == 0;
        }

        public int GetHashCode(FavouriteDeltaBroadcast favourite)
        {
            if (favourite == null)
            {
                return 0;
            }

            unchecked
            {
                var candidateHash = favourite.Candidate?.Hash == null ? 0 : favourite.Candidate.Hash.GetHashCode();
                var voterHash = favourite.Voter == null ? 0 : favourite.Voter.GetHashCode();
                return (candidateHash * 397) ^ voterHash;
            }
        }
    }
}
