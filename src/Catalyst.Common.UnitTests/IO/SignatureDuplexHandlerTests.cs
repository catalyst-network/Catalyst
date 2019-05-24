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

using System.Net;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.IO.Inbound;
using Catalyst.Cryptography.BulletProofs.Wrapper.Types;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Embedded;
using NSubstitute;
using Xunit;
using IPAddress = System.Net.IPAddress;

namespace Catalyst.Common.UnitTests.IO
{
    public class SignatureDuplexHandlerTests
    {
        private readonly IKeySigner _keySigner;
        private readonly EmbeddedChannel _channel;

        public SignatureDuplexHandlerTests()
        {
            _keySigner = Substitute.For<IKeySigner>();
            _keySigner.Verify(Arg.Any<AnySigned>()).Returns(true);
            _keySigner.Sign(Arg.Any<byte[]>()).Returns(new Signature(new byte[64]));

            var signatureDuplexHandler = new SignatureDuplexHandler(_keySigner);
            _channel = new EmbeddedChannel(signatureDuplexHandler);
        }

        [Fact]
        public void Can_Block_Invalid_Signature_Message()
        {
            _keySigner.Verify(Arg.Any<AnySigned>()).Returns(false);

            var channelHandler = Substitute.For<IChannelHandler>();
            EmbeddedChannel channel = new EmbeddedChannel(
                new SignatureDuplexHandler(_keySigner),
                channelHandler
            );

            AnySigned anySigned = new AnySigned();
            channel.WriteInbound(anySigned);
            channelHandler.DidNotReceiveWithAnyArgs().ChannelRead(Arg.Any<IChannelHandlerContext>(), Arg.Any<object>());
        }

        [Fact]
        public void Can_Continue_Pipeline_On_Valid_Sig()
        {
            var channelHandler = Substitute.For<IChannelHandler>();
            EmbeddedChannel channel = new EmbeddedChannel(
                new SignatureDuplexHandler(_keySigner),
                channelHandler
            );

            AnySigned anySigned = new AnySigned();
            channel.WriteInbound(anySigned);
            channelHandler.Received(1).ChannelRead(Arg.Any<IChannelHandlerContext>(), Arg.Any<object>());
        }

        [Fact]
        public void Can_Signature_Generate_On_Outbound_TCP_Message()
        {
            AnySigned anySigned = new AnySigned();
            _channel.WriteOutbound(anySigned);
            _keySigner.Received(1).Sign(Arg.Any<byte[]>());
        }

        [Fact]
        public void Can_Verify_On_Inbound_TCP_Message()
        {
            AnySigned anySigned = new AnySigned();
            _channel.WriteInbound(anySigned);
            _keySigner.Received(1).Verify(anySigned);
        }

        [Fact]
        public void Can_Signature_Generate_On_Outbound_UDP_Message()
        {
            AnySigned anySigned = new AnySigned();
            _channel.WriteOutbound(anySigned.ToDatagram(new IPEndPoint(IPAddress.Any, IPEndPoint.MaxPort)));
            _keySigner.Received(1).Sign(Arg.Any<byte[]>());
        }

        [Fact]
        public void Can_Verify_On_Inbound_UDP_Message()
        {
            AnySigned anySigned = new AnySigned();
            _channel.WriteInbound(anySigned.ToDatagram(new IPEndPoint(IPAddress.Any, IPEndPoint.MaxPort)));
            _keySigner.Received(1).Verify(anySigned);
        }
    }
}
