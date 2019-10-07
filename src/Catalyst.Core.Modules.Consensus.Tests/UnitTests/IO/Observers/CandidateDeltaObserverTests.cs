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

using System.Linq;
using System.Text;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Modules.Consensus.IO.Observers;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Ipfs;
using Ipfs.Registry;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.Modules.Consensus.Tests.UnitTests.IO.Observers
{
    public sealed class CandidateDeltaObserverTests
    {
        private readonly IHashProvider _hashProvider;
        private readonly IDeltaVoter _deltaVoter;
        private readonly IChannelHandlerContext _fakeChannelContext;
        private readonly MultiHash _newHash;
        private readonly MultiHash _prevHash;
        private readonly PeerId _producerId;
        private readonly CandidateDeltaObserver _candidateDeltaObserver;

        public CandidateDeltaObserverTests()
        {
            var hashingAlgorithm = HashingAlgorithm.GetAlgorithmMetadata("blake2b-256");
            _hashProvider = new HashProvider(hashingAlgorithm);
            _deltaVoter = Substitute.For<IDeltaVoter>();
            _fakeChannelContext = Substitute.For<IChannelHandlerContext>();
            var logger = Substitute.For<ILogger>();
            _newHash = _hashProvider.ComputeUtf8MultiHash("newHash");
            _prevHash = _hashProvider.ComputeUtf8MultiHash("prevHash");
            _producerId = PeerIdHelper.GetPeerId("candidate delta producer");
            _candidateDeltaObserver = new CandidateDeltaObserver(_deltaVoter, _hashProvider, logger);
        }

        [Fact]
        public void HandleBroadcast_Should_Cast_Hashes_To_Multihash_And_Send_To_Voter()
        {
            var receivedMessage = PrepareReceivedMessage(_newHash, _prevHash, _producerId);

            _candidateDeltaObserver.HandleBroadcast(receivedMessage);

            _deltaVoter.Received(1).OnNext(Arg.Is<CandidateDeltaBroadcast>(c =>
                c.Hash.SequenceEqual(_newHash.ToArray().ToByteString())
             && c.PreviousDeltaDfsHash.Equals(_prevHash.ToArray().ToByteString())
             && c.ProducerId.Equals(_producerId)));
        }

        [Fact]
        public void HandleBroadcast_Should_Not_Try_Forwarding_Invalid_Hash()
        {
            var invalidNewHash = _hashProvider.Cast(Encoding.UTF8.GetBytes("invalid hash"));
            var receivedMessage = PrepareReceivedMessage(invalidNewHash, _prevHash, _producerId);

            _candidateDeltaObserver.HandleBroadcast(receivedMessage);

            _deltaVoter.DidNotReceiveWithAnyArgs().OnNext(default);
        }

        [Fact]
        public void HandleBroadcast_Should_Not_Try_Forwarding_Invalid_PreviousHash()
        {
            var invalidPreviousHash = _hashProvider.Cast(Encoding.UTF8.GetBytes("invalid previous hash"));
            var receivedMessage = PrepareReceivedMessage(_newHash, invalidPreviousHash, _producerId);

            _candidateDeltaObserver.HandleBroadcast(receivedMessage);

            _deltaVoter.DidNotReceiveWithAnyArgs().OnNext(default);
        }

        private IObserverDto<ProtocolMessage> PrepareReceivedMessage(MultiHash newHash,
            MultiHash prevHash,
            PeerId producerId)
        {
            var message = new CandidateDeltaBroadcast
            {
                Hash = newHash.ToArray().ToByteString(),
                PreviousDeltaDfsHash = prevHash.ToArray().ToByteString(),
                ProducerId = producerId
            };

            var receivedMessage = new ObserverDto(_fakeChannelContext,
                message.ToProtocolMessage(PeerIdHelper.GetPeerId()));
            return receivedMessage;
        }
    }
}
