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
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Rpc.Authentication;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Handlers;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels.Embedded;
using Google.Protobuf;
using NSubstitute;
using Xunit;

namespace Catalyst.Core.UnitTests.Rpc.Authentication
{
    public sealed class AuthenticationHandlerTests
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
                CorrelationId.GenerateCorrelationId());
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
                CorrelationId.GenerateCorrelationId());
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
