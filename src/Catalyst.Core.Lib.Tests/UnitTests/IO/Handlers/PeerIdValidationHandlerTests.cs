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

using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Handlers;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using NSubstitute;
using NUnit.Framework;

namespace Catalyst.Core.Lib.Tests.UnitTests.IO.Handlers
{
    public class PeerIdValidationHandlerTests
    {
        private readonly IPeerIdValidator _peerIdValidator;
        private readonly PeerIdValidationHandler _peerIdValidationHandler;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly ProtocolMessage _message;

        public PeerIdValidationHandlerTests()
        {
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _peerIdValidator = Substitute.For<IPeerIdValidator>();
            _peerIdValidationHandler = new PeerIdValidationHandler(_peerIdValidator);

            _message = new PingRequest().ToProtocolMessage(PeerIdHelper.GetPeerId("Test"))
               .ToProtocolMessage(PeerIdHelper.GetPeerId("Test"));
        }

        [Test]
        public void Can_Stop_Next_Pipeline_On_Invalid_Peer()
        {
            _peerIdValidator.ValidatePeerIdFormat(Arg.Any<PeerId>()).Returns(false);
            _peerIdValidationHandler.ChannelRead(_fakeContext, _message);
            _fakeContext.DidNotReceiveWithAnyArgs().FireChannelRead(Arg.Any<object>());
        }

        [Test]
        public void Can_Continue_Next_Pipeline_On_Valid_Peer()
        {
            _peerIdValidator.ValidatePeerIdFormat(Arg.Any<PeerId>()).Returns(true);
            _peerIdValidationHandler.ChannelRead(_fakeContext, _message);
            _fakeContext.ReceivedWithAnyArgs(1).FireChannelRead(Arg.Any<object>());
        }
    }
}
