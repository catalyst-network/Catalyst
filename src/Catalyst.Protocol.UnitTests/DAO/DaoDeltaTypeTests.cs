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

using System;
using Catalyst.Common.Extensions;
using Catalyst.Protocol.DAO.Deltas;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Extensions;
using Catalyst.TestUtils;
using FluentAssertions;
using Multiformats.Hash.Algorithms;
using Xunit;

namespace Catalyst.Protocol.UnitTests.DAO
{
    public class DaoDeltaTypeTests
    {
        [Fact]
        public static void DeltasDao_Deltas_Should_Be_Convertible()
        {
            var deltaDao = new DeltaDao();

            var previousHash = "previousHash".ComputeUtf8Multihash(new ID()).ToBytes();

            var message = DeltaHelper.GetDelta(previousHash);

            var messageDao = deltaDao.ToDao(message);
            var protoBuff = messageDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public static void CandidateDeltaBroadcastDao_CandidateDeltaBroadcast_Should_Be_Convertible()
        {
            var candidateDeltaBroadcastDao = new CandidateDeltaBroadcastDao();
            var byteRn = new byte[30];
            new Random().NextBytes(byteRn);

            var previousHash = "previousHash".ComputeUtf8Multihash(new ID()).ToBytes();

            var message = new CandidateDeltaBroadcast
            {
                Hash = previousHash.ToByteString(),
                ProducerId = PeerIdentifierHelper.GetPeerIdentifier("test").PeerId,
                PreviousDeltaDfsHash = byteRn.ToByteString()
            };

            var candidateDeltaBroadcast = candidateDeltaBroadcastDao.ToDao(message);
            var protoBuff = candidateDeltaBroadcast.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public static void DeltaDfsHashBroadcastDao_DeltaDfsHashBroadcast_Should_Be_Convertible()
        {
            var deltaDfsHashBroadcastDao = new DeltaDfsHashBroadcastDao();
            var byteRn = new byte[30];
            new Random().NextBytes(byteRn);

            var previousDfsHash = "previousDfsHash".ComputeUtf8Multihash(new ID()).ToBytes();

            var message = new DeltaDfsHashBroadcast
            {
                DeltaDfsHash = byteRn.ToByteString(),
                PreviousDeltaDfsHash = previousDfsHash.ToByteString()
            };

            var contextDao = deltaDfsHashBroadcastDao.ToDao(message);
            var protoBuff = contextDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public static void FavouriteDeltaBroadcastDao_FavouriteDeltaBroadcast_Should_Be_Convertible()
        {
            var favouriteDeltaBroadcastDao = new FavouriteDeltaBroadcastDao();

            var message = new FavouriteDeltaBroadcast
            {
                Candidate = DeltaHelper.GetCandidateDelta(producerId: PeerIdHelper.GetPeerId("not me")),
                VoterId = PeerIdentifierHelper.GetPeerIdentifier("test").PeerId
            };

            var contextDao = favouriteDeltaBroadcastDao.ToDao(message);
            var protoBuff = contextDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }
    }
}
