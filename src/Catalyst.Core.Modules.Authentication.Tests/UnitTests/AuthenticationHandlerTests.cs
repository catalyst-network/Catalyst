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

using Catalyst.Abstractions.IO.Handlers;
using Catalyst.Abstractions.Rpc.Authentication;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Modules.Network.Dotnetty.IO.Handlers;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels.Embedded;
using MultiFormats;
using NSubstitute;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Authentication.Tests.UnitTests
{
    public sealed class AuthenticationHandlerTests
    {
        private IAuthenticationStrategy _authenticationStrategy;
        private EmbeddedChannel _serverChannel;
        private IObservableServiceHandler<ProtocolMessage> _testObservableServiceHandler;
        private ProtocolMessage _signedMessage;

        [SetUp]
        public void Init()
        {
            _testObservableServiceHandler = Substitute.For<IObservableServiceHandler<ProtocolMessage>>();
            _authenticationStrategy = Substitute.For<IAuthenticationStrategy>();
            _serverChannel = new EmbeddedChannel(new AuthenticationHandler(_authenticationStrategy), _testObservableServiceHandler);

            var senderAddress = MultiAddressHelper.GetAddress("Test");
            _signedMessage = new GetPeerListRequest()
               .ToProtocolMessage(senderAddress)
               .ToSignedProtocolMessage(senderAddress, new byte[new FfiWrapper().SignatureLength]);
        }

        [Test]
        public void Can_Block_Pipeline_Non_Authorized_Node_Operator()
        {
            _authenticationStrategy.Authenticate(Arg.Any<MultiAddress>()).Returns(false);

            _serverChannel.WriteInbound(_signedMessage);
            _authenticationStrategy.ReceivedWithAnyArgs(1).Authenticate(null);
            _testObservableServiceHandler.DidNotReceiveWithAnyArgs().ChannelRead(null, null);
        }

        [Test]
        public void Can_Continue_Pipeline_On_Authorized_Node_Operator()
        {
            _authenticationStrategy.Authenticate(Arg.Any<MultiAddress>()).Returns(true);

            _serverChannel.WriteInbound(_signedMessage);
            _authenticationStrategy.ReceivedWithAnyArgs(1).Authenticate(null);
            _testObservableServiceHandler.ReceivedWithAnyArgs(1).ChannelRead(null, null);
        }
    }
}
