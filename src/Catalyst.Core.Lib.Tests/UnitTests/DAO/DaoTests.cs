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
using System.Net;
using System.Text;
using Catalyst.Abstractions.DAO;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Cryptography;
using Catalyst.Core.Lib.DAO.Deltas;
using Catalyst.Core.Lib.DAO.Peer;
using Catalyst.Core.Lib.DAO.Transaction;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Network;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using Catalyst.TestUtils.Protocol;
using FluentAssertions;
using MultiFormats;
using MultiFormats.Registry;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Nethermind.Dirichlet.Numerics;
using Xunit;
using System.Numerics;

namespace Catalyst.Core.Lib.Tests.UnitTests.DAO
{
    public class DaoTests
    {
        private readonly HashProvider _hashProvider;
        private readonly MapperProvider _mapperProvider;

        public DaoTests()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));

            var initialisers = new IMapperInitializer[]
            {
                new ProtocolMessageMapperInitialiser(),
                new ConfidentialEntryMapperInitialiser(),
                new CandidateDeltaBroadcastMapperInitialiser(),
                new ProtocolErrorMessageMapperInitialiser(),
                new PeerIdMapperInitialiser(),
                new SigningContextMapperInitialiser(),
                new DeltaMapperInitialiser(),
                new CandidateDeltaBroadcastMapperInitialiser(),
                new DeltaDfsHashBroadcastMapperInitialiser(),
                new FavouriteDeltaBroadcastMapperInitialiser(),
                new CoinbaseEntryMapperInitialiser(),
                new PublicEntryMapperInitialiser(_hashProvider),
                new ConfidentialEntryMapperInitialiser(),
                new TransactionBroadcastMapperInitialiser(),
                new SignatureMapperInitialiser()
            };

            _mapperProvider = new MapperProvider(initialisers);
        }

        [Fact]
        public void ProtocolMessageDao_ProtocolMessage_Should_Be_Convertible()
        {
            var newGuid = Guid.NewGuid();
            var peerId = PeerIdHelper.GetPeerId("testcontent");
            var original = new ProtocolMessage
            {
                CorrelationId = newGuid.ToByteString(),
                TypeUrl = "cleanurl",
                Value = "somecontent".ToUtf8ByteString(),
                PeerId = peerId
            };

            var messageDao = original.ToDao<ProtocolMessage, ProtocolMessageDao>(_mapperProvider);

            messageDao.TypeUrl.Should().Be("cleanurl");
            messageDao.CorrelationId.Should().Be(newGuid.ToString());
            messageDao.PeerId.Port.Should().Be((ushort) peerId.Port);
            messageDao.PeerId.Ip.Should().Be(new IPAddress(peerId.Ip.ToByteArray()).MapToIPv6().ToString());

            var reconverted = messageDao.ToProtoBuff<ProtocolMessageDao, ProtocolMessage>(_mapperProvider);
            reconverted.Should().Be(original);
        }

        [Fact]
        public void ProtocolErrorMessageSignedDao_ProtocolErrorMessageSigned_Should_Be_Convertible()
        {
            var byteRn = new byte[30];
            new Random().NextBytes(byteRn);

            var original = new ProtocolErrorMessage
            {
                CorrelationId = Guid.NewGuid().ToByteString(),
                Signature = new Signature
                {
                    RawBytes = byteRn.ToByteString(),
                    SigningContext = DevNetPeerSigningContext.Instance
                },
                PeerId = PeerIdHelper.GetPeerId("test"),
                Code = 74
            };

            var errorMessageSignedDao = original.ToDao<ProtocolErrorMessage, ProtocolErrorMessageDao>(_mapperProvider);
            var reconverted =
                errorMessageSignedDao.ToProtoBuff<ProtocolErrorMessageDao, ProtocolErrorMessage>(_mapperProvider);
            reconverted.Should().Be(original);
        }

        [Fact]
        public void PeerIdDao_PeerId_Should_Be_Convertible()
        {
            var original = PeerIdHelper.GetPeerId("MyPeerId_Testing");

            var peer = original.ToDao<PeerId, PeerIdDao>(_mapperProvider);
            var reconverted = peer.ToProtoBuff<PeerIdDao, PeerId>(_mapperProvider);
            reconverted.Should().Be(original);
        }

        [Fact]
        public void SigningContextDao_SigningContext_Should_Be_Convertible()
        {
            var byteRn = new byte[30];
            new Random().NextBytes(byteRn);

            var original = new SigningContext
            {
                NetworkType = NetworkType.Devnet,
                SignatureType = SignatureType.TransactionPublic
            };

            var contextDao = original.ToDao<SigningContext, SigningContextDao>(_mapperProvider);
            var reconverted = contextDao.ToProtoBuff<SigningContextDao, SigningContext>(_mapperProvider);
            reconverted.Should().Be(original);
        }

        [Fact]
        public void DeltasDao_Deltas_Should_Be_Convertible()
        {
            var previousHash = _hashProvider.ComputeMultiHash(Encoding.UTF8.GetBytes("previousHash"));

            var original = DeltaHelper.GetDelta(_hashProvider, previousHash);

            var messageDao = original.ToDao<Delta, DeltaDao>(_mapperProvider);
            var reconverted = messageDao.ToProtoBuff<DeltaDao, Delta>(_mapperProvider);
            original.Should().Be(reconverted);
        }

        [Fact]
        public void CandidateDeltaBroadcastDao_CandidateDeltaBroadcast_Should_Be_Convertible()
        {
            var previousHash = _hashProvider.ComputeMultiHash(Encoding.UTF8.GetBytes("previousHash"));
            var hash = _hashProvider.ComputeMultiHash(Encoding.UTF8.GetBytes("anotherHash"));

            var original = new CandidateDeltaBroadcast
            {
                Hash = MultiBase.Decode(hash.ToCid()).ToByteString(),
                ProducerId = PeerIdHelper.GetPeerId("test"),
                PreviousDeltaDfsHash = MultiBase.Decode(previousHash.ToCid()).ToByteString()
            };

            var candidateDeltaBroadcast =
                original.ToDao<CandidateDeltaBroadcast, CandidateDeltaBroadcastDao>(_mapperProvider);
            var reconverted =
                candidateDeltaBroadcast.ToProtoBuff<CandidateDeltaBroadcastDao, CandidateDeltaBroadcast>(
                    _mapperProvider);
            reconverted.Should().Be(original);
        }

        [Fact]
        public void DeltaDfsHashBroadcastDao_DeltaDfsHashBroadcast_Should_Be_Convertible()
        {
            var hash = MultiBase.Decode(_hashProvider.ComputeUtf8MultiHash("this hash").ToCid());
            var previousDfsHash =
                MultiBase.Decode(_hashProvider.ComputeUtf8MultiHash("previousDfsHash").ToCid());

            var original = new DeltaDfsHashBroadcast
            {
                DeltaDfsHash = hash.ToByteString(),
                PreviousDeltaDfsHash = previousDfsHash.ToByteString()
            };

            var contextDao = original.ToDao<DeltaDfsHashBroadcast, DeltaDfsHashBroadcastDao>(_mapperProvider);
            var reconverted = contextDao.ToProtoBuff<DeltaDfsHashBroadcastDao, DeltaDfsHashBroadcast>(_mapperProvider);
            reconverted.Should().Be(original);
        }

        [Fact]
        public void FavouriteDeltaBroadcastDao_FavouriteDeltaBroadcast_Should_Be_Convertible()
        {
            var original = new FavouriteDeltaBroadcast
            {
                Candidate = DeltaHelper.GetCandidateDelta(_hashProvider, producerId: PeerIdHelper.GetPeerId("not me")),
                VoterId = PeerIdHelper.GetPeerId("test")
            };

            var contextDao = original.ToDao<FavouriteDeltaBroadcast, FavouriteDeltaBroadcastDao>(_mapperProvider);
            var reconverted =
                contextDao.ToProtoBuff<FavouriteDeltaBroadcastDao, FavouriteDeltaBroadcast>(_mapperProvider);
            reconverted.Should().Be(original);
        }

        [Fact]
        public void CoinbaseEntryDao_CoinbaseEntry_Should_Be_Convertible()
        {
            var pubKeyBytes = new byte[30];
            new Random().NextBytes(pubKeyBytes);

            var original = new CoinbaseEntry
            {
                ReceiverPublicKey = pubKeyBytes.ToByteString(),
                Amount = 271314.ToUint256ByteString()
            };

            var messageDao = original.ToDao<CoinbaseEntry, CoinbaseEntryDao>(_mapperProvider);
            messageDao.ReceiverPublicKey.Should().Be(pubKeyBytes.KeyToString());

            var reconverted = messageDao.ToProtoBuff<CoinbaseEntryDao, CoinbaseEntry>(_mapperProvider);
            reconverted.Should().Be(original);
        }

        [Fact]
        public void STTransactionEntryDao_STTransactionEntry_Should_Be_Convertible()
        {
            var pubKeyBytes = new byte[30];
            new Random().NextBytes(pubKeyBytes);

            var original = new PublicEntry
            {
                Amount = 8855274.ToUint256ByteString(),
                SenderAddress = pubKeyBytes.ToByteString(),
                Signature = new Signature
                {
                    RawBytes = new byte[] { 0x0 }.ToByteString(),
                    SigningContext = new SigningContext
                    { NetworkType = NetworkType.Devnet, SignatureType = SignatureType.TransactionPublic }
                },
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
            };

            var transactionEntryDao = original.ToDao<PublicEntry, PublicEntryDao>(_mapperProvider);

            transactionEntryDao.SenderAddress.Should().Be(pubKeyBytes.KeyToString());
            transactionEntryDao.Amount.Should().Be(8855274.ToString());

            var reconverted = transactionEntryDao.ToProtoBuff<PublicEntryDao, PublicEntry>(_mapperProvider);
            reconverted.Should().Be(original);
        }

        [Fact]
        public void ConfidentialEntry_And_ConfidentialEntryDao_Should_Be_Convertible()
        {
            var pubKeyBytes = new byte[30];
            var pedersenCommitBytes = new byte[50];
            var rangeProof = new RangeProof();

            var rnd = new Random();
            rnd.NextBytes(pubKeyBytes);
            rnd.NextBytes(pedersenCommitBytes);

            var original = new ConfidentialEntry
            {
                Nonce = ulong.MaxValue,
                SenderPublicKey = pubKeyBytes.ToByteString(),
                TransactionFees = UInt256.Zero.ToUint256ByteString(),
                PedersenCommitment = pedersenCommitBytes.ToByteString(),
                RangeProof = rangeProof
            };

            var transactionEntryDao = original.ToDao<ConfidentialEntry, ConfidentialEntryDao>(_mapperProvider);

            transactionEntryDao.SenderPublicKey.Should().Be(pubKeyBytes.KeyToString());
            transactionEntryDao.Nonce.Should().Be(ulong.MaxValue);
            transactionEntryDao.PedersenCommitment.Should().Be(pedersenCommitBytes.ToByteString().ToBase64());
            transactionEntryDao.RangeProof.Should().Be(rangeProof.ToByteString().ToBase64());

            var reconverted = transactionEntryDao.ToProtoBuff<ConfidentialEntryDao, ConfidentialEntry>(_mapperProvider);
            reconverted.Should().Be(original);
        }

        [Fact]
        public void TransactionBroadcastDao_TransactionBroadcast_Should_Be_Convertible()
        {
            var original = TransactionHelper.GetPublicTransaction();

            var transactionEntryDao = original.ToDao<TransactionBroadcast, TransactionBroadcastDao>(_mapperProvider);
            var reconverted =
                transactionEntryDao.ToProtoBuff<TransactionBroadcastDao, TransactionBroadcast>(_mapperProvider);
            reconverted.Should().Be(original);
        }

        [Fact]
        public void PublicEntryDao_Should_Be_The_Same_When_Converted()
        {
            var pubKeyBytes = new byte[30];
            new Random().NextBytes(pubKeyBytes);
            var original = new PublicEntry
            {
                Amount = new byte[] { 222, 11, 107, 58, 118, 64, 0, 0 }.ToByteString(),
                SenderAddress = pubKeyBytes.ToByteString(),
                Signature = new Signature
                {
                    RawBytes = new byte[] { 0x0 }.ToByteString(),
                    SigningContext = new SigningContext
                    { NetworkType = NetworkType.Devnet, SignatureType = SignatureType.TransactionPublic }
                },
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
            };

            var transactionEntryDao1 = original.ToDao<PublicEntry, PublicEntryDao>(_mapperProvider);
            var hashId1 = transactionEntryDao1.Id;

            var reconverted = transactionEntryDao1.ToProtoBuff<PublicEntryDao, PublicEntry>(_mapperProvider);

            var transactionEntryDao2 = reconverted.ToDao<PublicEntry, PublicEntryDao>(_mapperProvider);
            var hashId2 = transactionEntryDao2.Id;

            hashId1.Should().Be(hashId2);
        }
    }
}
