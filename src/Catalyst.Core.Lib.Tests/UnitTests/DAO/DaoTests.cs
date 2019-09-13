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
using System.Net;
using Catalyst.Abstractions.DAO;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Deltas;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Transaction;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.Collections;
using ICSharpCode.SharpZipLib.Tar;
using Multiformats.Hash;
using Multiformats.Hash.Algorithms;
using Xunit;

namespace Catalyst.Core.Lib.Tests.UnitTests.DAO
{
    public class DaoTests
    {
        private readonly IMultihashAlgorithm _hashingAlgorithm = new BLAKE2B_256();
        private readonly IMapperInitializer[] _mappers;
        
        public DaoTests()
        {
            _mappers = new IMapperInitializer[]
            {
                new ProtocolMessageDao(),
                new CfTransactionEntryDao(),
                new CandidateDeltaBroadcastDao(),
                new ProtocolErrorMessageSignedDao(),
                new PeerIdDao(),
                new SigningContextDao(),
                new DeltaDao(),
                new CandidateDeltaBroadcastDao(),
                new DeltaDfsHashBroadcastDao(),
                new FavouriteDeltaBroadcastDao(),
                new CoinbaseEntryDao(),
                new StTransactionEntryDao(),
                new CfTransactionEntryDao(),
                new TransactionBroadcastDao(),
                new EntryRangeProofDao(), 
            };

            var map = new MapperProvider(_mappers);
            map.Start();
        }

        private TDao GetMapper<TDao>() where TDao : IMapperInitializer => _mappers.OfType<TDao>().First();
        
        [Fact]
        public void ProtocolMessageDao_ProtocolMessage_Should_Be_Convertible()
        {
            var protocolMessageDao = GetMapper<ProtocolMessageDao>();

            var newGuid = Guid.NewGuid();
            var peerId = PeerIdentifierHelper.GetPeerIdentifier("testcontent").PeerId;
            var original = new ProtocolMessage
            {
                CorrelationId = newGuid.ToByteString(),
                TypeUrl = "cleanurl",
                Value = "somecontent".ToUtf8ByteString(),
                PeerId = peerId
            };

            var messageDao = protocolMessageDao.ToDao(original);

            messageDao.TypeUrl.Should().Be("cleanurl");
            messageDao.CorrelationId.Should().Be(newGuid.ToString());
            messageDao.PeerId.Port.Should().Be(BitConverter.ToUInt16(peerId.Port.ToByteArray()));
            messageDao.PeerId.Ip.Should().Be(new IPAddress(peerId.Ip.ToByteArray()).MapToIPv6().ToString());

            var reconverted = messageDao.ToProtoBuff();
            reconverted.Should().Be(original);
        }

        [Fact]
        public void ProtocolErrorMessageSignedDao_ProtocolErrorMessageSigned_Should_Be_Convertible()
        {
            var protocolErrorMessageSignedDao = GetMapper<ProtocolErrorMessageSignedDao>();
            var byteRn = new byte[30];
            new Random().NextBytes(byteRn);

            var original = new ProtocolErrorMessageSigned
            {
                CorrelationId = Guid.NewGuid().ToByteString(),
                Signature = byteRn.ToByteString(),
                PeerId = PeerIdentifierHelper.GetPeerIdentifier("test").PeerId,
                Code = 74
            };

            var errorMessageSignedDao = protocolErrorMessageSignedDao.ToDao(original);
            var reconverted = errorMessageSignedDao.ToProtoBuff();
            reconverted.Should().Be(original);
        }

        [Fact]
        public void PeerIdDao_PeerId_Should_Be_Convertible()
        {
            var peerIdDao = GetMapper<PeerIdDao>();

            var original = PeerIdentifierHelper.GetPeerIdentifier("MyPeerId_Testing").PeerId;

            var peer = peerIdDao.ToDao(original);
            var reconverted = peer.ToProtoBuff();
            reconverted.Should().Be(original);
        }

