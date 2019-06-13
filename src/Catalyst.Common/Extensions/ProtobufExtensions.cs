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
using System.Linq;
using System.Net;
using System.Reflection;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.P2P.Messaging.Dto;
using Catalyst.Common.Util;
using Catalyst.Protocol.Common;
using Dawn;
using DotNetty.Buffers;
using DotNetty.Transport.Channels.Sockets;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Type = System.Type;

namespace Catalyst.Common.Extensions
{
    public static class ProtobufExtensions
    {
        private const string CatalystProtocol = "Catalyst.Protocol";

        private static readonly List<string> ProtoGossipAllowedMessages;
        private static readonly List<string> ProtoRequestAllowedMessages;
        
        static ProtobufExtensions()
        {
            var protoToClrNameMapper = typeof(ProtocolMessage).Assembly.ExportedTypes
               .Where(t => typeof(IMessage).IsAssignableFrom(t))
               .Select(t => ((IMessage) Activator.CreateInstance(t)).Descriptor)
               .ToDictionary(d => d.ShortenedFullName(), d => d.ClrType.FullName);
            ProtoGossipAllowedMessages = protoToClrNameMapper.Keys
               .Where(t => t.EndsWith(MessageTypes.Broadcast.Name))
               .ToList();

            ProtoRequestAllowedMessages = protoToClrNameMapper.Keys
               .Where(t => t.EndsWith(MessageTypes.Request.Name))
               .ToList();
        }

        public static string ShortenedFullName(this MessageDescriptor descriptor)
        {
            //we don't need to serialise the complete full name of our own types
            return descriptor.FullName.Remove(0, CatalystProtocol.Length + 1);
        }

        public static string ShortenedProtoFullName(this Type protoType)
        {
            Guard.Argument(protoType, nameof(protoType)).Require(t => typeof(IMessage).IsAssignableFrom(t));
            
            //get the static field Descriptor from T
            var descriptor = (MessageDescriptor) protoType
               .GetProperty("Descriptor", BindingFlags.Static | BindingFlags.Public)
               .GetValue(null);
            return ShortenedFullName(descriptor);
        }

        public static ProtocolMessage ToProtocolMessage(this IMessage protobufObject,
            PeerId senderId,
            Guid correlationId = default)
        {
            var typeUrl = protobufObject.Descriptor.ShortenedFullName();
            Guard.Argument(senderId, nameof(senderId)).NotNull();
            Guard.Argument(correlationId, nameof(correlationId))
               .Require(c => !typeUrl.EndsWith(MessageTypes.Response.Name) || c != default,
                    g => $"{typeUrl} is a response type and needs a correlationId");

            var protocolMessage = new ProtocolMessage
            {
                PeerId = senderId,
                CorrelationId = (correlationId == default ? Guid.NewGuid() : correlationId).ToByteString(),
                
                TypeUrl = typeUrl,
                Value = protobufObject.ToByteString()
            };
            return protocolMessage;
        }

        public static bool IsRequestType(this Type type)
        {
            var shortType = ShortenedProtoFullName(type);
            return ProtoRequestAllowedMessages.Contains(shortType);
        }

        public static bool CheckIfMessageIsBroadcast(this ProtocolMessage message)
        {
            return message.TypeUrl.EndsWith(nameof(ProtocolMessage)) &&
                ProtoGossipAllowedMessages.Contains(ProtocolMessage.Parser.ParseFrom(message.Value).TypeUrl);
        }

        public static T FromProtocolMessage<T>(this ProtocolMessage message) where T : IMessage<T>
        {
            var empty = (T) Activator.CreateInstance(typeof(T));
            var typed = (T) empty.Descriptor.Parser.ParseFrom(message.Value);
            return typed;
        }

        public static T FromIMessageDto<T>(this IMessageDto message) where T : IMessage<T>
        {
            var empty = (T) Activator.CreateInstance(typeof(T));
            var typed = (T) empty.Descriptor.Parser.ParseFrom(message.Message.ToByteString());
            return typed;
        }

        public static ByteString ToUtf8ByteString(this string utf8String)
        {
            return ByteString.CopyFromUtf8(utf8String);
        }

        public static Guid ToGuid(this ByteString guidBytes)
        {
            return new Guid(guidBytes.ToByteArray());
        }

        public static ByteString ToByteString(this Guid guid)
        {
            return guid.ToByteArray().ToByteString();
        }

        public static DatagramPacket ToDatagram<T>(this T anySignedMessage, IPEndPoint recipient) where T : IMessage<T>
        {
            return new DatagramPacket(Unpooled.WrappedBuffer(anySignedMessage.ToByteArray()), recipient);
        }

        public static string GetRequestType(this string responseTypeUrl)
        {
            return SwapSuffixes(responseTypeUrl, MessageTypes.Response.Name, MessageTypes.Request.Name);
        }

        public static string GetResponseType(this string requestTypeUrl)
        {
            return SwapSuffixes(requestTypeUrl, MessageTypes.Request.Name, MessageTypes.Response.Name);
        }

        private static string SwapSuffixes(string requestTypeUrl, string originalSuffix, string targetSuffix)
        {
            Guard.Argument(requestTypeUrl, nameof(requestTypeUrl)).NotNull()
               .Require(t => t.EndsWith(originalSuffix), t => $"{t} should end with {originalSuffix}");
            return requestTypeUrl
                   .Remove(requestTypeUrl.Length - originalSuffix.Length, originalSuffix.Length)
              + targetSuffix;
        }
    }
}
