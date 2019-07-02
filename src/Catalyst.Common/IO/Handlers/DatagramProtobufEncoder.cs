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
using System.Collections.Generic;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Protocol.Common;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Common.IO.Handlers
{
    public sealed class DatagramProtobufEncoder : MessageToMessageEncoder<IMessageDto<ProtocolMessageSigned>>
    {
        public override bool IsSharable => true;

        private readonly ILogger _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        public DatagramProtobufEncoder(ILogger logger) { _logger = logger; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <param name="output"></param>
        /// <exception cref="CodecException"></exception>
        protected override void Encode(IChannelHandlerContext context,
            IMessageDto<ProtocolMessageSigned> message,
            List<object> output)
        {
            IByteBuffer byteBuffer = null;
            try
            {
                byteBuffer = Unpooled.WrappedBuffer(message.Message.ToByteArray());
                output.Add(new DatagramPacket(byteBuffer, message.Recipient.IpEndPoint));
            }
            catch (Exception e)
            {
                _logger.Debug(e, e.Message);
            }
            finally
            {
                byteBuffer?.Release();
            }
        }
    }
}