        [Fact]
        public void RangeProofDao_RangeProof_Should_Be_Convertible()
        {
            var peerIdDao = GetMapper<EntryRangeProofDao>();

            var rangeProof = GetEntryRangeProof();

            var peer = peerIdDao.ToDao(rangeProof);
            var reconverted = peer.ToProtoBuff();
            reconverted.Should().Be(rangeProof);
        }

        private static EntryRangeProof GetEntryRangeProof()
        {
            return new EntryRangeProof
            {
                A = "a".ToUtf8ByteString(),
                APrime0 = "a prime 0".ToUtf8ByteString(),
                BPrime0 = "b prime 0".ToUtf8ByteString(),
                MU = "mu".ToUtf8ByteString(),
                S = "s".ToUtf8ByteString(),
                T = "t".ToUtf8ByteString(),
                T1 = "t1".ToUtf8ByteString(),
                T2 = "t2".ToUtf8ByteString(),
                TAU = "tau".ToUtf8ByteString(),
                L = {"L".ToUtf8ByteString()},
                R = {"R".ToUtf8ByteString()},
                V = {"V".ToUtf8ByteString()},
            };
        }

        [Fact]
        public void SigningContextDao_SigningContext_Should_Be_Convertible()
        {
            var signingContextDao = GetMapper<SigningContextDao>();
            var byteRn = new byte[30];
            new Random().NextBytes(byteRn);

            var original = new SigningContext
            {
                Network = Protocol.Common.Network.Devnet,
                SignatureType = SignatureType.TransactionPublic
            };

            var contextDao = signingContextDao.ToDao(original);
            var reconverted = contextDao.ToProtoBuff();
            reconverted.Should().Be(original);
        }

        [Fact]
        public void DeltasDao_Deltas_Should_Be_Convertible()
        {
            var deltaDao = GetMapper<DeltaDao>();

            var previousHash = "previousHash".ComputeUtf8Multihash(_hashingAlgorithm).ToBytes();

            var original = DeltaHelper.GetDelta(previousHash);

            var messageDao = deltaDao.ToDao(original);
            var reconverted = messageDao.ToProtoBuff();
            original.Should().Be(reconverted);
        }

        [Fact]
        public void CandidateDeltaBroadcastDao_CandidateDeltaBroadcast_Should_Be_Convertible()
        {
            var candidateDeltaBroadcastDao = GetMapper<CandidateDeltaBroadcastDao>();
            var previousHash = "previousHash".ComputeUtf8Multihash(_hashingAlgorithm).ToBytes();
            var hash = "anotherHash".ComputeUtf8Multihash(_hashingAlgorithm).ToBytes();

            var original = new CandidateDeltaBroadcast
            {
                Hash = hash.ToByteString(),
                ProducerId = PeerIdentifierHelper.GetPeerIdentifier("test").PeerId,
                PreviousDeltaDfsHash = previousHash.ToByteString()
            };

            var candidateDeltaBroadcast = candidateDeltaBroadcastDao.ToDao(original);
            var reconverted = candidateDeltaBroadcast.ToProtoBuff();
            reconverted.Should().Be(original);
        }

        [Fact]
        public void DeltaDfsHashBroadcastDao_DeltaDfsHashBroadcast_Should_Be_Convertible()
        {
            var deltaDfsHashBroadcastDao = GetMapper<DeltaDfsHashBroadcastDao>();

            var hash = "this hash".ComputeUtf8Multihash(_hashingAlgorithm).ToBytes();
            var previousDfsHash = "previousDfsHash".ComputeUtf8Multihash(_hashingAlgorithm).ToBytes();

            var original = new DeltaDfsHashBroadcast
            {
                DeltaDfsHash = hash.ToByteString(),
                PreviousDeltaDfsHash = previousDfsHash.ToByteString()
            };

            var contextDao = deltaDfsHashBroadcastDao.ToDao(original);
            var reconverted = contextDao.ToProtoBuff();
            reconverted.Should().Be(original);
        }

