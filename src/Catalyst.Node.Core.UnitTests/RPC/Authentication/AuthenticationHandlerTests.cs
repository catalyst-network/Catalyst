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

using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Handlers;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc.Authentication;
using Catalyst.Common.IO.Handlers;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels.Embedded;
using NSubstitute;
using System;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Cryptography.BulletProofs.Wrapper.Types;
using Catalyst.Protocol.Common;
using Google.Protobuf;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.RPC.Authentication
{
    public class AuthenticationHandlerTests
    {
        private readonly IAuthenticationStrategy _authenticationStrategy;
        private readonly EmbeddedChannel _serverChannel;
        private readonly IObservableServiceHandler _testObservableServiceHandler;

        public AuthenticationHandlerTests()
        {
            _testObservableServiceHandler = Substitute.For<IObservableServiceHandler>();
            _authenticationStrategy = Substitute.For<IAuthenticationStrategy>();
            _serverChannel = new EmbeddedChannel(new AuthenticationHandler(_authenticationStrategy), _testObservableServiceHandler);
        }

        [Fact]
        public void Can_Block_Pipeline_Non_Authorized_Node_Operator()
        {
            _authenticationStrategy.Authenticate(Arg.Any<IPeerIdentifier>()).Returns(false);

            var request = new GetPeerListRequest().ToProtocolMessage(PeerIdHelper.GetPeerId("Test"),
                Guid.NewGuid());
            var signedMessage = new ProtocolMessageSigned
            {
                Message = request,
                Signature = ByteString.CopyFrom(new byte[64])
            };

            _serverChannel.WriteInbound(signedMessage);
            _authenticationStrategy.ReceivedWithAnyArgs(1).Authenticate(null);
            _testObservableServiceHandler.DidNotReceiveWithAnyArgs().ChannelRead(null, null);
        }

        [Fact]
        public void Can_Continue_Pipeline_On_Authorized_Node_Operator()
        {
            _authenticationStrategy.Authenticate(Arg.Any<IPeerIdentifier>()).Returns(true);

            var request = new GetPeerListRequest().ToProtocolMessage(PeerIdHelper.GetPeerId("Test"),
                Guid.NewGuid());
            var signedMessage = new ProtocolMessageSigned
            {
                Message = request,
                Signature = ByteString.CopyFrom(new byte[64])
            };

            _serverChannel.WriteInbound(signedMessage);
            _authenticationStrategy.ReceivedWithAnyArgs(1).Authenticate(null);
            _testObservableServiceHandler.ReceivedWithAnyArgs(1).ChannelRead(null, null);
        }
    }
}
