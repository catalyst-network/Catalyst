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

using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Util;
using Catalyst.Protocol.Common;
using Dawn;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Google.Protobuf;

namespace Catalyst.Common.IO.Inbound
{
    public class SignatureDuplexHandler : ChannelDuplexHandler
    {
        private readonly IKeySigner _keySigner;

        public SignatureDuplexHandler(IKeySigner keySigner) { _keySigner = keySigner; }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            bool isUdp = message is DatagramPacket;

            AnySigned anySigned = isUdp
                ? ((DatagramPacket) message).ToAnySigned()
                : (AnySigned) message;

            bool valid = _keySigner.Verify(anySigned);

            if (valid)
            {
                IChanneledMessage<AnySigned> channeledMessage = new ChanneledAnySigned(context, anySigned);
                context.FireChannelRead(channeledMessage);
            }
        }

        public override Task WriteAsync(IChannelHandlerContext context, object message)
        {
            Guard.Argument(context).NotNull();
            Guard.Argument(message).NotNull();

            bool isUdp = message is DatagramPacket;
            
            if (isUdp)
            {
                DatagramPacket datagram = (DatagramPacket) message;

                var anySigned = datagram.ToAnySigned();
                Sign(anySigned);
                DatagramPacket packet = new DatagramPacket(
                    Unpooled.CopiedBuffer(anySigned.ToByteArray()), datagram.Sender, datagram.Recipient);
                return base.WriteAsync(context, packet);
            }
            else
            {
                var anySigned = (AnySigned) message;
                Sign(anySigned);
                return base.WriteAsync(context, anySigned);
            }
        }

        /// <summary>Signs the specified message.</summary>
        /// <param name="anySigned">The message.</param>
        private void Sign(AnySigned anySigned)
        {
            anySigned.Signature = _keySigner
               .Sign(anySigned.Value.ToByteArray())
               .Bytes.RawBytes.ToByteString();
        }
    }
}
