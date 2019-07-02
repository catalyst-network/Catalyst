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
using System.IO;
using Catalyst.Protocol.Common;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Serilog;

namespace Catalyst.Common.IO.Handlers
{
    public sealed class DatagramProtobufDecoder : MessageToMessageEncoder<DatagramPacket>
    {
        public override bool IsSharable => true;

        private readonly ILogger _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        public DatagramProtobufDecoder(ILogger logger) { _logger = logger; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="datagramPacket"></param>
        /// <param name="output"></param>
        protected override void Encode(IChannelHandlerContext context,
            DatagramPacket datagramPacket,
            List<object> output)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    memoryStream.Write(datagramPacket.Content.Array, 0, datagramPacket.Content.ReadableBytes);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    output.Add(ProtocolMessageSigned.Parser.ParseFrom(memoryStream));
                }
            }
            catch (Exception e)
            {
                _logger.Debug(e, e.Message);
            }
        }
    }
}
