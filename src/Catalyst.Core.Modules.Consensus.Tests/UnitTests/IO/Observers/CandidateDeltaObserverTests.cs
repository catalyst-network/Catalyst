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
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.Service;
using Catalyst.Core.Modules.Consensus.IO.Observers;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Lib.P2P;
using MultiFormats.Registry;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.Modules.Consensus.Tests.UnitTests.IO.Observers
{
    public sealed class CandidateDeltaObserverTests
    {
        private readonly IDeltaVoter _deltaVoter;
        private readonly IChannelHandlerContext _fakeChannelContext;
        private readonly Cid _newHash;
        private readonly Cid _prevHash;
        private readonly PeerId _producerId;
        private readonly CandidateDeltaObserver _candidateDeltaObserver;

        public CandidateDeltaObserverTests()
        {
            var hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));
            _deltaVoter = Substitute.For<IDeltaVoter>();
            _fakeChannelContext = Substitute.For<IChannelHandlerContext>();
            var logger = Substitute.For<ILogger>();
            _newHash = hashProvider.ComputeUtf8MultiHash("newHash").ToCid();
            _prevHash = hashProvider.ComputeUtf8MultiHash("prevHash").ToCid();
            _producerId = PeerIdHelper.GetPeerId("candidate delta producer");

            var deltaIndexService = Substitute.For<IDeltaIndexService>();
            deltaIndexService.LatestDeltaIndex().Returns(new Lib.DAO.Ledger.DeltaIndexDao() { Cid = _prevHash, Height = 0 });
            _candidateDeltaObserver = new CandidateDeltaObserver(_deltaVoter, deltaIndexService, hashProvider, logger);
        }

        [Fact]
        public void HandleBroadcast_Should_Cast_Hashes_To_Multihash_And_Send_To_Voter()
        {
            var receivedMessage = PrepareReceivedMessage(_newHash.ToArray(), _prevHash.ToArray(), _producerId);

            _candidateDeltaObserver.HandleBroadcast(receivedMessage);

            _deltaVoter.Received(1).OnNext(Arg.Is<CandidateDeltaBroadcast>(c =>
                c.Hash.SequenceEqual(_newHash.ToArray().ToByteString())
             && c.PreviousDeltaDfsHash.Equals(_prevHash.ToArray().ToByteString())
             && c.ProducerId.Equals(_producerId)));
        }

        [Fact]
        public void HandleBroadcast_Should_Not_Try_Forwarding_Invalid_Hash()
        {
            var invalidNewHash = Encoding.UTF8.GetBytes("invalid hash");
            var receivedMessage = PrepareReceivedMessage(invalidNewHash, _prevHash.ToArray(), _producerId);

            _candidateDeltaObserver.HandleBroadcast(receivedMessage);

            _deltaVoter.DidNotReceiveWithAnyArgs().OnNext(default);
        }

        [Fact]
        public void HandleBroadcast_Should_Not_Try_Forwarding_Invalid_PreviousHash()
        {
            var invalidPreviousHash = Encoding.UTF8.GetBytes("invalid previous hash");
            var receivedMessage = PrepareReceivedMessage(_newHash.ToArray(), invalidPreviousHash, _producerId);

            _candidateDeltaObserver.HandleBroadcast(receivedMessage);

            _deltaVoter.DidNotReceiveWithAnyArgs().OnNext(default);
        }

        private IObserverDto<ProtocolMessage> PrepareReceivedMessage(byte[] newHash,
            byte[] prevHash,
            PeerId producerId)
        {
            var message = new CandidateDeltaBroadcast
            {
                Hash = newHash.ToByteString(),
                PreviousDeltaDfsHash = prevHash.ToByteString(),
                ProducerId = producerId
            };

            var receivedMessage = new ObserverDto(_fakeChannelContext,
                message.ToProtocolMessage(PeerIdHelper.GetPeerId()));
            return receivedMessage;
        }
    }
}
