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
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.Util;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Common.IO.Handlers
{
    public sealed class ProtocolMessageSignHandler : OutboundChannelHandlerBase<IMessageDto<ProtocolMessage>>
    {
        private readonly IKeySigner _keySigner;

        public ProtocolMessageSignHandler(IKeySigner keySigner, ILogger logger) : base(logger)
        {
            _keySigner = keySigner;
        }

        /// <summary>
        ///     Signs a protocol message, or straight WriteAndFlush non-protocolMessages
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        protected override Task WriteAsync0(IChannelHandlerContext context, IMessageDto<ProtocolMessage> message)
        {
            var sig = _keySigner.Sign(message.Message.ToByteArray());
            
            var protocolMessageSigned = new ProtocolMessageSigned
            {
                Signature = sig.Bytes.RawBytes.ToByteString(),
                Message = message.Message.ToProtocolMessage(message.Sender.PeerId, message.CorrelationId)
            };

            return context.WriteAsync(new MessageSignedDto(protocolMessageSigned, message.MessageType, message.Recipient, message.Sender));
        }
    }
}
