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

using Catalyst.Core.Consensus.Deltas;
using Catalyst.Core.Util;
using Catalyst.Protocol.Deltas;
using Catalyst.TestUtils;
using FluentAssertions;
using Xunit;

namespace Catalyst.Core.UnitTests.Consensus.Deltas
{
    public class FavouriteByHashAndVoterComparerTests
    {
        public class FavouritesComparisonData : TheoryData<FavouriteDeltaBroadcast, FavouriteDeltaBroadcast, bool>
        {
            public FavouritesComparisonData()
            {
                Add(null, null, true);
                Add(new FavouriteDeltaBroadcast(), new FavouriteDeltaBroadcast(), true);
                Add(null, new FavouriteDeltaBroadcast(), false);
                Add(new FavouriteDeltaBroadcast(), null, false);

                var voter1 = PeerIdHelper.GetPeerId("voter1");
                var voter2 = PeerIdHelper.GetPeerId("voter2");

                var producer1 = PeerIdHelper.GetPeerId("producer1");
                var producer2 = PeerIdHelper.GetPeerId("producer2");

                var hash1 = ByteUtil.GenerateRandomByteArray(32).ToByteString();
                var hash2 = ByteUtil.GenerateRandomByteArray(32).ToByteString();

                var previousHash1 = ByteUtil.GenerateRandomByteArray(32).ToByteString();
                var previousHash2 = ByteUtil.GenerateRandomByteArray(32).ToByteString();

                Add(new FavouriteDeltaBroadcast
                    {
                        Candidate = new CandidateDeltaBroadcast
                        {
                            Hash = hash1, ProducerId = producer1, PreviousDeltaDfsHash = previousHash1
                        },
                        VoterId = voter1
                    },
                    new FavouriteDeltaBroadcast
                    {
                        Candidate = new CandidateDeltaBroadcast
                        {
                            Hash = hash2, ProducerId = producer1, PreviousDeltaDfsHash = previousHash1
                        },
                        VoterId = voter1
                    }, false);

                Add(new FavouriteDeltaBroadcast
                    {
                        Candidate = new CandidateDeltaBroadcast
                        {
                            Hash = hash1, ProducerId = producer1, PreviousDeltaDfsHash = previousHash1
                        },
                        VoterId = voter1
                    },
                    new FavouriteDeltaBroadcast
                    {
                        Candidate = new CandidateDeltaBroadcast
                        {
                            Hash = hash1, ProducerId = producer1, PreviousDeltaDfsHash = previousHash1
                        },
                        VoterId = voter2
                    }, false);

                Add(new FavouriteDeltaBroadcast
                    {
                        Candidate = new CandidateDeltaBroadcast
                        {
                            Hash = hash1, ProducerId = producer1, PreviousDeltaDfsHash = previousHash1
                        },
                        VoterId = voter1
                    }, 
                    new FavouriteDeltaBroadcast
                    {
                        Candidate = new CandidateDeltaBroadcast
                        {
                            Hash = hash1, ProducerId = producer2, PreviousDeltaDfsHash = previousHash2
                        },
                        VoterId = voter1
                    }, true);
            }
        }
        
        [Theory]
        [ClassData(typeof(FavouritesComparisonData))]
        public void FavouriteByHashComparer_should_differentiate_by_candidate_hash_and_voter_only(FavouriteDeltaBroadcast x, FavouriteDeltaBroadcast y, bool comparisonResult)
        {
            var xHashCode = FavouriteByHashAndVoterComparer.Default.GetHashCode(x);
            var yHashCode = FavouriteByHashAndVoterComparer.Default.GetHashCode(y);
            if (xHashCode != 0 && yHashCode != 0)
            {
                xHashCode.Equals(yHashCode).Should().Be(comparisonResult);
            }

            FavouriteByHashAndVoterComparer.Default.Equals(x, y).Should().Be(comparisonResult);
        }
    }
}
