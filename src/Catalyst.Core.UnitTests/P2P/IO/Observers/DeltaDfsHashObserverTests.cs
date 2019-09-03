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
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Dto;
using Catalyst.Core.P2P.IO.Observers;
using Catalyst.Core.Util;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Deltas;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Multiformats.Hash;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.P2P.IO.Observers
{
    public sealed class DeltaDfsHashObserverTests
    {
        private readonly IDeltaHashProvider _deltaHashProvider;
        private readonly IChannelHandlerContext _fakeChannelContext;
        private readonly ILogger _logger;

        public DeltaDfsHashObserverTests()
        {
            _deltaHashProvider = Substitute.For<IDeltaHashProvider>();
            _fakeChannelContext = Substitute.For<IChannelHandlerContext>();
            _logger = Substitute.For<ILogger>();
        }

        [Fact]
        public void HandleBroadcast_Should_Cast_Hashes_To_Multihash_And_Try_Update()
        {
            var newHash = Multihash.Sum(HashType.ID, Encoding.UTF8.GetBytes("newHash"));
            var prevHash = Multihash.Sum(HashType.ID, Encoding.UTF8.GetBytes("prevHash"));
            var receivedMessage = PrepareReceivedMessage(newHash, prevHash);

            var deltaDfsHashObserver = new DeltaDfsHashObserver(_deltaHashProvider, _logger);

            deltaDfsHashObserver.HandleBroadcast(receivedMessage);

            _deltaHashProvider.Received(1).TryUpdateLatestHash(prevHash, newHash);
        }

        [Fact]
        public void HandleBroadcast_Should_Not_Try_Update_Invalid_Hash()
        {
            var invalidNewHash = Encoding.UTF8.GetBytes("invalid hash");
            var prevHash = Multihash.Sum(HashType.ID, Encoding.UTF8.GetBytes("prevHash"));
            var receivedMessage = PrepareReceivedMessage(invalidNewHash, prevHash);

            var deltaDfsHashObserver = new DeltaDfsHashObserver(_deltaHashProvider, _logger);

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
