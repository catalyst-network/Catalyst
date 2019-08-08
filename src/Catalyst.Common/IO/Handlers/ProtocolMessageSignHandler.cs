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

using System.Reflection;
using System.Threading.Tasks;
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
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IKeySigner _keySigner;

        public ProtocolMessageSignHandler(IKeySigner keySigner)
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
            Logger.Verbose("Signing message {message}", message);
            var sig = _keySigner.Sign(message.Content.ToByteArray());
            
            var protocolMessageSigned = new ProtocolMessageSigned
            {
                Signature = sig.SignatureBytes.ToByteString(),
                Message = message.Content
            };

            var signedDto = new MessageDto<ProtocolMessageSigned>(protocolMessageSigned,
                message.SenderPeerIdentifier,
                message.RecipientPeerIdentifier,
                message.CorrelationId);
            
            return context.WriteAsync(signedDto);
        }
    }
}
