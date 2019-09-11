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
using Catalyst.Abstractions.DAO;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Deltas;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Transaction;
using Catalyst.TestUtils;
using FluentAssertions;
using Multiformats.Hash.Algorithms;
using Xunit;

namespace Catalyst.Core.Lib.Tests.UnitTests.DAO
{
    public class DaoTests
    {
        public DaoTests()
        {
            var map = new MapperProvider(new IMapperInitializer[]
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
                new TransactionBroadcastDao()
            });
            map.Start();
        }

        [Fact]
        public void ProtocolMessageDao_ProtocolMessage_Should_Be_Convertible()
        {
            var protocolMessageDao = new ProtocolMessageDao();

            var message = new ProtocolMessage
            {
                CorrelationId = Guid.NewGuid().ToByteString(),
                TypeUrl = "cleanurl",
                Value = "somecontent".ToUtf8ByteString(),
                PeerId = PeerIdentifierHelper.GetPeerIdentifier("testcontent").PeerId
            };

            var messageDao = protocolMessageDao.ToDao(message);
            var protoBuff = messageDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public void ProtocolErrorMessageSignedDao_ProtocolErrorMessageSigned_Should_Be_Convertible()
        {
            var protocolErrorMessageSignedDao = new ProtocolErrorMessageSignedDao();
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
            var peerIdDao = new PeerIdDao();

            var message = PeerIdentifierHelper.GetPeerIdentifier("MyPeerId_Testing").PeerId;

            var peer = peerIdDao.ToDao(message);
            var protoBuff = peer.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public void SigningContextDao_SigningContext_Should_Be_Convertible()
        {
            var signingContextDao = new SigningContextDao();
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
            var deltaDao = new DeltaDao();

            var previousHash = "previousHash".ComputeUtf8Multihash(new ID()).ToBytes();

            var message = DeltaHelper.GetDelta(previousHash);

            var messageDao = deltaDao.ToDao(message);
            var protoBuff = messageDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public void CandidateDeltaBroadcastDao_CandidateDeltaBroadcast_Should_Be_Convertible()
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
        public void DeltaDfsHashBroadcastDao_DeltaDfsHashBroadcast_Should_Be_Convertible()
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
        public void FavouriteDeltaBroadcastDao_FavouriteDeltaBroadcast_Should_Be_Convertible()
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

        [Fact]
        public void CoinbaseEntryDao_CoinbaseEntry_Should_Be_Convertible()
        {
            var coinbaseEntryDao = new CoinbaseEntryDao();
            var byteRn = new byte[30];
            new Random().NextBytes(byteRn);

            var message = new CoinbaseEntry
            {
                Version = 415,
                PubKey = byteRn.ToByteString(),
                Amount = 271314
            };

            var messageDao = coinbaseEntryDao.ToDao(message);
            var protoBuff = messageDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public void STTransactionEntryDao_STTransactionEntry_Should_Be_Convertible()
        {
            var stTransactionEntryDao = new StTransactionEntryDao();
            var byteRn = new byte[30];
            new Random().NextBytes(byteRn);

            var message = new STTransactionEntry
            {
                PubKey = byteRn.ToByteString(),
                Amount = 8855274
            };

            var transactionEntryDao = stTransactionEntryDao.ToDao(message);
            var protoBuff = transactionEntryDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public void CFTransactionEntryDao_CFTransactionEntry_Should_Be_Convertible()
        {
            var cfTransactionEntryDao = new CfTransactionEntryDao();
            var byteRn = new byte[30];
            var pedersenCommitBytes = new byte[50];

            var rnd = new Random();
            rnd.NextBytes(byteRn);
            rnd.NextBytes(pedersenCommitBytes);

            var message = new CFTransactionEntry
            {
                PubKey = byteRn.ToByteString(),
                PedersenCommit = pedersenCommitBytes.ToByteString()
            };

            var transactionEntryDao = cfTransactionEntryDao.ToDao(message);
            var protoBuff = transactionEntryDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public void TransactionBroadcastDao_TransactionBroadcast_Should_Be_Convertible()
        {
            var transactionBroadcastDao = new TransactionBroadcastDao();

            var message = TransactionHelper.GetTransaction();

            var transactionEntryDao = transactionBroadcastDao.ToDao(message);
            var protoBuff = transactionEntryDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }
    }
}