        [Fact]
        public void FavouriteDeltaBroadcastDao_FavouriteDeltaBroadcast_Should_Be_Convertible()
        {
            var favouriteDeltaBroadcastDao = GetMapper<FavouriteDeltaBroadcastDao>();

            var original = new FavouriteDeltaBroadcast
            {
                Candidate = DeltaHelper.GetCandidateDelta(producerId: PeerIdHelper.GetPeerId("not me")),
                VoterId = PeerIdentifierHelper.GetPeerIdentifier("test").PeerId
            };

            var contextDao = favouriteDeltaBroadcastDao.ToDao(original);
            var reconverted = contextDao.ToProtoBuff();
            reconverted.Should().Be(original);
        }

        [Fact]
        public void CoinbaseEntryDao_CoinbaseEntry_Should_Be_Convertible()
        {
            var coinbaseEntryDao = GetMapper<CoinbaseEntryDao>();
            var pubKeyBytes = new byte[30];
            new Random().NextBytes(pubKeyBytes);

            var original = new CoinbaseEntry
            {
                Version = 415,
                PubKey = pubKeyBytes.ToByteString(),
                Amount = 271314
            };

            var messageDao = coinbaseEntryDao.ToDao(original);
            messageDao.PubKey.Should().Be(pubKeyBytes.KeyToString());

            var reconverted = messageDao.ToProtoBuff();
            reconverted.Should().Be(original);
        }

        [Fact]
        public void STTransactionEntryDao_STTransactionEntry_Should_Be_Convertible()
        {
            var stTransactionEntryDao = GetMapper<StTransactionEntryDao>();
            var pubKeyBytes = new byte[30];
            new Random().NextBytes(pubKeyBytes);

            var original = new STTransactionEntry
            {
                PubKey = pubKeyBytes.ToByteString(),
                Amount = 8855274
            };

            var transactionEntryDao = stTransactionEntryDao.ToDao(original);

            transactionEntryDao.PubKey.Should().Be(pubKeyBytes.KeyToString());
            transactionEntryDao.Amount.Should().Be(8855274);

            var reconverted = transactionEntryDao.ToProtoBuff();
            reconverted.Should().Be(original);
        }

        [Fact]
        public void CFTransactionEntryDao_CFTransactionEntry_Should_Be_Convertible()
        {
            var cfTransactionEntryDao = GetMapper<CfTransactionEntryDao>();

            var pubKeyBytes = new byte[30];
            var pedersenCommitBytes = new byte[50];

            var rnd = new Random();
            rnd.NextBytes(pubKeyBytes);
            rnd.NextBytes(pedersenCommitBytes);

            var original = new CFTransactionEntry
            {
                PubKey = pubKeyBytes.ToByteString(),
                PedersenCommit = pedersenCommitBytes.ToByteString(),
                EntryRangeProofs = GetEntryRangeProof()
            };

            var transactionEntryDao = cfTransactionEntryDao.ToDao(original);

            transactionEntryDao.PubKey.Should().Be(pubKeyBytes.KeyToString());
            transactionEntryDao.PedersenCommit.Should().Be(pedersenCommitBytes.ToByteString().ToBase64());

            var reconverted = transactionEntryDao.ToProtoBuff();
            reconverted.Should().Be(original);
        }

        [Fact]
        public void TransactionBroadcastDao_TransactionBroadcast_Should_Be_Convertible()
        {
            var transactionBroadcastDao = GetMapper<TransactionBroadcastDao>();

            var original = TransactionHelper.GetTransaction();

            var transactionEntryDao = transactionBroadcastDao.ToDao(original);
            var reconverted = transactionEntryDao.ToProtoBuff();
            reconverted.Should().Be(original);
        }
    }
}
