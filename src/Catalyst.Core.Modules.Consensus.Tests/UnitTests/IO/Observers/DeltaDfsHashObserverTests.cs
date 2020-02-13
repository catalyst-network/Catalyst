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

using System.Text;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Core.Abstractions.Sync;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Modules.Consensus.IO.Observers;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using MultiFormats.Registry;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.Modules.Consensus.Tests.UnitTests.IO.Observers
{
    public sealed class DeltaDfsHashObserverTests
    {
        private readonly IHashProvider _hashProvider;
        private readonly IDeltaHashProvider _deltaHashProvider;
        private readonly IChannelHandlerContext _fakeChannelContext;
        private readonly SyncState _syncState;
        private readonly ILogger _logger;

        public DeltaDfsHashObserverTests()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));
            _deltaHashProvider = Substitute.For<IDeltaHashProvider>();
            _fakeChannelContext = Substitute.For<IChannelHandlerContext>();
            _syncState = new SyncState {IsSynchronized = true, IsRunning = true};
            _logger = Substitute.For<ILogger>();
        }

        [Fact]
        public void HandleBroadcast_Should_Cast_Hashes_To_Multihash_And_Try_Update()
        {
            var newHash = _hashProvider.ComputeUtf8MultiHash("newHash").ToCid();
            var prevHash = _hashProvider.ComputeUtf8MultiHash("prevHash").ToCid();
            var receivedMessage = PrepareReceivedMessage(newHash.ToArray(), prevHash.ToArray());

            var deltaDfsHashObserver = new DeltaDfsHashObserver(_deltaHashProvider, _syncState, _logger);

            deltaDfsHashObserver.HandleBroadcast(receivedMessage);

            _deltaHashProvider.Received(1).TryUpdateLatestHash(prevHash, newHash);
        }

        [Fact]
        public void HandleBroadcast_Should_Not_Try_Update_Invalid_Hash()
        {
            var invalidNewHash = Encoding.UTF8.GetBytes("invalid hash");
            var prevHash = _hashProvider.ComputeUtf8MultiHash("prevHash").ToCid();
            var receivedMessage = PrepareReceivedMessage(invalidNewHash, prevHash.ToArray());

            var deltaDfsHashObserver = new DeltaDfsHashObserver(_deltaHashProvider, _syncState, _logger);

            deltaDfsHashObserver.HandleBroadcast(receivedMessage);

            _deltaHashProvider.DidNotReceiveWithAnyArgs().TryUpdateLatestHash(default, default);
        }

        private IObserverDto<ProtocolMessage> PrepareReceivedMessage(byte[] newHash, byte[] prevHash)
        {
            var message = new DeltaDfsHashBroadcast
            {
                DeltaDfsHash = newHash.ToByteString(),
                PreviousDeltaDfsHash = prevHash.ToByteString()
            };

            var receivedMessage = new ObserverDto(_fakeChannelContext,
                message.ToProtocolMessage(PeerIdHelper.GetPeerId()));
            return receivedMessage;
        }
    }
}
