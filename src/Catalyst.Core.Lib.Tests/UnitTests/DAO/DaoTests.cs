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
using ICSharpCode.SharpZipLib.Tar;
using Multiformats.Hash;
using Multiformats.Hash.Algorithms;
using Xunit;

namespace Catalyst.Core.Lib.Tests.UnitTests.DAO
{
    public class DaoTests
    {
        private readonly IMultihashAlgorithm _hashingAlgorithm = new BLAKE2B_256();

        public DaoTests()
        {
            //var map = new MapperProvider(new IMapperInitializer[]
            //{
            //    new ProtocolMessageDao(),
            //    new CfTransactionEntryDao(),
            //    new CandidateDeltaBroadcastDao(),
            //    new ProtocolErrorMessageSignedDao(), 
            //    new PeerIdDao(), 
            //    new SigningContextDao(),
            //    new DeltaDao(),
            //    new CandidateDeltaBroadcastDao(),
            //    new DeltaDfsHashBroadcastDao(),
            //    new FavouriteDeltaBroadcastDao(),
            //    new CoinbaseEntryDao(),
            //    new StTransactionEntryDao(),
            //    new CfTransactionEntryDao(),
            //    new TransactionBroadcastDao()
            //});
            //map.Start();
        }

        private TDao StartMappingProvider<TDao>() where TDao : IMapperInitializer
        {
            var daoInstance = Activator.CreateInstance<TDao>();
            var map = new MapperProvider(new List<IMapperInitializer>(new IMapperInitializer[] {daoInstance}));
            map.Start();
            return daoInstance;
        }

