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
using Catalyst.Core.IO.Handlers;
using Catalyst.Protocol.Common;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using NSubstitute;
using Xunit;

namespace Catalyst.Core.UnitTests.IO.Handlers
{
    public class PeerIdValidationHandlerTests
    {
        private readonly IPeerIdValidator _peerIdValidator;
        private readonly PeerIdValidationHandler _peerIdValidationHandler;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly ProtocolMessageSigned _message;

        public PeerIdValidationHandlerTests()
        {
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _peerIdValidator = Substitute.For<IPeerIdValidator>();
            _peerIdValidationHandler = new PeerIdValidationHandler(_peerIdValidator);
            _message = new ProtocolMessageSigned
            {
                Message = new ProtocolMessage {PeerId = PeerIdHelper.GetPeerId("Test")}
            };
        }

        [Fact]
        public void Can_Stop_Next_Pipeline_On_Invalid_Peer()
        {
            _peerIdValidator.ValidatePeerIdFormat(Arg.Any<PeerId>()).Returns(false);
            _peerIdValidationHandler.ChannelRead(_fakeContext, _message);
            _fakeContext.DidNotReceiveWithAnyArgs().FireChannelRead(Arg.Any<object>());
        }

        [Fact]
        public void Can_Continue_Next_Pipeline_On_Valid_Peer()
        {
            _peerIdValidator.ValidatePeerIdFormat(Arg.Any<PeerId>()).Returns(true);
            _peerIdValidationHandler.ChannelRead(_fakeContext, _message);
            _fakeContext.ReceivedWithAnyArgs(1).FireChannelRead(Arg.Any<object>());
        }
    }
}
