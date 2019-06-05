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

using System;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using DotNetty.Transport.Channels;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Common.UnitTests.IO.Messaging
{
    public sealed class SignatureHandlerTests
    {
        private readonly IChannelHandlerContext _fakeContext;
        private readonly SignatureHandler _signatureHandler;

        public SignatureHandlerTests()
        {
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _signatureHandler = new SignatureHandler(Substitute.For<IKeySigner>());
        }

        [Fact]
        private void CanFireNextPipelineOnValidSignature()
        {
            var pingRequest = new PingRequest();
            var pid = PeerIdentifierHelper.GetPeerIdentifier("im_a_key");
            var signedPingMessage = pingRequest.ToAnySigned(pid.PeerId, Guid.NewGuid());
            _signatureHandler.ChannelRead(_fakeContext, signedPingMessage);
        }

        // [Fact]
        // private void ClosesChannelOnInvalidSignature()
        // {
        //     
        // }
    }
}
