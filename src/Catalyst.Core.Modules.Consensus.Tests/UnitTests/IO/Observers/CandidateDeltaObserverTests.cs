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

using System.Linq;
using System.Text;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Modules.Consensus.IO.Observers;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using MultiFormats.Registry;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using Google.Protobuf;
using System.Collections.Generic;
using Lib.P2P;
using MultiFormats;

namespace Catalyst.Core.Modules.Consensus.Tests.UnitTests.IO.Observers
{
    public sealed class CandidateDeltaObserverTests
    {
        private IDeltaVoter _deltaVoter;
        private Cid _newHash;
        private Cid _prevHash;
        private MultiAddress _producer;
        private CandidateDeltaObserver _candidateDeltaObserver;

        [SetUp]
        public void Init()
        {
            HashProvider hashProvider = new(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));
            _deltaVoter = Substitute.For<IDeltaVoter>();
            var logger = Substitute.For<ILogger>();
            _newHash = hashProvider.ComputeUtf8MultiHash("newHash").ToCid();
            _prevHash = hashProvider.ComputeUtf8MultiHash("prevHash").ToCid();
            _producer = MultiAddressHelper.GetAddress("candidate delta producer");

            var peerRepository = Substitute.For<IPeerRepository>();
            peerRepository.GetPoaPeersByPublicKey(Arg.Any<string>()).Returns(new List<Lib.P2P.Models.Peer> { new Lib.P2P.Models.Peer() });

            _candidateDeltaObserver = new CandidateDeltaObserver(_deltaVoter, peerRepository, hashProvider, logger);
        }

        [Test]
        public void HandleBroadcast_Should_Cast_Hashes_To_Multihash_And_Send_To_Voter()
        {
            var receivedMessage = PrepareReceivedMessage(_newHash.ToArray(), _prevHash.ToArray(), _producer);

            _candidateDeltaObserver.HandleBroadcast(receivedMessage);

            _deltaVoter.Received(1).OnNext(Arg.Is<CandidateDeltaBroadcast>(c =>
                c.Hash.SequenceEqual(_newHash.ToArray().ToByteString())
             && c.PreviousDeltaDfsHash.Equals(_prevHash.ToArray().ToByteString())
             && c.Producer == _producer.GetKvmAddressByteString()));
        }

        [Test]
        public void HandleBroadcast_Should_Not_Try_Forwarding_Invalid_Hash()
        {
            var invalidNewHash = Encoding.UTF8.GetBytes("invalid hash");
            var receivedMessage = PrepareReceivedMessage(invalidNewHash, _prevHash.ToArray(), _producer);

            _candidateDeltaObserver.HandleBroadcast(receivedMessage);

            _deltaVoter.DidNotReceiveWithAnyArgs().OnNext(default);
        }

        [Test]
        public void HandleBroadcast_Should_Not_Try_Forwarding_Invalid_PreviousHash()
        {
            var invalidPreviousHash = Encoding.UTF8.GetBytes("invalid previous hash");
            var receivedMessage = PrepareReceivedMessage(_newHash.ToArray(), invalidPreviousHash, _producer);

            _candidateDeltaObserver.HandleBroadcast(receivedMessage);

            _deltaVoter.DidNotReceiveWithAnyArgs().OnNext(default);
        }

        private ProtocolMessage PrepareReceivedMessage(byte[] newHash,
            byte[] prevHash,
            MultiAddress producer)
        {
            var message = new CandidateDeltaBroadcast
            {
                Hash = newHash.ToByteString(),
                PreviousDeltaDfsHash = prevHash.ToByteString(),
                Producer = producer.GetKvmAddressByteString()
            };

            return message.ToProtocolMessage(MultiAddressHelper.GetAddress());
        }
    }
}
