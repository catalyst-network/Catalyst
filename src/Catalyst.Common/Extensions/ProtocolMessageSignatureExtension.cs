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

using Catalyst.Common.Interfaces.IO.Messaging;
using Google.Protobuf;
using Catalyst.Common.Util;
using Catalyst.Protocol.Common;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.Extensions
{
    public static class ProtocolMessageSignatureExtension
    {
        /// <summary>
        ///     Wraps ProtocolMessage in a ProtocolMessageSigned object and message signature from IKeySigner
        /// </summary>
        /// <param name="protocolMessage"></param>
        /// <param name="keySigner"></param>
        /// <returns></returns>
        public static ProtocolMessageSigned SignProtocolMessage(this ProtocolMessage protocolMessage, IKeySigner keySigner)
        {
            return new ProtocolMessageSigned
            {
                Message = protocolMessage,
                Signature = keySigner.Sign(protocolMessage.ToByteArray()).Bytes.RawBytes.ToByteString()
            };
        }
        
        /// <summary>
        ///     This methods yours!
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="protocolMessage"></param>
        /// <param name="keySigner"></param>
        /// <param name="messageCorrelationManager"></param>
        /// <returns></returns>
        public static bool SignSealAndDeliver(this IChannel channel, ProtocolMessage protocolMessage, IKeySigner keySigner, IMessageCorrelationManager messageCorrelationManager)
        {
            var protocolCignedMessage = SignProtocolMessage(protocolMessage, keySigner);
            return true;
        }
    }
}
