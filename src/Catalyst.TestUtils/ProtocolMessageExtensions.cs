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

using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Google.Protobuf;

namespace Catalyst.TestUtils
{
    public static class ProtocolMessageExtensions
    {
        public static ProtocolMessage ToSignedProtocolMessage(this IMessage proto,
            ISignature signature,
            PeerId senderId,
            SigningContext signingContext,
            ICorrelationId correlationId = default)
        {
            return ToSignedProtocolMessage(proto, signature.SignatureBytes, senderId, signingContext, correlationId);
        }

        public static ProtocolMessage ToSignedProtocolMessage(this IMessage proto,
            byte[] signature,
            PeerId senderId,
            SigningContext signingContext,
            ICorrelationId correlationId = default)
        {
            var protocolMessage = proto.ToProtocolMessage(senderId, correlationId
             ?? CorrelationId.GenerateCorrelationId());
            var newSignature = new Signature
            {
                RawBytes = signature.ToByteString(),
                SigningContext = signingContext
            };
            protocolMessage.Signature = newSignature;

            return protocolMessage;
        }
    }
}
