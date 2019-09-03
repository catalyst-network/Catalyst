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
using System.Linq;
using System.Net;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.Types;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.Network;
using Catalyst.Core.Util;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Dawn;
using Google.Protobuf;
using Multiformats.Hash;
using Nethereum.RLP;

namespace Catalyst.Core.Extensions
{
    public static class ProtobufExtensions
    {
        public static ProtocolMessage ToProtocolMessage(this IMessage protobufObject,
            PeerId senderId,
            ICorrelationId correlationId = default)
        {
            var typeUrl = protobufObject.Descriptor.ShortenedFullName();
            Guard.Argument(senderId, nameof(senderId)).NotNull();

            if (typeUrl.EndsWith(MessageTypes.Response.Name))
            {
                Guard.Argument(correlationId, nameof(correlationId)).NotNull();
            }

            return new ProtocolMessage
            {
                PeerId = senderId,
                CorrelationId = (correlationId?.Id ?? CorrelationId.GenerateCorrelationId().Id).ToByteString(),

                TypeUrl = typeUrl,
                Value = protobufObject.ToByteString()
            };
        }

        public static ICorrelationId ToCorrelationId(this ByteString guidBytes)
        {
            var bytes = guidBytes?.ToByteArray();

            var validBytes = bytes?.Length == CorrelationId.GuidByteLength
                ? bytes
                : (bytes ?? new byte[0]).Concat(Enumerable.Repeat((byte) 0, CorrelationId.GuidByteLength))
               .Take(CorrelationId.GuidByteLength).ToArray();

            return new CorrelationId(new Guid(validBytes));
        }

        public static ByteString ToByteString(this Guid guid) { return guid.ToByteArray().ToByteString(); }

        public static Multihash AsMultihash(this ByteString byteString)
        {
            return Multihash.Decode(byteString.ToByteArray());
        }

        public static string AsMultihashString(this ByteString byteString)
        {
            return AsMultihash(byteString).ToString();
        }

        public static string AsBase32Address(this ByteString byteString)
        {
            return AsMultihash(byteString).AsBase32Address();
        }

        public static ByteString PublicKeyToProtobuf(this string publicKey)
        {
            return publicKey.ToBytesForRLPEncoding().ToByteString();
        }

        public static ByteString IpAddressToProtobuf(this IPAddress ipAddress)
        {
            return ByteString.CopyFrom(ipAddress.To16Bytes());
        }

        public static ByteString IpAddressToProtobuf(this string ipAddress)
        {
            return IPAddress.Parse(ipAddress).IpAddressToProtobuf();
        }
    }
}
