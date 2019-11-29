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
using System.Linq;
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
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Network;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Catalyst.Protocol.Transaction;
using Catalyst.TestUtils;
using Catalyst.TestUtils.Protocol;
using FluentAssertions;
using Nethermind.Dirichlet.Numerics;
using TheDotNetLeague.MultiFormats.MultiBase;
using TheDotNetLeague.MultiFormats.MultiHash;
using Xunit;
using CandidateDeltaBroadcast = Catalyst.Protocol.Wire.CandidateDeltaBroadcast;

namespace Catalyst.Core.Lib.Tests.UnitTests.DAO
{
    public class DaoTests
    {
        private readonly IMapperInitializer[] _initialisers;
        private readonly HashProvider _hashProvider;
        private MapperProvider _mapperProvider;

        public DaoTests()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));

            _initialisers = new IMapperInitializer[]
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
                new PublicEntryMapperInitialiser(),
                new ConfidentialEntryMapperInitialiser(),
                new TransactionBroadcastMapperInitialiser(),
                new SignatureMapperInitialiser(),
                new BaseEntryMapperInitialiser(), 
            };

            _mapperProvider = new MapperProvider(_initialisers);
        }

        // ReSharper disable once UnusedMember.Local
        private TDao GetMapper<TDao>() where TDao : IMapperInitializer => _initialisers.OfType<TDao>().First();
        
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
            var reconverted = errorMessageSignedDao.ToProtoBuff<ProtocolErrorMessageDao, ProtocolErrorMessage>(_mapperProvider);
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
        public void BaseEntryDao_And_BaseEntry_Should_Be_Convertible()
        {
            var original = new BaseEntry    
            {
                ReceiverPublicKey = "hello".ToUtf8ByteString(),
                SenderPublicKey = "bye bye".ToUtf8ByteString(),
                TransactionFees = UInt256.MaxValue.ToUint256ByteString()
            };

            var contextDao = original.ToDao<BaseEntry, BaseEntryDao>(_mapperProvider);
            var reconverted = contextDao.ToProtoBuff<BaseEntryDao, BaseEntry>(_mapperProvider);
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
                Hash = MultiBase.Decode(CidHelper.CreateCid(hash)).ToByteString(),
                ProducerId = PeerIdHelper.GetPeerId("test"),
                PreviousDeltaDfsHash = MultiBase.Decode(CidHelper.CreateCid(previousHash)).ToByteString()
            };

            var candidateDeltaBroadcast = original.ToDao<CandidateDeltaBroadcast, CandidateDeltaBroadcastDao>(_mapperProvider);
            var reconverted = candidateDeltaBroadcast.ToProtoBuff<CandidateDeltaBroadcastDao, CandidateDeltaBroadcast>(_mapperProvider);
            reconverted.Should().Be(original);
        }

        [Fact]
        public void DeltaDfsHashBroadcastDao_DeltaDfsHashBroadcast_Should_Be_Convertible()
        {
            var hash = MultiBase.Decode(CidHelper.CreateCid(_hashProvider.ComputeUtf8MultiHash("this hash")));
            var previousDfsHash = MultiBase.Decode(CidHelper.CreateCid(_hashProvider.ComputeUtf8MultiHash("previousDfsHash")));

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
            var reconverted = contextDao.ToProtoBuff<FavouriteDeltaBroadcastDao, FavouriteDeltaBroadcast>(_mapperProvider);
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
                Base = new BaseEntry
                {
                    SenderPublicKey = pubKeyBytes.ToByteString(),
                    TransactionFees = UInt256.Zero.ToUint256ByteString()
                }
            };

            var transactionEntryDao = original.ToDao<PublicEntry, PublicEntryDao>(_mapperProvider);

            transactionEntryDao.Base.SenderPublicKey.Should().Be(pubKeyBytes.KeyToString());
            transactionEntryDao.Amount.Should().Be(8855274.ToString());

            var reconverted = transactionEntryDao.ToProtoBuff<PublicEntryDao, PublicEntry>(_mapperProvider);
            reconverted.Base.TransactionFees.ToUInt256().Should().Be(UInt256.Zero);
            reconverted.Should().Be(original);
        }

        [Fact]
        public void ConfidentialEntry_And_ConfidentialEntryDao_Should_Be_Convertible()
        {
            var pubKeyBytes = new byte[30];
            var pedersenCommitBytes = new byte[50];
            var rangeProofBytes = new byte[50];

            var rnd = new Random();
            rnd.NextBytes(pubKeyBytes);
            rnd.NextBytes(pedersenCommitBytes);
            rnd.NextBytes(rangeProofBytes);

            var original = new ConfidentialEntry
            {
                Base = new BaseEntry
                {
                    Nonce = ulong.MaxValue,
                    SenderPublicKey = pubKeyBytes.ToByteString(),
                    TransactionFees = UInt256.Zero.ToUint256ByteString()
                },
                PedersenCommitment = pedersenCommitBytes.ToByteString(),
                RangeProof = rangeProofBytes.ToByteString()
            };

            var transactionEntryDao = original.ToDao<ConfidentialEntry, ConfidentialEntryDao>(_mapperProvider);

            transactionEntryDao.Base.SenderPublicKey.Should().Be(pubKeyBytes.KeyToString());
            transactionEntryDao.Base.Nonce.Should().Be(ulong.MaxValue);
            transactionEntryDao.PedersenCommitment.Should().Be(pedersenCommitBytes.ToByteString().ToBase64());
            transactionEntryDao.RangeProof.Should().Be(rangeProofBytes.ToByteString().ToBase64());

            var reconverted = transactionEntryDao.ToProtoBuff<ConfidentialEntryDao, ConfidentialEntry>(_mapperProvider);
            reconverted.Should().Be(original);
        }

        [Fact]
        public void TransactionBroadcastDao_TransactionBroadcast_Should_Be_Convertible()
        {
            var original = TransactionHelper.GetPublicTransaction();

            var transactionEntryDao = original.ToDao<TransactionBroadcast, TransactionBroadcastDao>(_mapperProvider);
            var reconverted = transactionEntryDao.ToProtoBuff<TransactionBroadcastDao, TransactionBroadcast>(_mapperProvider);
            reconverted.Should().Be(original);
        }
    }
}
