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

using Catalyst.Abstractions.IO.Handlers;
using Catalyst.Abstractions.Rpc.Authentication;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Handlers;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels.Embedded;
using NSubstitute;
using Xunit;

namespace Catalyst.Core.Modules.Authentication.Tests
{
    public sealed class AuthenticationHandlerTests
    {
        private readonly IAuthenticationStrategy _authenticationStrategy;
        private readonly EmbeddedChannel _serverChannel;
        private readonly IObservableServiceHandler _testObservableServiceHandler;
        private readonly ProtocolMessage _signedMessage;

        public AuthenticationHandlerTests()
        {
            _testObservableServiceHandler = Substitute.For<IObservableServiceHandler>();
            _authenticationStrategy = Substitute.For<IAuthenticationStrategy>();
            _serverChannel = new EmbeddedChannel(new AuthenticationHandler(_authenticationStrategy), _testObservableServiceHandler);

            var senderId = PeerIdHelper.GetPeerId("Test");
            _signedMessage = new GetPeerListRequest()
               .ToProtocolMessage(senderId)
               .ToSignedProtocolMessage(senderId, new byte[Ffi.SignatureLength]);
        }

        [Fact]
        public void Can_Block_Pipeline_Non_Authorized_Node_Operator()
        {
            _authenticationStrategy.Authenticate(Arg.Any<PeerId>()).Returns(false);

            _serverChannel.WriteInbound(_signedMessage);
            _authenticationStrategy.ReceivedWithAnyArgs(1).Authenticate(null);
            _testObservableServiceHandler.DidNotReceiveWithAnyArgs().ChannelRead(null, null);
        }

        [Fact]
        public void Can_Continue_Pipeline_On_Authorized_Node_Operator()
        {
            _authenticationStrategy.Authenticate(Arg.Any<PeerId>()).Returns(true);

            _serverChannel.WriteInbound(_signedMessage);
            _authenticationStrategy.ReceivedWithAnyArgs(1).Authenticate(null);
            _testObservableServiceHandler.ReceivedWithAnyArgs(1).ChannelRead(null, null);
        }
    }
}