        [Fact]
        public void ProtocolMessageDao_ProtocolMessage_Should_Be_Convertible()
        {
            var protocolMessageDao = StartMappingProvider<ProtocolMessageDao>();

            var newGuid = Guid.NewGuid();
            var peerId = PeerIdentifierHelper.GetPeerIdentifier("testcontent").PeerId;
            var message = new ProtocolMessage
            {
                CorrelationId = newGuid.ToByteString(),
                TypeUrl = "cleanurl",
                Value = "somecontent".ToUtf8ByteString(),
                PeerId = peerId
            };

            var messageDao = protocolMessageDao.ToDao(message);

            messageDao.TypeUrl.Should().Be("cleanurl");
            messageDao.CorrelationId.Should().Be(newGuid.ToString());
            messageDao.PeerId.Port.Should().Be(BitConverter.ToUInt16(peerId.Port.ToByteArray()));
            messageDao.PeerId.Ip.Should().Be(new IPAddress(peerId.Ip.ToByteArray()).MapToIPv6().ToString());

            var protoBuff = messageDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public void ProtocolErrorMessageSignedDao_ProtocolErrorMessageSigned_Should_Be_Convertible()
        {
            var protocolErrorMessageSignedDao = StartMappingProvider<ProtocolErrorMessageSignedDao>();
            var byteRn = new byte[30];
            new Random().NextBytes(byteRn);

            var message = new ProtocolErrorMessageSigned
            {
                CorrelationId = Guid.NewGuid().ToByteString(),
                Signature = byteRn.ToByteString(),
                PeerId = PeerIdentifierHelper.GetPeerIdentifier("test").PeerId,
                Code = 74
            };

            var errorMessageSignedDao = protocolErrorMessageSignedDao.ToDao(message);
            var protoBuff = errorMessageSignedDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public void PeerIdDao_PeerId_Should_Be_Convertible()
        {
            var peerIdDao = StartMappingProvider<PeerIdDao>();

            var message = PeerIdentifierHelper.GetPeerIdentifier("MyPeerId_Testing").PeerId;

            var peer = peerIdDao.ToDao(message);
            var protoBuff = peer.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public void RangeProofDao_RangeProof_Should_Be_Convertible()
        {
            var peerIdDao = StartMappingProvider<EntryRangeProofDao>();

            var rangeProof = new EntryRangeProof
            {
                A = "a".ToUtf8ByteString(),
                APrime0 = "a prime 0".ToUtf8ByteString(),
                BPrime0 = "b prime 0".ToUtf8ByteString(),
                MU = "mu".ToUtf8ByteString(),
                S = "s".ToUtf8ByteString(),
                T = "t".ToUtf8ByteString(),
                T1 = "t1".ToUtf8ByteString(),
                T2 = "t2".ToUtf8ByteString(),
                TAU = "tau".ToUtf8ByteString()
            };

            var peer = peerIdDao.ToDao(rangeProof);
            var protoBuff = peer.ToProtoBuff();
            rangeProof.Should().Be(protoBuff);
        }

        [Fact]
        public void SigningContextDao_SigningContext_Should_Be_Convertible()
        {
            var signingContextDao = StartMappingProvider<SigningContextDao>();
            var byteRn = new byte[30];
            new Random().NextBytes(byteRn);

            var message = new SigningContext
            {
                Network = Protocol.Common.Network.Devnet,
                SignatureType = SignatureType.TransactionPublic
            };

            var contextDao = signingContextDao.ToDao(message);
            var protoBuff = contextDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public void DeltasDao_Deltas_Should_Be_Convertible()
        {
            //var deltaDao = StartMappingProvider<CandidateDeltaBroadcastDao>();

            //var previousHash = "previousHash".ComputeUtf8Multihash(_hashingAlgorithm).ToBytes();

            //var message = DeltaHelper.GetDelta(previousHash);

            //var messageDao = deltaDao.ToDao(message);
            //var protoBuff = messageDao.ToProtoBuff();
            //message.Should().Be(protoBuff);
        }

        [Fact]
        public void CandidateDeltaBroadcastDao_CandidateDeltaBroadcast_Should_Be_Convertible()
        {
            var candidateDeltaBroadcastDao = StartMappingProvider<CandidateDeltaBroadcastDao>();
            var byteRn = new byte[30];
            new Random().NextBytes(byteRn);

            var previousHash = "previousHash".ComputeUtf8Multihash(_hashingAlgorithm).ToBytes();

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
        public void DeltaDfsHashBroadcastDao_DeltaDfsHashBroadcast_Should_Be_Convertible()
        {
            var deltaDfsHashBroadcastDao = StartMappingProvider<DeltaDfsHashBroadcastDao>();

            var hash = "this hash".ComputeUtf8Multihash(_hashingAlgorithm).ToBytes();
            var previousDfsHash = "previousDfsHash".ComputeUtf8Multihash(_hashingAlgorithm).ToBytes();

            var message = new DeltaDfsHashBroadcast
            {
                DeltaDfsHash = hash.ToByteString(),
                PreviousDeltaDfsHash = previousDfsHash.ToByteString()
            };

            var contextDao = deltaDfsHashBroadcastDao.ToDao(message);
            var protoBuff = contextDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public void FavouriteDeltaBroadcastDao_FavouriteDeltaBroadcast_Should_Be_Convertible()
        {
            var favouriteDeltaBroadcastDao = StartMappingProvider<FavouriteDeltaBroadcastDao>();

            var message = new FavouriteDeltaBroadcast
            {
                Candidate = DeltaHelper.GetCandidateDelta(producerId: PeerIdHelper.GetPeerId("not me")),
                VoterId = PeerIdentifierHelper.GetPeerIdentifier("test").PeerId
            };

            var contextDao = favouriteDeltaBroadcastDao.ToDao(message);
            var protoBuff = contextDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public void CoinbaseEntryDao_CoinbaseEntry_Should_Be_Convertible()
        {
            var coinbaseEntryDao = StartMappingProvider<CoinbaseEntryDao>();
            var pubKeyBytes = new byte[30];
            new Random().NextBytes(pubKeyBytes);

            var message = new CoinbaseEntry
            {
                Version = 415,
                PubKey = pubKeyBytes.ToByteString(),
                Amount = 271314
            };

            var messageDao = coinbaseEntryDao.ToDao(message);
            messageDao.PubKey.Should().Be(pubKeyBytes.KeyToString());

            var protoBuff = messageDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public void STTransactionEntryDao_STTransactionEntry_Should_Be_Convertible()
        {
            var stTransactionEntryDao = StartMappingProvider<StTransactionEntryDao>();
            var pubKeyBytes = new byte[30];
            new Random().NextBytes(pubKeyBytes);

            var message = new STTransactionEntry
            {
                PubKey = pubKeyBytes.ToByteString(),
                Amount = 8855274
            };

            var transactionEntryDao = stTransactionEntryDao.ToDao(message);

            transactionEntryDao.PubKey.Should().Be(pubKeyBytes.KeyToString());
            transactionEntryDao.Amount.Should().Be(8855274);

            var protoBuff = transactionEntryDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public void CFTransactionEntryDao_CFTransactionEntry_Should_Be_Convertible()
        {
            var cfTransactionEntryDao = StartMappingProvider<CfTransactionEntryDao>();

            var pubKeyBytes = new byte[30];
            var pedersenCommitBytes = new byte[50];

            var rnd = new Random();
            rnd.NextBytes(pubKeyBytes);
            rnd.NextBytes(pedersenCommitBytes);

            var message = new CFTransactionEntry
            {
                PubKey = pubKeyBytes.ToByteString(),
                PedersenCommit = pedersenCommitBytes.ToByteString()
            };

            var transactionEntryDao = cfTransactionEntryDao.ToDao(message);

            transactionEntryDao.PubKey.Should().Be(pubKeyBytes.KeyToString());
            transactionEntryDao.PedersenCommit.Should().Be(pedersenCommitBytes.ToByteString().ToBase64());

            var protoBuff = transactionEntryDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public void TransactionBroadcastDao_TransactionBroadcast_Should_Be_Convertible()
        {
            var transactionBroadcastDao = StartMappingProvider<TransactionBroadcastDao>();

            var message = TransactionHelper.GetTransaction();

            var transactionEntryDao = transactionBroadcastDao.ToDao(message);
            var protoBuff = transactionEntryDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }
    }
}
