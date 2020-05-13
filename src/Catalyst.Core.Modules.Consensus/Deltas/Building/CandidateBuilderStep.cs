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
using System.Collections.Generic;
using System.Linq;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Hashing;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using Lib.P2P;
using MultiFormats;
using Nethermind.Dirichlet.Numerics;
using Serilog;

namespace Catalyst.Core.Modules.Consensus.Deltas.Building
{
    internal sealed class CandidateBuilderStep : IDeltaBuilderStep
    {
        private readonly MultiAddress _producerUniqueId;
        private readonly IDeterministicRandomFactory _randomFactory;
        private readonly IHashProvider _hashProvider;
        private readonly ILogger _logger;

        public CandidateBuilderStep(MultiAddress producerUniqueId,
            IDeterministicRandomFactory randomFactory,
            IHashProvider hashProvider,
            ILogger logger)
        {
            _producerUniqueId = producerUniqueId ?? throw new ArgumentNullException(nameof(producerUniqueId));
            _randomFactory = randomFactory ?? throw new ArgumentNullException(nameof(randomFactory));
            _hashProvider = hashProvider ?? throw new ArgumentNullException(nameof(hashProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        private byte[] GetSaltFromPreviousDelta(Cid previousDeltaHash)
        {
            IDeterministicRandom isaac = _randomFactory.GetDeterministicRandomFromSeed(previousDeltaHash.ToArray());
            return BitConverter.GetBytes(isaac.NextInt());
        }
        
        public void Execute(DeltaBuilderContext context)
        {
            byte[] salt = GetSaltFromPreviousDelta(context.PreviousDeltaHash);

            IEnumerable<RawEntryWithSaltedAndHashedEntry> rawAndSaltedEntriesBySignature = context.Transactions.Select(e => new RawEntryWithSaltedAndHashedEntry(e, salt, _hashProvider));

            // (Eα;Oα)
            byte[] shuffledEntriesBytes = rawAndSaltedEntriesBySignature
               .OrderBy(v => v.SaltedAndHashedEntry, ByteUtil.ByteListComparer.Default)
               .SelectMany(v => v.RawEntry.ToByteArray())
               .ToArray();

            // dn
            byte[] signaturesInOrder = context.Transactions
               .Select(p => p.Signature.ToByteArray())
               .OrderBy(s => s, ByteUtil.ByteListComparer.Default)
               .SelectMany(b => b)
               .ToArray();

            // xf
            UInt256 summedFees = context.Transactions.Sum(t => t.GasPrice.ToUInt256() * t.GasLimit);

            //∆Ln,j = L(f/E) + dn + E(xf, j)
            context.CoinbaseEntry = new CoinbaseEntry
            {
                Amount = summedFees.ToUint256ByteString(),
                ReceiverPublicKey = _producerUniqueId.PeerId.GetPublicKeyBytesFromPeerId().ToByteString()
            };
            
            byte[] globalLedgerStateUpdate = shuffledEntriesBytes
               .Concat(signaturesInOrder)
               .Concat(context.CoinbaseEntry.ToByteArray())
               .ToArray();

            //hj
            CandidateDeltaBroadcast candidate = new CandidateDeltaBroadcast
            {
                // h∆j
                Hash = MultiBase.Decode(_hashProvider.ComputeMultiHash(globalLedgerStateUpdate).ToCid())
                   .ToByteString(),

                // Idj
                ProducerId = _producerUniqueId.ToString(),
                PreviousDeltaDfsHash = context.PreviousDeltaHash.ToArray().ToByteString()
            };
            
            context.Candidate = candidate;
        }
        
        private sealed class RawEntryWithSaltedAndHashedEntry
        {
            public PublicEntry RawEntry { get; }

            public byte[] SaltedAndHashedEntry { get; }

            public RawEntryWithSaltedAndHashedEntry(PublicEntry rawEntry,
                byte[] salt,
                IHashProvider hashProvider)
            {
                RawEntry = rawEntry;
                SaltedAndHashedEntry = hashProvider.ComputeMultiHash(rawEntry, salt).ToArray();
            }
        }
    }
}
