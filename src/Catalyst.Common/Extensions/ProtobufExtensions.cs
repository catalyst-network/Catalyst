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
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.Util;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Dawn;
using Google.Protobuf;
using Multiformats.Hash;

namespace Catalyst.Common.Extensions
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
        
        public static T FromIMessageDto<T>(this IMessageDto<T> message) where T : IMessage<T>
        {
            var empty = (T) Activator.CreateInstance(typeof(T));
            var typed = (T) empty.Descriptor.Parser.ParseFrom(message.Content.ToByteString());
            return typed;
        }

        public static ICorrelationId ToCorrelationId(this ByteString guidBytes)
        {
            var bytes = guidBytes?.ToByteArray();
            
            var validBytes = bytes?.Length == CorrelationId.GuidByteLength
                ? bytes
                : (bytes ?? new byte[0]).Concat(Enumerable.Repeat((byte) 0, CorrelationId.GuidByteLength)).Take(CorrelationId.GuidByteLength).ToArray();

            return new CorrelationId(new Guid(validBytes));
        }

        public static ByteString ToByteString(this Guid guid)
        {
            return guid.ToByteArray().ToByteString();
        }

        public static Multihash ToMultihash(this ByteString byteString)
        {
            return Multihash.Decode(byteString.ToByteArray());
        }

        public static string ToMultihashString(this ByteString byteString)
        {
            return ToMultihash(byteString).ToString();
        }
    }
}
