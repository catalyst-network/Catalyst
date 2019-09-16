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
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils.Protocol;
using Google.Protobuf;
using Serilog;

namespace Catalyst.TestUtils
{
    public static class ProtocolMessageExtensions
    {
        public static ProtocolMessage ToSignedProtocolMessage(this IMessage proto,
            PeerId senderId,
            ISignature signature = default,
            SigningContext signingContext = default,
            ICorrelationId correlationId = default)
        {
            return ToSignedProtocolMessage(proto, senderId, signature?.SignatureBytes, signingContext, correlationId);
        }

        public static ProtocolMessage ToSignedProtocolMessage(this IMessage proto,
            PeerId senderId = default,
            byte[] signature = default,
            SigningContext signingContext = default,
            ICorrelationId correlationId = default)
        {
            var peerId = senderId ?? PeerIdHelper.GetPeerId("sender");
            var protocolMessage = proto.ToProtocolMessage(peerId, 
                correlationId ?? CorrelationId.GenerateCorrelationId());
            var newSignature = new Signature
            {
                RawBytes = signature?.ToByteString() ?? ByteUtil.GenerateRandomByteArray(FFI.SignatureLength).ToByteString(),
                SigningContext = signingContext ?? DevNetPeerSigningContext.Instance
            };
            protocolMessage.Signature = newSignature;

            return protocolMessage;
        }
    }
}
