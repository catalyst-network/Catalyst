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
using Catalyst.Abstractions.Hashing;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Google.Protobuf.WellKnownTypes;
using Lib.P2P;
using MultiFormats;

namespace Catalyst.TestUtils
{
    public static class DeltaHelper
    {
        public static Delta GetDelta(IHashProvider hashProvider,
            Cid previousDeltaHash = null,
            byte[] merkleRoot = default,
            byte[] merklePoda = default,
            DateTime? timestamp = default)
        {
            var previousHash = previousDeltaHash ??
                hashProvider.ComputeMultiHash(ByteUtil.GenerateRandomByteArray(32)).ToCid();
            var root = merkleRoot ?? ByteUtil.GenerateRandomByteArray(32);
            var poda = merklePoda ?? ByteUtil.GenerateRandomByteArray(32);
            var nonNullTimestamp =
                Timestamp.FromDateTime(timestamp?.ToUniversalTime() ?? DateTime.Now.ToUniversalTime());

            var delta = new Delta
            {
                PreviousDeltaDfsHash = previousHash.ToArray().ToByteString(),
                MerkleRoot = root.ToByteString(),
                MerklePoda = poda.ToByteString(),
                TimeStamp = nonNullTimestamp
            };

            return delta;
        }

        public static CandidateDeltaBroadcast GetCandidateDelta(IHashProvider hashProvider,
            Cid previousDeltaHash = null,
            Cid hash = null,
            MultiAddress producerId = null)
        {
            var candidateHash = hash ??
                hashProvider.ComputeMultiHash(ByteUtil.GenerateRandomByteArray(32)).ToCid();
            var previousHash = previousDeltaHash ??
                hashProvider.ComputeMultiHash(ByteUtil.GenerateRandomByteArray(32)).ToCid();
            var producer = producerId
             ?? MultiAddressHelper.GetAddress(ByteUtil.GenerateRandomByteArray(32));

            return new CandidateDeltaBroadcast
            {
                Hash = MultiBase.Decode(candidateHash).ToByteString(),
                PreviousDeltaDfsHash = MultiBase.Decode(previousHash).ToByteString(),
                Producer = producer.ToString()
            };
        }

        public static FavouriteDeltaBroadcast GetFavouriteDelta(IHashProvider hashProvider,
            Cid previousDeltaHash = null,
            Cid hash = null,
            MultiAddress producerId = null,
            MultiAddress voterId = null)
        {
            var candidate = GetCandidateDelta(hashProvider, previousDeltaHash, hash, producerId);
            var voter = voterId ?? MultiAddressHelper.GetAddress();

            return new FavouriteDeltaBroadcast
            {
                Candidate = candidate,
                Voter = voter.ToString()
            };
        }
    }
}
